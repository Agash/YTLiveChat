using System.Globalization;
using System.Text.RegularExpressions;
using YTLiveChat.Contracts.Models; // Use the contract namespace
using YTLiveChat.Models;          // Internal models namespace
using YTLiveChat.Models.Response; // Internal response models namespace
using Action = YTLiveChat.Models.Response.Action; // Explicitly use internal Action

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
               item.LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer as MessageRendererBase ?? // Check before text msg
               item.LiveChatSponsorshipsGiftRedemptionAnnouncementRenderer as MessageRendererBase ?? // Check before text msg
               item.LiveChatTextMessageRenderer as MessageRendererBase ?? // Standard text message last
               null; // PlaceholderItemRenderer doesn't inherit from Base
    }

    /// <summary>
    /// Converts a MessageRun (internal model) to a MessagePart (contract model).
    /// </summary>
    public static Contracts.Models.MessagePart ToMessagePart(this Models.Response.MessageRun run) // Specify internal type
    {
        if (run is Models.Response.MessageText textRun && textRun.Text != null)
        {
            // TODO: Potentially handle navigationEndpoint, bold, italics later if needed
            return new Contracts.Models.TextPart { Text = textRun.Text }; // Use contract type
        }

        if (run is Models.Response.MessageEmoji emojiRun && emojiRun.Emoji != null)
        {
            Emoji emoji = emojiRun.Emoji;
            bool isCustom = emoji.IsCustomEmoji;
            string? altText = emoji.Shortcuts?.FirstOrDefault() ?? emoji.SearchTerms?.FirstOrDefault(); // Use shortcut/search term as alt
            string emojiText = isCustom ? (altText ?? $"[:{emoji.EmojiId}:]") : (emoji.EmojiId ?? ""); // Use alt or ID for custom, ID for standard
            return new Contracts.Models.EmojiPart // Use contract type
            {
                Url = emoji.Image?.Thumbnails?.LastOrDefault()?.Url ?? string.Empty,
                IsCustomEmoji = isCustom,
                Alt = altText,
                EmojiText = emojiText
            };
        }
        // Fallback for unknown run types
        return new Contracts.Models.TextPart { Text = "[Unknown Message Part]" }; // Use contract type
    }

    /// <summary>
    /// Converts an array of MessageRun (internal) to MessagePart[] (contract).
    /// </summary>
    public static Contracts.Models.MessagePart[] ToMessageParts(this IEnumerable<Models.Response.MessageRun>? runs) =>
        runs?.Select(r => r.ToMessagePart()).ToArray() ?? []; // Ensure call uses corrected ToMessagePart

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
        // Fallback parsing attempt if regex fails
        if (decimal.TryParse(amountString, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal fallbackAmount))
        {
            return (fallbackAmount, "USD"); // Default currency
        }

        return (0M, "USD"); // Final fallback
    }

    /// <summary>
    /// Converts a raw YouTube Action into a StreamWeaver ChatItem contract.
    /// </summary>
    public static Contracts.Models.ChatItem? ToChatItem(this Action action) // Return contract type
    {
        ArgumentNullException.ThrowIfNull(action);

        if (action.AddChatItemAction?.Item == null) return null;

        AddChatItemActionItem item = action.AddChatItemAction.Item;
        MessageRendererBase? baseRenderer = GetBaseRenderer(item);

        if (baseRenderer == null)
        {
            // Handle placeholder/skipped types if needed
            return null;
        }

        // --- Author & Badges ---
        // Start with default author
        Contracts.Models.Author author = new() // Use contract type
        {
            Name = baseRenderer.AuthorName?.Text ?? "Unknown Author",
            ChannelId = baseRenderer.AuthorExternalChannelId ?? string.Empty,
            Thumbnail = baseRenderer.AuthorPhoto?.Thumbnails?.ToImage(baseRenderer.AuthorName?.Text)
        };

        // Determine flags and badge *within* the loop
        bool isMembershipBadge = false;
        bool isVerified = false;
        bool isOwner = false;
        bool isModerator = false;
        Contracts.Models.Badge? authorBadge = null; // Use contract type

        if (baseRenderer.AuthorBadges != null)
        {
            foreach (AuthorBadgeContainer badgeContainer in baseRenderer.AuthorBadges)
            {
                if (badgeContainer.LiveChatAuthorBadgeRenderer?.CustomThumbnail != null) // Membership badges
                {
                    isMembershipBadge = true;
                    // Populate the authorBadge object directly
                    authorBadge ??= new Contracts.Models.Badge // Use contract type
                    {
                        Thumbnail = badgeContainer.LiveChatAuthorBadgeRenderer?.CustomThumbnail.Thumbnails?.ToImage(badgeContainer.LiveChatAuthorBadgeRenderer?.Tooltip),
                        Label = badgeContainer.LiveChatAuthorBadgeRenderer?.Tooltip ?? "Member"
                    };
                }
                else // Standard badges
                {
                    switch (badgeContainer.LiveChatAuthorBadgeRenderer?.Icon?.IconType)
                    {
                        case "OWNER": isOwner = true; break;
                        case "VERIFIED": isVerified = true; break;
                        case "MODERATOR": isModerator = true; break;
                    }
                }
            }
        }
        // *** Assign the parsed Badge object back to the Author ***
        author.Badge = authorBadge;

        // --- Message Content ---
        Contracts.Models.MessagePart[] messageParts = []; // Use contract type
        // ... (Parsing logic remains the same, uses ToMessageParts which returns contract type) ...
        if (item.LiveChatTextMessageRenderer?.Message?.Runs != null)
        {
            messageParts = item.LiveChatTextMessageRenderer.Message.Runs.ToMessageParts();
        }
        else if (item.LiveChatPaidMessageRenderer?.Message?.Runs != null) // Paid message content
        {
            messageParts = item.LiveChatPaidMessageRenderer.Message.Runs.ToMessageParts();
        }

        // --- Super Chat / Sticker Details ---
        Contracts.Models.Superchat? superchatDetails = null; // Use contract type
        if (item.LiveChatPaidMessageRenderer != null)
        {
            LiveChatPaidMessageRenderer paidRenderer = item.LiveChatPaidMessageRenderer;
            (decimal amountValue, string currency) = ParseAmount(paidRenderer.PurchaseAmountText?.Text);
            superchatDetails = new Contracts.Models.Superchat // Use contract type
            {
                AmountString = paidRenderer.PurchaseAmountText?.Text ?? string.Empty,
                AmountValue = amountValue,
                Currency = currency,
                BodyBackgroundColor = paidRenderer.BodyBackgroundColor.ToHex6Color() ?? "000000",
                HeaderBackgroundColor = paidRenderer.HeaderBackgroundColor.ToHex6Color(),
                HeaderTextColor = paidRenderer.HeaderTextColor.ToHex6Color(),
                BodyTextColor = paidRenderer.BodyTextColor.ToHex6Color(),
                AuthorNameTextColor = paidRenderer.AuthorNameTextColor.ToHex6Color(),
                Sticker = null
            };
        }
        else if (item.LiveChatPaidStickerRenderer != null)
        {
            LiveChatPaidStickerRenderer stickerRenderer = item.LiveChatPaidStickerRenderer;
            (decimal amountValue, string currency) = ParseAmount(stickerRenderer.PurchaseAmountText?.Text);
            superchatDetails = new Contracts.Models.Superchat // Use contract type
            {
                AmountString = stickerRenderer.PurchaseAmountText?.Text ?? string.Empty,
                AmountValue = amountValue,
                Currency = currency,
                BodyBackgroundColor = stickerRenderer.BackgroundColor.ToHex6Color() ?? "000000",
                AuthorNameTextColor = stickerRenderer.AuthorNameTextColor.ToHex6Color(),
                Sticker = stickerRenderer.Sticker?.Thumbnails?.ToImage(stickerRenderer.Sticker?.Accessibility?.AccessibilityData?.Label)
            };
        }

        // --- Membership Details ---
        MembershipDetails? membershipInfo = null;
        switch (baseRenderer)
        {
            case LiveChatMembershipItemRenderer membershipItem:
                string? levelNameFromBadge = baseRenderer.AuthorBadges?
                    .FirstOrDefault(b => b.LiveChatAuthorBadgeRenderer?.CustomThumbnail != null)?
                    .LiveChatAuthorBadgeRenderer?.Tooltip;
                membershipInfo = new()
                {
                    LevelName = levelNameFromBadge ?? "Member",
                    HeaderSubtext = membershipItem.HeaderSubtext?.Text,
                    HeaderPrimaryText = membershipItem.HeaderPrimaryText?.Runs?.ToMessageParts()?.ToSimpleString()
                };

                if (membershipInfo.HeaderPrimaryText?.StartsWith("Welcome", StringComparison.OrdinalIgnoreCase) == true ||
                    membershipInfo.HeaderSubtext?.StartsWith("New member", StringComparison.OrdinalIgnoreCase) == true)
                {
                    membershipInfo.EventType = Contracts.Models.MembershipEventType.New;
                    if (membershipInfo.LevelName == "Member" && membershipInfo.HeaderPrimaryText != null)
                    {
                        Match levelMatch = NewMemberLevelRegex().Match(membershipInfo.HeaderPrimaryText);
                        if (levelMatch.Success) membershipInfo.LevelName = levelMatch.Groups[1].Value.Trim();
                    }
                }
                else if (membershipInfo.HeaderPrimaryText?.Contains("member for", StringComparison.OrdinalIgnoreCase) == true)
                {
                    membershipInfo.EventType = Contracts.Models.MembershipEventType.Milestone;
                    Match monthsMatch = MilestoneMonthsRegex().Match(membershipInfo.HeaderPrimaryText);
                    if (monthsMatch.Success && int.TryParse(monthsMatch.Groups[1].Value, out int months))
                    {
                        membershipInfo.MilestoneMonths = months;
                    }
                }

                break;

            case LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer giftPurchase:
                levelNameFromBadge = baseRenderer.AuthorBadges?
                    .FirstOrDefault(b => b.LiveChatAuthorBadgeRenderer?.CustomThumbnail != null)?
                    .LiveChatAuthorBadgeRenderer?.Tooltip;
                membershipInfo = new()
                {
                    LevelName = levelNameFromBadge ?? "Member",
                    EventType = Contracts.Models.MembershipEventType.GiftPurchase,
                    HeaderPrimaryText = giftPurchase.Header?.LiveChatSponsorshipsHeaderRenderer?.PrimaryText?.Runs?.ToMessageParts()?.ToSimpleString(),
                    GifterUsername = giftPurchase.Header?.LiveChatSponsorshipsHeaderRenderer?.AuthorName?.Text // Author of this event is the gifter
                };
                author.Name = giftPurchase.Header?.LiveChatSponsorshipsHeaderRenderer?.AuthorName?.Text ?? author.Name; // Looks like the Message in general doesn't have information on this, so we populate here.
                author.Thumbnail = giftPurchase.Header?.LiveChatSponsorshipsHeaderRenderer?.AuthorPhoto?.Thumbnails.ToImage(author.Name);

                if (giftPurchase.Header?.LiveChatSponsorshipsHeaderRenderer?.AuthorBadges != null)
                {
                    foreach (AuthorBadgeContainer badgeContainer in giftPurchase.Header?.LiveChatSponsorshipsHeaderRenderer?.AuthorBadges ?? [])
                    {
                        if (badgeContainer.LiveChatAuthorBadgeRenderer?.CustomThumbnail != null) // Membership badges
                        {
                            isMembershipBadge = true;
                            // Populate the authorBadge object directly
                            authorBadge ??= new Contracts.Models.Badge // Use contract type
                            {
                                Thumbnail = badgeContainer.LiveChatAuthorBadgeRenderer?.CustomThumbnail.Thumbnails?.ToImage(badgeContainer.LiveChatAuthorBadgeRenderer?.Tooltip),
                                Label = badgeContainer.LiveChatAuthorBadgeRenderer?.Tooltip ?? "Member"
                            };
                        }
                        else // Standard badges
                        {
                            switch (badgeContainer.LiveChatAuthorBadgeRenderer?.Icon?.IconType)
                            {
                                case "OWNER": isOwner = true; break;
                                case "VERIFIED": isVerified = true; break;
                                case "MODERATOR": isModerator = true; break;
                            }
                        }
                    }
                }
                // Assign the parsed Badge object back to the Author
                author.Badge = authorBadge;

                if (membershipInfo.HeaderPrimaryText != null)
                {
                    Match giftMatch = GiftedCountRegex().Match(membershipInfo.HeaderPrimaryText);
                    if (giftMatch.Success && int.TryParse(giftMatch.Groups[1].Value, out int count))
                    {
                        membershipInfo.GiftCount = count;
                    }
                    else if (membershipInfo.HeaderPrimaryText.Contains("gifted", StringComparison.OrdinalIgnoreCase) ||
                             membershipInfo.HeaderPrimaryText.Contains("membership gift", StringComparison.OrdinalIgnoreCase)) // Handle "a membership gift"
                    {
                        membershipInfo.GiftCount = 1; // Default to 1
                    }
                }

                membershipInfo.LevelName = levelNameFromBadge ?? "Member"; // Gifter's level or fallback
                break;

            case LiveChatSponsorshipsGiftRedemptionAnnouncementRenderer giftRedemption:
                levelNameFromBadge = baseRenderer.AuthorBadges?
                    .FirstOrDefault(b => b.LiveChatAuthorBadgeRenderer?.CustomThumbnail != null)?
                    .LiveChatAuthorBadgeRenderer?.Tooltip;
                membershipInfo = new()
                {
                    LevelName = levelNameFromBadge ?? "Member",
                    EventType = Contracts.Models.MembershipEventType.GiftRedemption,
                    HeaderPrimaryText = giftRedemption.Message?.Runs?.ToMessageParts()?.ToSimpleString(), // Usually "Welcome!"
                    RecipientUsername = author.Name // Author of this event is the recipient
                };
                membershipInfo.LevelName = levelNameFromBadge ?? "Member"; // Recipient's level
                break;
        }

        // --- Timestamp ---
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        if (long.TryParse(baseRenderer.TimestampUsec, out long timestampUsec))
        {
            timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampUsec / 1000);
        }

        // --- Construct Final ChatItem (Using Contract Model) ---
        Contracts.Models.ChatItem chatItem = new() // Use contract type
        {
            Id = baseRenderer.Id ?? Guid.NewGuid().ToString(),
            Timestamp = timestamp,
            Author = author, // Assign the fully populated author object
            Message = messageParts,
            Superchat = superchatDetails,
            MembershipDetails = membershipInfo,
            // *** FIX: Assign the calculated boolean flags ***
            IsVerified = isVerified,
            IsOwner = isOwner,
            IsModerator = isModerator,
            IsMembership = isMembershipBadge || membershipInfo != null // Recalculate here or use local var
        };
        return chatItem;
    }


    /// <summary>
    /// Converts an array of image thumbnails (internal model) to an ImagePart (contract model).
    /// </summary>
    public static Contracts.Models.ImagePart? ToImage(this List<Thumbnail>? thumbnails, string? alt = null) // Return contract type
    {
        Thumbnail? thumbnail = thumbnails?.LastOrDefault();
        return thumbnail == null || thumbnail.Url == null
            ? null
            : new Contracts.Models.ImagePart // Use contract type
            {
                Url = thumbnail.Url,
                Alt = alt,
            };
    }

    /// <summary>
    /// Parses the full API response to extract chat items and the next continuation token.
    /// </summary>
    public static (List<Contracts.Models.ChatItem> Items, string? Continuation) ParseLiveChatResponse(LiveChatResponse? response) // Return contract type
    {
        List<Contracts.Models.ChatItem> items = []; // Use contract type
        string? continuationToken = null;

        if (response?.ContinuationContents?.LiveChatContinuation?.Actions != null)
        {
            items = [.. response.ContinuationContents.LiveChatContinuation.Actions
                .Select(a => a.ToChatItem()) // Uses the corrected ToChatItem
                .WhereNotNull()
                ];
        }

        Continuation? nextContinuation = response?.ContinuationContents?.LiveChatContinuation?.Continuations?.FirstOrDefault();
        continuationToken = nextContinuation?.InvalidationContinuationData?.Continuation ??
                            nextContinuation?.TimedContinuationData?.Continuation;

        if (string.IsNullOrEmpty(continuationToken))
        {
            continuationToken = response?.InvalidationContinuationData?.Continuation;
        }

        return (items, continuationToken);
    }

    // --- Regex Definitions (remain the same) ---
    [GeneratedRegex("<link rel=\"canonical\" href=\"https:\\/\\/www\\.youtube\\.com\\/watch\\?v=([^\"]+)\">", RegexOptions.Compiled)]
    private static partial Regex LiveIdRegex();
    [GeneratedRegex("\"isReplay\":\\s*(true)", RegexOptions.Compiled)]
    private static partial Regex ReplayRegex();
    [GeneratedRegex("\"INNERTUBE_API_KEY\":\\s*\"([^\"]*)\"", RegexOptions.Compiled)]
    private static partial Regex ApiKeyRegex();
    [GeneratedRegex("\"INNERTUBE_CONTEXT_CLIENT_VERSION\":\\s*\"([^\"]*)\"", RegexOptions.Compiled)]
    private static partial Regex ClientVersionRegex();
    [GeneratedRegex("\"continuation\":\\s*\"([^\"]*)\"", RegexOptions.Compiled)]
    private static partial Regex ContinuationRegex();
    [GeneratedRegex(@"([€$£¥])?\s*([\d.,]+)\s*(?:([A-Z]{3}))?", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex AmountCurrencyRegex();
    [GeneratedRegex(@"member for (\d+) months?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MilestoneMonthsRegex();
    [GeneratedRegex(@"gifted (\d+|a) membership", RegexOptions.IgnoreCase | RegexOptions.Compiled)] // Handle "a membership"
    private static partial Regex GiftedCountRegex();
    [GeneratedRegex(@"Welcome to (.*?) membership", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NewMemberLevelRegex();

    // Helper extension method to convert message parts back to a simple string
    private static string ToSimpleString(this IEnumerable<Contracts.Models.MessagePart>? parts) // Use contract type
    {
        return string.Join("", parts?.Select(p => p switch
        {
            Contracts.Models.TextPart tp => tp.Text,
            Contracts.Models.EmojiPart ep => ep.EmojiText,
            _ => ""
        }) ?? []);
    }

    // Helper class for currency symbol/code mapping (remains the same)
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
        };
        public static string GetCodeFromSymbolOrCode(string symbolOrCode)
        {
            if (string.IsNullOrWhiteSpace(symbolOrCode)) return "USD";
            if (symbolOrCode.Length == 3 && symbolOrCode.All(char.IsLetter)) return symbolOrCode.ToUpperInvariant();
            if (s_symbolToCode.TryGetValue(symbolOrCode, out string? code)) return code;
            if (symbolOrCode == "$") return "USD";
            return symbolOrCode == "¥" ? "JPY" : symbolOrCode.ToUpperInvariant();
        }
    }
}