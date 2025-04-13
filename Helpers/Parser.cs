using System.Globalization;
using System.Text.RegularExpressions;
using YTLiveChat.Contracts.Models;
using YTLiveChat.Models;
using YTLiveChat.Models.Response;

namespace YTLiveChat.Helpers;

internal static partial class Parser
{
    public static FetchOptions GetOptionsFromLivePage(string raw)
    {
        Match idResult = LiveIdRegex().Match(raw);
        string liveId = idResult.Success ? idResult.Groups[1].Value : throw new Exception("Live Stream was not found");

        Match replayResult = ReplayRegex().Match(raw);
        if (replayResult.Success)
        {
            throw new Exception($"{liveId} is finished live");
        }

        Match keyResult = ApiKeyRegex().Match(raw);
        string apiKey = keyResult.Success ? keyResult.Groups[1].Value : throw new Exception("API Key was not found");

        Match verResult = ClientVersionRegex().Match(raw);
        string clientVersion = verResult.Success ? verResult.Groups[1].Value : throw new Exception("Client Version was not found");

        Match continuationResult = ContinuationRegex().Match(raw);
        string continuation = continuationResult.Success ? continuationResult.Groups[1].Value : throw new Exception("Continuation was not found");

        return new()
        {
            ApiKey = apiKey,
            ClientVersion = clientVersion,
            Continuation = continuation,
            LiveId = liveId
        };
    }


    public static MessageRendererBase? GetMessageRenderer(AddChatItemAction.ItemObj item)
    {
        // Allow null item gracefully
        return item?.LiveChatTextMessageRenderer ??
               item?.LiveChatPaidMessageRenderer ?? // Check Paid Message before Text Message
               item?.LiveChatPaidStickerRenderer ??
               item?.LiveChatMembershipItemRenderer ??
               (MessageRendererBase?)null;
    }

    public static MessagePart ToMessagePart(this MessageRun run)
    {
        if (run is MessageText text)
        {
            return new TextPart { Text = text.Text };
        }

        MessageEmoji? emoji = run as MessageEmoji;

        bool isCustom = emoji?.Emoji.IsCustomEmoji ?? false;
        string? altText = emoji?.Emoji.Shortcuts.FirstOrDefault();

        return new EmojiPart
        {
            Url = emoji?.Emoji.Image?.Thumbnails.FirstOrDefault()?.Url ?? string.Empty,
            IsCustomEmoji = isCustom,
            Alt = altText,
            EmojiText = (isCustom ? altText : emoji?.Emoji.EmojiId) ?? string.Empty
        };
    }

    public static MessagePart[] ToMessagePart(this MessageRun[] runs) => [.. runs.Select(r => r.ToMessagePart())];

    /// <summary>
    /// Converts a raw YouTube Action into a StreamWeaver ChatItem.
    /// </summary>
    public static ChatItem? ToChatItem(this YTAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (action.AddChatItemAction == null)
        {
            return null;
        }

        MessageRendererBase? renderer = GetMessageRenderer(action.AddChatItemAction.Item);
        if (renderer == null)
        {
            return null;
        }

        // --- Author & Badges ---
        Author author = new()
        {
            Name = renderer.AuthorName?.SimpleText ?? string.Empty,
            ChannelId = renderer.AuthorExternalChannelId,
            Thumbnail = renderer.AuthorPhoto.Thumbnails.ToImage(renderer.AuthorName?.SimpleText ?? string.Empty)
        };

        bool isMembership = false;
        bool isVerified = false;
        bool isOwner = false;
        bool isModerator = false;
        AuthorBadgeRenderer? membershipBadgeRenderer = null; // Store for membership level parsing

        if (renderer.AuthorBadges != null && renderer.AuthorBadges.Length > 0)
        {
            foreach (AuthorBadge item in renderer.AuthorBadges)
            {
                AuthorBadgeRenderer badge = item.LiveChatAuthorBadgeRenderer;
                if (badge.CustomThumbnail != null) // Membership badges have custom thumbnails
                {
                    isMembership = true;
                    membershipBadgeRenderer = badge; // Use this later for level name
                    author.Badge = new Badge // Assign the first custom badge found as the primary Author.Badge
                    {
                        Thumbnail = badge.CustomThumbnail.Thumbnails.ToImage(badge.Tooltip),
                        Label = badge.Tooltip
                    };
                }
                else // Built-in badges (Owner, Verified, Mod)
                {
                    switch (badge.Icon?.IconType)
                    {
                        case "OWNER": isOwner = true; break;
                        case "VERIFIED": isVerified = true; break;
                        case "MODERATOR": isModerator = true; break;
                    }
                }
            }
        }

        // --- Event Specific Details ---
        Superchat? superchatDetails = null;
        MembershipDetails? membershipInfo = null;

        switch (renderer)
        {
            case LiveChatPaidStickerRenderer stickerRenderer:
                (decimal stickerAmountValue, string stickerCurrency) = ParseAmount(stickerRenderer.PurchaseAmountText.SimpleText ?? string.Empty);
                superchatDetails = new Superchat
                {
                    AmountString = stickerRenderer.PurchaseAmountText.SimpleText ?? string.Empty,
                    AmountValue = stickerAmountValue,
                    Currency = stickerCurrency,
                    Color = stickerRenderer.BackgroundColor.ToHex6Color(), // Primary color is background for stickers
                    AuthorNameTextColor = stickerRenderer.AuthorNameTextColor.ToHex6Color(),
                    Sticker = stickerRenderer.Sticker.Thumbnails.ToImage(stickerRenderer.Sticker.Accessibility.AccessibilityData.Label)
                };
                break;

            case LiveChatPaidMessageRenderer paidMessageRenderer:
                (decimal scAmountValue, string scCurrency) = ParseAmount(paidMessageRenderer.PurchaseAmountText.SimpleText ?? string.Empty);
                superchatDetails = new Superchat
                {
                    AmountString = paidMessageRenderer.PurchaseAmountText.SimpleText ?? string.Empty,
                    AmountValue = scAmountValue,
                    Currency = scCurrency,
                    Color = paidMessageRenderer.BodyBackgroundColor.ToHex6Color(), // Primary color is body background
                    HeaderBackgroundColor = paidMessageRenderer.HeaderBackgroundColor.ToHex6Color(),
                    HeaderTextColor = paidMessageRenderer.HeaderTextColor.ToHex6Color(),
                    BodyTextColor = paidMessageRenderer.BodyTextColor.ToHex6Color(),
                    AuthorNameTextColor = paidMessageRenderer.AuthorNameTextColor.ToHex6Color(),
                    Sticker = null
                };
                break;

            case LiveChatMembershipItemRenderer membershipItemRenderer:
                // Assume this renderer always indicates some form of membership event
                string headerText = string.Join("", membershipItemRenderer.HeaderSubtext.Runs.Select(r => r is MessageText mt ? mt.Text : ""));
                membershipInfo = ParseMembershipInfo(headerText, membershipBadgeRenderer);
                // Gifter username might be the author of *this* item if EventType is GiftedMemberships
                if (membershipInfo.EventType == MembershipEventType.GiftedMemberships)
                {
                    membershipInfo.GifterUsername = author.Name;
                }

                break;
        }

        // --- Message Parsing ---
        // Get message runs based on renderer type
        MessageRun[]? messageRuns = renderer switch
        {
            LiveChatTextMessageRenderer textRenderer => textRenderer.Message?.Runs, // Standard/Paid message
            LiveChatMembershipItemRenderer memberRenderer => memberRenderer.HeaderSubtext?.Runs, // Use header for membership system messages
            LiveChatPaidStickerRenderer => null, // Stickers don't have a separate message body here
            _ => null
        };
        MessagePart[] parsedMessage = messageRuns != null ? messageRuns.ToMessagePart() : [];

        // --- Construct Final ChatItem ---
        ChatItem chatItem = new()
        {
            Id = renderer.Id,
            Timestamp = DateTimeOffset.UtcNow, // TODO: Consider parsing `renderer.TimestampUsec` if accuracy needed
            Author = author,
            Message = parsedMessage,
            Superchat = superchatDetails,
            MembershipDetails = membershipInfo, // Assign parsed details
            IsMembership = isMembership, // Based on badge presence
            IsVerified = isVerified,
            IsOwner = isOwner,
            IsModerator = isModerator
        };

        return chatItem;
    }

    public static ImagePart? ToImage(this Thumbnail[] thumbnails, string? alt = null)
    {
        // Prefer larger thumbnails if available (usually last in array)
        Thumbnail? thumbnail = thumbnails.LastOrDefault();
        return thumbnail == null
            ? null
            : new ImagePart
            {
                Url = thumbnail.Url,
                Alt = alt,
            };
    }

    public static (List<ChatItem> Items, string Continuation) ParseGetLiveChatResponse(GetLiveChatResponse? response)
    {
        List<ChatItem> items = [];

        if (response != null)
        {
            items = [.. response.ContinuationContents.LiveChatContinuation.Actions
                .Where(a => a.AddChatItemAction != null)
                .Select(a => a.ToChatItem()) // Uses the updated ToChatItem method
                .WhereNotNull()
                ];
        }

        Continuation? continuationData = response?.ContinuationContents.LiveChatContinuation.Continuations.FirstOrDefault();
        string continuation = "";

        if (continuationData?.InvalidationContinuationData != null)
        {
            continuation = continuationData.InvalidationContinuationData.Continuation;
        }
        else if (continuationData?.TimedContinuationData != null)
        {
            continuation = continuationData.TimedContinuationData.Continuation;
        }

        return (items, continuation);
    }

    /// <summary>
    /// Parses the primary amount/currency string into numeric value and symbol/code.
    /// </summary>
    private static (decimal AmountValue, string Currency) ParseAmount(string amountString)
    {
        if (string.IsNullOrWhiteSpace(amountString))
        {
            return (0M, "USD"); // Default on error
        }

        // Remove common formatting like commas
        amountString = amountString.Replace(",", "");

        // Use regex to capture currency symbols/codes and the numeric value
        Match match = AmountCurrencyRegex().Match(amountString);

        if (match.Success)
        {
            string currencySymbolOrCode = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[3].Value; // Symbol first, then code
            string amountPart = match.Groups[2].Value;

            if (decimal.TryParse(amountPart, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amountValue))
            {
                // Basic symbol mapping - enhance if needed
                string currencyCode = currencySymbolOrCode switch
                {
                    "€" => "EUR",
                    "£" => "GBP",
                    "¥" => "JPY", // Could be CNY as well, JPY more common on YT?
                    "$" => "USD", // Default $ to USD, could be CAD, AUD etc.
                    _ => currencySymbolOrCode // Assume it's already a code (e.g., "CAD", "AUD")
                };
                return (amountValue, currencyCode.ToUpperInvariant());
            }
        }

        // Fallback if regex fails
        if (decimal.TryParse(amountString, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal fallbackAmount))
        {
            return (fallbackAmount, "USD"); // Default currency if only number found
        }

        return (0M, "USD"); // Final fallback
    }

    /// <summary>
    /// Parses membership header text to determine event type and details.
    /// </summary>
    private static MembershipDetails ParseMembershipInfo(string headerText, AuthorBadgeRenderer? authorBadgeRenderer)
    {
        MembershipDetails details = new()
        {
            EventType = MembershipEventType.Unknown, // Default
            HeaderPrimaryText = headerText,
            LevelName = authorBadgeRenderer?.Tooltip // Start with badge tooltip for level name
        };

        // Heuristics based on common message formats
        if (headerText.StartsWith("Welcome", StringComparison.OrdinalIgnoreCase))
        {
            details.EventType = MembershipEventType.New;
            if (string.IsNullOrEmpty(details.LevelName)) // Infer level from text if badge didn't provide
            {
                Match levelMatch = NewMemberLevelRegex().Match(headerText);
                if (levelMatch.Success) details.LevelName = levelMatch.Groups[1].Value.Trim();
            }
        }
        else if (headerText.Contains("member for", StringComparison.OrdinalIgnoreCase))
        {
            details.EventType = MembershipEventType.Milestone;
            Match monthsMatch = MilestoneMonthsRegex().Match(headerText);
            if (monthsMatch.Success && int.TryParse(monthsMatch.Groups[1].Value, out int months))
            {
                details.MilestoneMonths = months;
            }

            if (string.IsNullOrEmpty(details.LevelName)) // Infer level from badge if available
            {
                details.LevelName = authorBadgeRenderer?.Tooltip; // Might just be "Member"
            }
        }
        else if (headerText.Contains("gifted", StringComparison.OrdinalIgnoreCase))
        {
            details.EventType = MembershipEventType.GiftedMemberships;
            Match giftMatch = GiftedCountRegex().Match(headerText);
            if (giftMatch.Success && int.TryParse(giftMatch.Groups[1].Value, out int count))
            {
                details.GiftCount = count;
            }
            else
            {
                details.GiftCount = 1; // Assume 1 if count not parsed
            }

            if (string.IsNullOrEmpty(details.LevelName)) // Infer level from text
            {
                Match levelMatch = GiftedLevelRegex().Match(headerText);
                if (levelMatch.Success) details.LevelName = levelMatch.Groups[1].Value.Trim();
            }
        }
        // TODO: Refine GiftedMemberships parsing and potentially GifterUsername if possible from other fields

        details.LevelName ??= "Member"; // Default level name

        return details;
    }

    // --- Regex Definitions ---
    [GeneratedRegex("<link rel=\"canonical\" href=\"https:\\/\\/www\\.youtube\\.com\\/watch\\?v=([^\"]+)\">")]
    private static partial Regex LiveIdRegex();

    [GeneratedRegex("\"isReplay\":\\s*(true)")]
    private static partial Regex ReplayRegex();

    [GeneratedRegex("\"INNERTUBE_API_KEY\":\\s*\"([^\"]*)\"")]
    private static partial Regex ApiKeyRegex();

    [GeneratedRegex("\"clientVersion\":\\s*\"([^\"]*)\"")]
    private static partial Regex ClientVersionRegex();

    [GeneratedRegex("\"continuation\":\\s*\"([^\"]*)\"")]
    private static partial Regex ContinuationRegex();

    // Regex to capture currency symbol OR code and amount value
    [GeneratedRegex(@"([€$£¥])?\s*([\d.,]+)(?:\s*([A-Z]{3}))?", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex AmountCurrencyRegex();

    // Regex for Membership Milestone parsing
    [GeneratedRegex(@"member for (\d+) months", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MilestoneMonthsRegex();

    // Regex for parsing gifted membership count
    [GeneratedRegex(@"gifted (\d+) membership", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GiftedCountRegex();

    // Regex for parsing gifted membership level
    [GeneratedRegex(@"gifted.*to.*as (.*?) member", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GiftedLevelRegex();

    // Regex for parsing new member level
    [GeneratedRegex(@"Welcome to (.*?) membership", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NewMemberLevelRegex();
}
