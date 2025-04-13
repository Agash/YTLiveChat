using System.Globalization;
using System.Text.RegularExpressions;
using YTLiveChat.Contracts.Models;
using YTLiveChat.Models;
using YTLiveChat.Models.Response;
using Action = YTLiveChat.Models.Response.Action; // Namespace for the new internal models

namespace YTLiveChat.Helpers;

internal static partial class Parser
{
    // --- GetOptionsFromLivePage remains the same ---
    public static FetchOptions GetOptionsFromLivePage(string raw)
    {
        Match idResult = LiveIdRegex().Match(raw);
        string liveId = idResult.Success ? idResult.Groups[1].Value : throw new Exception("Live Stream canonical link not found");

        Match replayResult = ReplayRegex().Match(raw);
        if (replayResult.Success)
        {
            throw new Exception($"{liveId} is finished live (isReplay: true found)");
        }

        Match keyResult = ApiKeyRegex().Match(raw);
        string apiKey = keyResult.Success ? keyResult.Groups[1].Value : throw new Exception("API Key (INNERTUBE_API_KEY) not found");

        Match verResult = ClientVersionRegex().Match(raw);
        string clientVersion = verResult.Success ? verResult.Groups[1].Value : throw new Exception("Client Version (INNERTUBE_CONTEXT_CLIENT_VERSION) not found");

        Match continuationResult = ContinuationRegex().Match(raw);
        string continuation = continuationResult.Success ? continuationResult.Groups[1].Value : throw new Exception("Initial Continuation token not found");

        return new()
        {
            ApiKey = apiKey,
            ClientVersion = clientVersion,
            Continuation = continuation,
            LiveId = liveId
        };
    }

    /// <summary>
    /// Extracts the relevant message renderer from a polymorphic item container.
    /// </summary>
    private static MessageRendererBase? GetBaseRenderer(AddChatItemActionItem? item)
    {
        if (item == null) return null;

        // Check in likely order of appearance or specificity
        return item.LiveChatPaidMessageRenderer as MessageRendererBase ??
               item.LiveChatPaidStickerRenderer as MessageRendererBase ??
               item.LiveChatMembershipItemRenderer as MessageRendererBase ??
               item.LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer as MessageRendererBase ??
               item.LiveChatSponsorshipsGiftRedemptionAnnouncementRenderer as MessageRendererBase ??
               item.LiveChatTextMessageRenderer as MessageRendererBase ?? // Standard text message last
               null; // PlaceholderItemRenderer doesn't inherit from Base
    }

    /// <summary>
    /// Converts a MessageRun (internal model) to a MessagePart (contract model).
    /// </summary>
    public static MessagePart ToMessagePart(this MessageRun run)
    {
        if (run is MessageText textRun && textRun.Text != null)
        {
            // TODO: Potentially handle navigationEndpoint, bold, italics later if needed
            return new TextPart { Text = textRun.Text };
        }

        if (run is MessageEmoji emojiRun && emojiRun.Emoji != null)
        {
            Emoji emoji = emojiRun.Emoji;
            bool isCustom = emoji.IsCustomEmoji;
            string? altText = emoji.Shortcuts?.FirstOrDefault() ?? emoji.SearchTerms?.FirstOrDefault(); // Use shortcut/search term as alt
            string emojiText = isCustom ? (altText ?? $"[:{emoji.EmojiId}:]") : (emoji.EmojiId ?? ""); // Use alt or ID for custom, ID for standard

            return new EmojiPart
            {
                Url = emoji.Image?.Thumbnails?.LastOrDefault()?.Url ?? string.Empty,
                IsCustomEmoji = isCustom,
                Alt = altText,
                EmojiText = emojiText
            };
        }

        // Fallback for unknown run types
        return new TextPart { Text = "[Unknown Message Part]" };
    }

    /// <summary>
    /// Converts an array of MessageRun (internal) to MessagePart[] (contract).
    /// </summary>
    public static MessagePart[] ToMessageParts(this IEnumerable<MessageRun>? runs) => runs?.Select(r => r.ToMessagePart()).ToArray() ?? [];

    /// <summary>
    /// Parses the primary amount/currency string into numeric value and symbol/code.
    /// </summary>
    private static (decimal AmountValue, string Currency) ParseAmount(string? amountString)
    {
        if (string.IsNullOrWhiteSpace(amountString)) return (0M, "USD");

        amountString = amountString.Replace(",", "").Replace("\u00A0", " ").Trim(); // Normalize spaces/commas

        Match match = AmountCurrencyRegex().Match(amountString);
        if (match.Success)
        {
            string currencySymbolOrCode = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[3].Value;
            string amountPart = match.Groups[2].Value;

            if (decimal.TryParse(amountPart, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amountValue))
            {
                string currencyCode = CurrencyHelper.GetCodeFromSymbolOrCode(currencySymbolOrCode);
                return (amountValue, currencyCode);
            }
        }

        if (decimal.TryParse(amountString, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal fallbackAmount))
        {
            return (fallbackAmount, "USD"); // Default currency
        }

        return (0M, "USD"); // Final fallback
    }


    /// <summary>
    /// Extracts membership details from the specific renderer types.
    /// </summary>
    private static MembershipDetails? ParseMembershipDetails(
        MessageRendererBase renderer,
        Author author // Pass the already parsed author for Gift events
    )
    {
        string? levelNameFromBadge = renderer.AuthorBadges?
            .FirstOrDefault(b => b.CustomThumbnail != null)?
            .Tooltip;

        MembershipDetails details = new() { LevelName = levelNameFromBadge ?? "Member" }; // Default level name

        switch (renderer)
        {
            case LiveChatMembershipItemRenderer membershipItem:
                details.HeaderSubtext = membershipItem.HeaderSubtext?.Text;
                details.HeaderPrimaryText = membershipItem.HeaderPrimaryText?.Runs?.ToMessageParts()?.ToSimpleString();

                // Determine type: New or Milestone based on text
                if (details.HeaderPrimaryText?.StartsWith("Welcome", StringComparison.OrdinalIgnoreCase) == true ||
                    details.HeaderSubtext?.StartsWith("New member", StringComparison.OrdinalIgnoreCase) == true)
                {
                    details.EventType = MembershipEventType.New;
                    // Try to refine level name if needed (less reliable than badge)
                    if (details.LevelName == "Member" && details.HeaderPrimaryText != null)
                    {
                        Match levelMatch = NewMemberLevelRegex().Match(details.HeaderPrimaryText);
                        if (levelMatch.Success) details.LevelName = levelMatch.Groups[1].Value.Trim();
                    }
                }
                else if (details.HeaderPrimaryText?.Contains("member for", StringComparison.OrdinalIgnoreCase) == true)
                {
                    details.EventType = MembershipEventType.Milestone;
                    Match monthsMatch = MilestoneMonthsRegex().Match(details.HeaderPrimaryText);
                    if (monthsMatch.Success && int.TryParse(monthsMatch.Groups[1].Value, out int months))
                    {
                        details.MilestoneMonths = months;
                    }
                }

                break;

            case LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer giftPurchase:
                details.EventType = MembershipEventType.GiftPurchase;
                details.HeaderPrimaryText = giftPurchase.Header?.LiveChatSponsorshipsHeaderRenderer?.PrimaryText?.Runs?.ToMessageParts()?.ToSimpleString();
                details.GifterUsername = author.Name; // Author of this event is the gifter

                // Parse count from text like "Gifted 5 memberships"
                if (details.HeaderPrimaryText != null)
                {
                    Match giftMatch = GiftedCountRegex().Match(details.HeaderPrimaryText);
                    if (giftMatch.Success && int.TryParse(giftMatch.Groups[1].Value, out int count))
                    {
                        details.GiftCount = count;
                    }
                    else if (details.HeaderPrimaryText.Contains("gifted", StringComparison.OrdinalIgnoreCase))
                    {
                        details.GiftCount = 1; // Default to 1 if text indicates gift but count parsing fails
                    }
                }
                // Level name often not present in gift purchase message, rely on badge if available
                details.LevelName = levelNameFromBadge ?? "Member"; // Fallback if gifter has no level badge visible
                break;

            case LiveChatSponsorshipsGiftRedemptionAnnouncementRenderer giftRedemption:
                details.EventType = MembershipEventType.GiftRedemption;
                details.HeaderPrimaryText = giftRedemption.Message?.Runs?.ToMessageParts()?.ToSimpleString(); // Usually "Welcome!"
                details.RecipientUsername = author.Name; // Author of this event is the recipient
                // Level name comes from the recipient's badge
                details.LevelName = levelNameFromBadge ?? "Member";
                break;

            default:
                return null; // Not a membership-related event
        }

        return details;
    }

    /// <summary>
    /// Converts a raw YouTube Action into a StreamWeaver ChatItem.
    /// </summary>
    public static ChatItem? ToChatItem(this Action action) // Updated parameter type
    {
        ArgumentNullException.ThrowIfNull(action);

        // We only care about adding chat items for now
        if (action.AddChatItemAction?.Item == null)
        {
            return null;
        }

        AddChatItemActionItem item = action.AddChatItemAction.Item;
        MessageRendererBase? baseRenderer = GetBaseRenderer(item);

        if (baseRenderer == null)
        {
            // Could be a placeholder or unsupported type
            if (item.LiveChatPlaceholderItemRenderer != null)
            {
                // Handle placeholder if needed (e.g., for deleted messages)
                // For now, we skip them.
            }
            else if (item.LiveChatViewerEngagementMessageRenderer != null)
            {
                // Skip engagement messages for now
            }
            else
            {
                // Log unknown item type if debug logging is on
                // Console.WriteLine($"[YTLiveChat DEBUG] Skipped unknown item type: {action.AddChatItemAction.Item}");
            }

            return null;
        }

        // --- Author & Badges ---
        Author author = new()
        {
            Name = baseRenderer.AuthorName?.Text ?? "Unknown Author",
            ChannelId = baseRenderer.AuthorExternalChannelId ?? string.Empty,
            Thumbnail = baseRenderer.AuthorPhoto?.Thumbnails?.ToImage(baseRenderer.AuthorName?.Text)
        };

        bool isMembershipBadge = false; // Specifically check for membership badge presence
        bool isVerified = false;
        bool isOwner = false;
        bool isModerator = false;

        if (baseRenderer.AuthorBadges != null)
        {
            foreach (LiveChatAuthorBadgeRenderer badgeRenderer in baseRenderer.AuthorBadges)
            {
                if (badgeRenderer.CustomThumbnail != null) // Membership badges
                {
                    isMembershipBadge = true;
                    // Use the first membership badge found for the Author contract
                    author.Badge ??= new Badge
                    {
                        Thumbnail = badgeRenderer.CustomThumbnail.Thumbnails?.ToImage(badgeRenderer.Tooltip),
                        Label = badgeRenderer.Tooltip ?? "Member"
                    };
                }
                else // Standard badges
                {
                    switch (badgeRenderer.Icon?.IconType)
                    {
                        case "OWNER": isOwner = true; break;
                        case "VERIFIED": isVerified = true; break;
                        case "MODERATOR": isModerator = true; break;
                    }
                }
            }
        }

        // --- Message Content ---
        MessagePart[] messageParts = [];
        if (item.LiveChatTextMessageRenderer?.Message?.Runs != null)
        {
            messageParts = item.LiveChatTextMessageRenderer.Message.Runs.ToMessageParts();
        }
        else if (item.LiveChatPaidMessageRenderer?.Message?.Runs != null) // Paid message content
        {
            messageParts = item.LiveChatPaidMessageRenderer.Message.Runs.ToMessageParts();
        }
        // Note: Membership renderers' primary text is handled within ParseMembershipDetails

        // --- Super Chat / Sticker Details ---
        Superchat? superchatDetails = null;
        if (item.LiveChatPaidMessageRenderer != null)
        {
            LiveChatPaidMessageRenderer paidRenderer = item.LiveChatPaidMessageRenderer;
            (decimal amountValue, string currency) = ParseAmount(paidRenderer.PurchaseAmountText?.Text);
            superchatDetails = new Superchat
            {
                AmountString = paidRenderer.PurchaseAmountText?.Text ?? string.Empty,
                AmountValue = amountValue,
                Currency = currency,
                BodyBackgroundColor = paidRenderer.BodyBackgroundColor.ToHex6Color() ?? "000000", // Black fallback
                HeaderBackgroundColor = paidRenderer.HeaderBackgroundColor.ToHex6Color(),
                HeaderTextColor = paidRenderer.HeaderTextColor.ToHex6Color(),
                BodyTextColor = paidRenderer.BodyTextColor.ToHex6Color(),
                AuthorNameTextColor = paidRenderer.AuthorNameTextColor.ToHex6Color(),
                Sticker = null // Not a sticker
            };
        }
        else if (item.LiveChatPaidStickerRenderer != null)
        {
            LiveChatPaidStickerRenderer stickerRenderer = item.LiveChatPaidStickerRenderer;
            (decimal amountValue, string currency) = ParseAmount(stickerRenderer.PurchaseAmountText?.Text);
            superchatDetails = new Superchat
            {
                AmountString = stickerRenderer.PurchaseAmountText?.Text ?? string.Empty,
                AmountValue = amountValue,
                Currency = currency,
                BodyBackgroundColor = stickerRenderer.BackgroundColor.ToHex6Color() ?? "000000", // Main bg color
                AuthorNameTextColor = stickerRenderer.AuthorNameTextColor.ToHex6Color(),
                Sticker = stickerRenderer.Sticker?.Thumbnails?.ToImage(stickerRenderer.Sticker?.Accessibility?.AccessibilityData?.Label)
                // Header/Body colors don't apply to stickers
            };
        }

        // --- Membership Details ---
        // Parse details if it's any of the membership-related renderers
        MembershipDetails? membershipInfo = ParseMembershipDetails(baseRenderer, author);


        // --- Timestamp ---
        DateTimeOffset timestamp = DateTimeOffset.UtcNow; // Default to now
        if (long.TryParse(baseRenderer.TimestampUsec, out long timestampUsec))
        {
            timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampUsec / 1000);
        }

        // --- Construct Final ChatItem ---
        ChatItem chatItem = new()
        {
            Id = baseRenderer.Id ?? Guid.NewGuid().ToString(), // Ensure ID exists
            Timestamp = timestamp,
            Author = author,
            Message = messageParts,
            Superchat = superchatDetails,
            MembershipDetails = membershipInfo,
            IsMembership = isMembershipBadge || membershipInfo != null, // True if has badge OR is a membership event
            IsVerified = isVerified,
            IsOwner = isOwner,
            IsModerator = isModerator
        };

        return chatItem;
    }

    /// <summary>
    /// Converts an array of image thumbnails (internal model) to an ImagePart (contract model).
    /// </summary>
    public static ImagePart? ToImage(this List<Thumbnail>? thumbnails, string? alt = null)
    {
        // Prefer larger thumbnails if available (usually last in array)
        Thumbnail? thumbnail = thumbnails?.LastOrDefault();
        return thumbnail == null || thumbnail.Url == null
            ? null
            : new ImagePart
            {
                Url = thumbnail.Url,
                Alt = alt,
                // We don't have width/height directly in ImagePart contract
            };
    }

    /// <summary>
    /// Parses the full API response to extract chat items and the next continuation token.
    /// </summary>
    /// <param name="response">The deserialized API response.</param>
    /// <returns>A tuple containing a list of parsed ChatItems and the next continuation token string.</returns>
    public static (List<ChatItem> Items, string? Continuation) ParseLiveChatResponse(LiveChatResponse? response) // Changed method name for clarity
    {
        List<ChatItem> items = [];
        string? continuationToken = null;

        if (response?.ContinuationContents?.LiveChatContinuation?.Actions != null)
        {
            items = [.. response.ContinuationContents.LiveChatContinuation.Actions
                .Select(a => a.ToChatItem()) // Uses the updated ToChatItem method
                .WhereNotNull()
                ];
        }

        // Extract the next continuation token
        Continuation? nextContinuation = response?.ContinuationContents?.LiveChatContinuation?.Continuations?.FirstOrDefault();
        continuationToken = nextContinuation?.InvalidationContinuationData?.Continuation ??
                            nextContinuation?.TimedContinuationData?.Continuation;

        // Sometimes the continuation might be at the root level (initial response)
        if (string.IsNullOrEmpty(continuationToken))
        {
            continuationToken = response?.InvalidationContinuationData?.Continuation;
        }

        return (items, continuationToken);
    }

    // --- Regex Definitions ---
    [GeneratedRegex("<link rel=\"canonical\" href=\"https:\\/\\/www\\.youtube\\.com\\/watch\\?v=([^\"]+)\">", RegexOptions.Compiled)]
    private static partial Regex LiveIdRegex();

    [GeneratedRegex("\"isReplay\":\\s*(true)", RegexOptions.Compiled)]
    private static partial Regex ReplayRegex();

    [GeneratedRegex("\"INNERTUBE_API_KEY\":\\s*\"([^\"]*)\"", RegexOptions.Compiled)]
    private static partial Regex ApiKeyRegex();

    [GeneratedRegex("\"INNERTUBE_CONTEXT_CLIENT_VERSION\":\\s*\"([^\"]*)\"", RegexOptions.Compiled)] // Updated key based on typical web source
    private static partial Regex ClientVersionRegex();

    [GeneratedRegex("\"continuation\":\\s*\"([^\"]*)\"", RegexOptions.Compiled)]
    private static partial Regex ContinuationRegex();

    // Regex to capture currency symbol OR code and amount value more robustly
    // Allows optional space, handles symbols before/after, captures value with decimals/commas
    [GeneratedRegex(@"(?:([€$£¥])\s?([\d,.]+)|\s?([\d,.]+)\s?([A-Z]{3}|[€$£¥]))", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex AmountCurrencyRegex_Experimental(); // Keeping old one as fallback if needed
    [GeneratedRegex(@"([€$£¥])?\s*([\d.,]+)\s*(?:([A-Z]{3}))?", RegexOptions.Compiled | RegexOptions.CultureInvariant)] // Original one, safer?
    private static partial Regex AmountCurrencyRegex();


    [GeneratedRegex(@"member for (\d+) months?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MilestoneMonthsRegex();

    [GeneratedRegex(@"gifted (\d+|a) membership", RegexOptions.IgnoreCase | RegexOptions.Compiled)] // Handle "a membership"
    private static partial Regex GiftedCountRegex();

    [GeneratedRegex(@"Welcome to (.*?) membership", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NewMemberLevelRegex();

    // Helper extension method to convert message parts back to a simple string
    private static string ToSimpleString(this IEnumerable<MessagePart>? parts)
    {
        return string.Join("", parts?.Select(p => p switch
        {
            TextPart tp => tp.Text,
            EmojiPart ep => ep.EmojiText, // Use text representation of emoji
            _ => ""
        }) ?? Enumerable.Empty<string>());
    }

    // Helper class for currency symbol/code mapping
    private static class CurrencyHelper
    {
        private static readonly Dictionary<string, string> s_symbolToCode = new(StringComparer.OrdinalIgnoreCase)
        {
            ["€"] = "EUR",
            ["£"] = "GBP",
            ["¥"] = "JPY",
            ["$"] = "USD",
            ["₽"] = "RUB",
            ["₹"] = "INR",
            ["₩"] = "KRW",
            ["₱"] = "PHP",
            ["฿"] = "THB",
            ["₫"] = "VND",
            // Add more common symbols as needed
        };

        public static string GetCodeFromSymbolOrCode(string symbolOrCode)
        {
            if (string.IsNullOrWhiteSpace(symbolOrCode)) return "USD"; // Default
            // Check if it's already a known code (3 letters)
            if (symbolOrCode.Length == 3 && symbolOrCode.All(char.IsLetter)) return symbolOrCode.ToUpperInvariant();
            // Try mapping from symbol
            if (s_symbolToCode.TryGetValue(symbolOrCode, out string? code)) return code;
            // Fallback defaults
            if (symbolOrCode == "$") return "USD";
            if (symbolOrCode == "¥") return "JPY";
            return symbolOrCode.ToUpperInvariant(); // Return original if unknown
        }
    }
}