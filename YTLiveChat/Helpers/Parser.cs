using System.Globalization;
using System.Text.RegularExpressions;
using YTLiveChat.Contracts.Models; // Use the contract namespace
using YTLiveChat.Models; // Internal models namespace
using YTLiveChat.Models.Response; // Internal response models namespace
using Action = YTLiveChat.Models.Response.Action; // Explicitly use internal Action

namespace YTLiveChat.Helpers;

internal static partial class Parser
{
    // --- GetOptionsFromLivePage remains the same ---
    public static FetchOptions GetOptionsFromLivePage(string raw)
    {
        Match idResult = LiveIdRegex().Match(raw);
        string liveId = idResult.Success
            ? idResult.Groups[1].Value
            : throw new Exception("Live Stream canonical link not found");
        Match replayResult = ReplayRegex().Match(raw);
        if (replayResult.Success)
        {
            throw new Exception($"{liveId} is finished live (isReplay: true found)");
        }

        Match keyResult = ApiKeyRegex().Match(raw);
        string apiKey = keyResult.Success
            ? keyResult.Groups[1].Value
            : throw new Exception("API Key (INNERTUBE_API_KEY) not found");
        Match verResult = ClientVersionRegex().Match(raw);
        string clientVersion = verResult.Success
            ? verResult.Groups[1].Value
            : throw new Exception("Client Version (INNERTUBE_CONTEXT_CLIENT_VERSION) not found");
        Match continuationResult = ContinuationRegex().Match(raw);
        string continuation = continuationResult.Success
            ? continuationResult.Groups[1].Value
            : throw new Exception("Initial Continuation token not found");
        return new()
        {
            ApiKey = apiKey,
            ClientVersion = clientVersion,
            Continuation = continuation,
            LiveId = liveId,
        };
    }

    /// <summary>
    /// Extracts the relevant message renderer from a polymorphic item container.
    /// </summary>
    private static MessageRendererBase? GetBaseRenderer(AddChatItemActionItem? item) =>
        item switch
        {
            { LiveChatPaidMessageRenderer: not null } => item.LiveChatPaidMessageRenderer,
            { LiveChatPaidStickerRenderer: not null } => item.LiveChatPaidStickerRenderer,
            { LiveChatMembershipItemRenderer: not null } => item.LiveChatMembershipItemRenderer,
            { LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer: not null } =>
                item.LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer,
            { LiveChatSponsorshipsGiftRedemptionAnnouncementRenderer: not null } =>
                item.LiveChatSponsorshipsGiftRedemptionAnnouncementRenderer,
            { LiveChatTextMessageRenderer: not null } => item.LiveChatTextMessageRenderer,
            // PlaceholderItemRenderer doesn't inherit from Base and isn't handled here
            _ => null,
        };

    /// <summary>
    /// Converts a MessageRun (internal model) to a MessagePart (contract model).
    /// </summary>
    public static Contracts.Models.MessagePart ToMessagePart(this Models.Response.MessageRun run) =>
        run switch
        {
            Models.Response.MessageText { Text: not null } textRun => new Contracts.Models.TextPart
            {
                Text = textRun.Text,
            },
            Models.Response.MessageEmoji { Emoji: not null } emojiRun =>
                new Contracts.Models.EmojiPart
                {
                    Url = emojiRun.Emoji.Image?.Thumbnails?.LastOrDefault()?.Url ?? string.Empty,
                    IsCustomEmoji = emojiRun.Emoji.IsCustomEmoji,
                    Alt =
                        emojiRun.Emoji.Shortcuts?.FirstOrDefault()
                        ?? emojiRun.Emoji.SearchTerms?.FirstOrDefault(),
                    EmojiText = emojiRun.Emoji.IsCustomEmoji
                        ? (
                            emojiRun.Emoji.Shortcuts?.FirstOrDefault()
                            ?? emojiRun.Emoji.SearchTerms?.FirstOrDefault()
                            ?? $"[:{emojiRun.Emoji.EmojiId}:]"
                        )
                        : (emojiRun.Emoji.EmojiId ?? string.Empty),
                },
            // Fallback for unknown or null-property run types
            _ => new Contracts.Models.TextPart { Text = "[Unknown Message Part]" },
        };

    /// <summary>
    /// Converts an array of MessageRun (internal) to MessagePart[] (contract).
    /// </summary>
    public static Contracts.Models.MessagePart[] ToMessageParts(
        this IEnumerable<Models.Response.MessageRun>? runs
    ) => runs?.Select(r => r.ToMessagePart()).ToArray() ?? []; // Ensure call uses corrected ToMessagePart

    /// <summary>
    /// Parses the primary amount/currency string into numeric value and symbol/code.
    /// </summary>
    private static (decimal AmountValue, string Currency) ParseAmount(string? amountString)
    {
        if (string.IsNullOrWhiteSpace(amountString))
            return (0M, "USD");

        amountString = amountString!.Replace(",", "").Replace("\u00A0", " ").Trim(); // Normalize spaces/commas
        Match match = AmountCurrencyRegex().Match(amountString);
        if (match.Success)
        {
            string currencySymbolOrCode = match.Groups[1].Success
                ? match.Groups[1].Value
                : match.Groups[3].Value;
            string amountPart = match.Groups[2].Value;
            if (
                decimal.TryParse(
                    amountPart,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out decimal amountValue
                )
            )
            {
                string currencyCode = CurrencyHelper.GetCodeFromSymbolOrCode(currencySymbolOrCode);
                return (amountValue, currencyCode);
            }
        }
        // Fallback parsing attempt if regex fails
        if (
            decimal.TryParse(
                amountString,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out decimal fallbackAmount
            )
        )
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
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        if (action.AddChatItemAction?.Item == null)
            return null;

        AddChatItemActionItem item = action.AddChatItemAction.Item;
        MessageRendererBase? baseRenderer = GetBaseRenderer(item);

        if (baseRenderer == null)
        {
            // Handle placeholder/skipped types if needed
            return null;
        }

        // --- Author & Badges ---
        Contracts.Models.Author author = new() // Use contract type
        {
            Name = baseRenderer.AuthorName?.Text ?? "Unknown Author",
            ChannelId = baseRenderer.AuthorExternalChannelId ?? string.Empty,
            Thumbnail = baseRenderer.AuthorPhoto?.Thumbnails?.ToImage(
                baseRenderer.AuthorName?.Text
            ),
        };

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
                    authorBadge ??= new Contracts.Models.Badge // Use contract type
                    {
                        Thumbnail =
                            badgeContainer.LiveChatAuthorBadgeRenderer?.CustomThumbnail.Thumbnails?.ToImage(
                                badgeContainer.LiveChatAuthorBadgeRenderer?.Tooltip
                            ),
                        Label = badgeContainer.LiveChatAuthorBadgeRenderer?.Tooltip ?? "Member",
                    };
                }
                else // Standard badges
                {
                    switch (badgeContainer.LiveChatAuthorBadgeRenderer?.Icon?.IconType)
                    {
                        case "OWNER":
                            isOwner = true;
                            break;
                        case "VERIFIED":
                            isVerified = true;
                            break;
                        case "MODERATOR":
                            isModerator = true;
                            break;
                    }
                }
            }
        }

        author.Badge = authorBadge;

        // --- Message Content ---
        Contracts.Models.MessagePart[] messageParts = []; // Use contract type
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
            (decimal amountValue, string currency) = ParseAmount(
                paidRenderer.PurchaseAmountText?.Text
            );
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
                Sticker = null,
            };
        }
        else if (item.LiveChatPaidStickerRenderer != null)
        {
            LiveChatPaidStickerRenderer stickerRenderer = item.LiveChatPaidStickerRenderer;
            (decimal amountValue, string currency) = ParseAmount(
                stickerRenderer.PurchaseAmountText?.Text
            );
            superchatDetails = new Contracts.Models.Superchat // Use contract type
            {
                AmountString = stickerRenderer.PurchaseAmountText?.Text ?? string.Empty,
                AmountValue = amountValue,
                Currency = currency,
                BodyBackgroundColor = stickerRenderer.BackgroundColor.ToHex6Color() ?? "000000",
                AuthorNameTextColor = stickerRenderer.AuthorNameTextColor.ToHex6Color(),
                Sticker = stickerRenderer.Sticker?.Thumbnails?.ToImage(
                    stickerRenderer.Sticker?.Accessibility?.AccessibilityData?.Label
                ),
            };
        }

        // --- Membership Details ---
        MembershipDetails? membershipInfo = null;
        switch (baseRenderer)
        {
            case LiveChatMembershipItemRenderer membershipItem:
                string? levelNameFromBadge = baseRenderer
                    .AuthorBadges?.FirstOrDefault(b =>
                        b.LiveChatAuthorBadgeRenderer?.CustomThumbnail != null
                    )
                    ?.LiveChatAuthorBadgeRenderer?.Tooltip;
                membershipInfo = new()
                {
                    LevelName = levelNameFromBadge ?? "Member",
                    HeaderSubtext = membershipItem.HeaderSubtext?.Text,
                    HeaderPrimaryText = membershipItem
                        .HeaderPrimaryText?.Runs?.ToMessageParts()
                        ?.ToSimpleString(),
                };

                if (
                    membershipInfo.HeaderPrimaryText?.StartsWith(
                        "Welcome",
                        StringComparison.OrdinalIgnoreCase
                    ) == true
                    || membershipInfo.HeaderSubtext?.StartsWith(
                        "New member",
                        StringComparison.OrdinalIgnoreCase
                    ) == true
                )
                {
                    membershipInfo.EventType = Contracts.Models.MembershipEventType.New;
                    if (
                        membershipInfo.LevelName == "Member"
                        && membershipInfo.HeaderPrimaryText != null
                    )
                    {
                        Match levelMatch = NewMemberLevelRegex()
                            .Match(membershipInfo.HeaderPrimaryText);
                        if (levelMatch.Success)
                            membershipInfo.LevelName = levelMatch.Groups[1].Value.Trim();
                    }
                }
                else if (
                    membershipInfo.HeaderPrimaryText?.IndexOf(
                        "member for",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
                )
                {
                    if (membershipItem?.Message?.Runs != null)
                    {
                        messageParts = membershipItem.Message.Runs.ToMessageParts();
                    }

                    membershipInfo.EventType = Contracts.Models.MembershipEventType.Milestone;
                    Match monthsMatch = MilestoneMonthsRegex()
                        .Match(membershipInfo.HeaderPrimaryText);
                    if (
                        monthsMatch.Success
                        && int.TryParse(monthsMatch.Groups[1].Value, out int months)
                    )
                    {
                        membershipInfo.MilestoneMonths = months;
                    }
                }

                break;

            case LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer giftPurchase:
                levelNameFromBadge = baseRenderer
                    .AuthorBadges?.FirstOrDefault(b =>
                        b.LiveChatAuthorBadgeRenderer?.CustomThumbnail != null
                    )
                    ?.LiveChatAuthorBadgeRenderer?.Tooltip;

                // *** Update Author for Gift Purchase Event ***
                // The author of the *event* is the gifter, whose details are in the header
                LiveChatSponsorshipsHeaderRenderer? gifterHeader = giftPurchase
                    .Header
                    ?.LiveChatSponsorshipsHeaderRenderer;
                if (gifterHeader != null)
                {
                    author.Name = gifterHeader.AuthorName?.Text ?? author.Name;
                    author.Thumbnail = gifterHeader.AuthorPhoto?.Thumbnails.ToImage(author.Name);

                    // Re-evaluate badges based on the gifter's header info
                    isMembershipBadge = false; // Reset badge flags for gifter
                    isVerified = false;
                    isOwner = false;
                    isModerator = false;
                    authorBadge = null; // Reset badge object

                    if (gifterHeader.AuthorBadges != null)
                    {
                        foreach (AuthorBadgeContainer badgeContainer in gifterHeader.AuthorBadges)
                        {
                            if (badgeContainer.LiveChatAuthorBadgeRenderer?.CustomThumbnail != null) // Membership badges
                            {
                                isMembershipBadge = true;
                                authorBadge ??= new Contracts.Models.Badge // Use contract type
                                {
                                    Thumbnail =
                                        badgeContainer.LiveChatAuthorBadgeRenderer?.CustomThumbnail.Thumbnails?.ToImage(
                                            badgeContainer.LiveChatAuthorBadgeRenderer?.Tooltip
                                        ),
                                    Label =
                                        badgeContainer.LiveChatAuthorBadgeRenderer?.Tooltip
                                        ?? "Member",
                                };
                            }
                            else // Standard badges
                            {
                                switch (badgeContainer.LiveChatAuthorBadgeRenderer?.Icon?.IconType)
                                {
                                    case "OWNER":
                                        isOwner = true;
                                        break;
                                    case "VERIFIED":
                                        isVerified = true;
                                        break;
                                    case "MODERATOR":
                                        isModerator = true;
                                        break;
                                }
                            }
                        }
                    }

                    author.Badge = authorBadge; // Assign gifter's badge
                }

                membershipInfo = new()
                {
                    // Use the gifter's level if available from badge, otherwise default
                    LevelName = levelNameFromBadge ?? "Member",
                    EventType = Contracts.Models.MembershipEventType.GiftPurchase,
                    HeaderPrimaryText = gifterHeader
                        ?.PrimaryText?.Runs?.ToMessageParts()
                        ?.ToSimpleString(),
                    GifterUsername = author.Name, // We just populated author with gifter info
                };

                // *** Updated Gift Count Parsing Logic ***
                if (membershipInfo.HeaderPrimaryText != null)
                {
                    int giftCount = 0; // Default to 0
                    // Try the new "Sent X ..." format first
                    Match giftMatch = GiftedSentCountRegex()
                        .Match(membershipInfo.HeaderPrimaryText);

                    if (!giftMatch.Success)
                    {
                        // If new format fails, try the old "gifted X ..." format
                        giftMatch = GiftedCountRegex().Match(membershipInfo.HeaderPrimaryText);
                    }

                    // Process if either regex matched
                    if (giftMatch.Success)
                    {
                        string countStr = giftMatch.Groups[1].Value;
                        if (countStr.Equals("a", StringComparison.OrdinalIgnoreCase))
                        {
                            giftCount = 1;
                        }
                        else if (int.TryParse(countStr, out int count))
                        {
                            giftCount = count;
                        }
                    }
                    else if ( // Fallback for "a membership gift" or similar phrasing if regexes fail
                        membershipInfo.HeaderPrimaryText.IndexOf(
                            "gift", // Keep it general
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0
                    )
                    {
                        giftCount = 1; // Default to 1 if text implies a single gift
                    }

                    membershipInfo.GiftCount = giftCount; // Assign the determined count
                }

                break;

            case LiveChatSponsorshipsGiftRedemptionAnnouncementRenderer giftRedemption:
                levelNameFromBadge = baseRenderer
                    .AuthorBadges?.FirstOrDefault(b =>
                        b.LiveChatAuthorBadgeRenderer?.CustomThumbnail != null
                    )
                    ?.LiveChatAuthorBadgeRenderer?.Tooltip;

                // Author of this event *is* the recipient (already parsed)
                membershipInfo = new()
                {
                    LevelName = levelNameFromBadge ?? "Member", // Recipient's level
                    EventType = Contracts.Models.MembershipEventType.GiftRedemption,
                    HeaderPrimaryText = giftRedemption
                        .Message?.Runs?.ToMessageParts()
                        ?.ToSimpleString(), // Usually "Welcome!" or similar
                    RecipientUsername = author.Name, // Author of this event is the recipient
                };

                // Attempt to extract gifter name from the message runs (often the last part)
                MessageText? relevantText = (MessageText?)(
                    giftRedemption.Message?.Runs?.LastOrDefault(r => r is MessageText)
                );
                string? gifterName = relevantText?.Text?.Trim(); // Get first text part

                if (
                    giftRedemption.Message?.Runs?.LastOrDefault(r => r is MessageText)
                    is MessageText mt
                )
                {
                    gifterName = mt.Text?.Trim();
                    // More robust check: sometimes it's "[GifterName] gifted you..."
                    Match gifterMatch = GiftRedemptionGifterRegex()
                        .Match(membershipInfo.HeaderPrimaryText ?? "");
                    if (gifterMatch.Success)
                    {
                        gifterName = gifterMatch.Groups[1].Value.Trim();
                    }
                }

                membershipInfo.GifterUsername = gifterName;
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
            IsVerified = isVerified,
            IsOwner = isOwner,
            IsModerator = isModerator,
            // Determine IsMembership based on if it's a membership event OR the author has a member badge
            IsMembership = membershipInfo != null || isMembershipBadge,
        };
        return chatItem;
    }

    /// <summary>
    /// Converts an array of image thumbnails (internal model) to an ImagePart (contract model).
    /// </summary>
    public static Contracts.Models.ImagePart? ToImage(
        this List<Thumbnail>? thumbnails,
        string? alt = null
    ) // Return contract type
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
    public static (
        List<Contracts.Models.ChatItem> Items,
        string? Continuation
    ) ParseLiveChatResponse(LiveChatResponse? response) // Return contract type
    {
        List<Contracts.Models.ChatItem> items = []; // Use contract type
        string? continuationToken = null;

        if (response?.ContinuationContents?.LiveChatContinuation?.Actions != null)
        {
            items =
            [
                .. response
                    .ContinuationContents.LiveChatContinuation.Actions.Select(a => a.ToChatItem()) // Uses the corrected ToChatItem
                    .WhereNotNull(),
            ];
        }

        Continuation? nextContinuation =
            response?.ContinuationContents?.LiveChatContinuation?.Continuations?.FirstOrDefault();
        continuationToken =
            nextContinuation?.InvalidationContinuationData?.Continuation
            ?? nextContinuation?.TimedContinuationData?.Continuation;

        if (string.IsNullOrEmpty(continuationToken))
        {
            continuationToken = response?.InvalidationContinuationData?.Continuation;
        }

        return (items, continuationToken);
    }

    #region Regex Definitions
#if NET7_0_OR_GREATER
    [GeneratedRegex(
        "<link rel=\"canonical\" href=\"https:\\/\\/www\\.youtube\\.com\\/watch\\?v=([^\"]+)\">",
        RegexOptions.Compiled
    )]
    private static partial Regex LiveIdRegex();

    [GeneratedRegex("\"isReplay\":\\s*(true)", RegexOptions.Compiled)]
    private static partial Regex ReplayRegex();

    [GeneratedRegex("\"INNERTUBE_API_KEY\":\\s*\"([^\"]*)\"", RegexOptions.Compiled)]
    private static partial Regex ApiKeyRegex();

    [GeneratedRegex("\"INNERTUBE_CONTEXT_CLIENT_VERSION\":\\s*\"([^\"]*)\"", RegexOptions.Compiled)]
    private static partial Regex ClientVersionRegex();

    [GeneratedRegex("\"continuation\":\\s*\"([^\"]*)\"", RegexOptions.Compiled)]
    private static partial Regex ContinuationRegex();

    [GeneratedRegex(
        @"([€$£¥])?\s*([\d.,]+)\s*(?:([A-Z]{3}))?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    )]
    private static partial Regex AmountCurrencyRegex();

    [GeneratedRegex(@"member for (\d+) months?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MilestoneMonthsRegex();

    [GeneratedRegex(
        @"Gifted (\d+|a) .*? membership",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    )] // Handle "a membership"
    private static partial Regex GiftedCountRegex();

    [GeneratedRegex(
        @"^Sent (\d+) .*? gift memberships?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    )] // Match start, handle optional 's'
    private static partial Regex GiftedSentCountRegex();

    [GeneratedRegex(
        @"Welcome to (.*?) membership",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    )]
    private static partial Regex NewMemberLevelRegex();

    // Regex to extract gifter name from redemption message like "GifterName gifted you..."
    [GeneratedRegex(@"^(.*?) gifted you", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GiftRedemptionGifterRegex();
#else
    // Fallback for .NET Standard 2.1
    private static readonly Regex _liveIdRegex = new(
        "<link rel=\"canonical\" href=\"https:\\/\\/www\\.youtube\\.com\\/watch\\?v=([^\"]+)\">",
        RegexOptions.Compiled
    );

    private static Regex LiveIdRegex() => _liveIdRegex;

    private static readonly Regex _replayRegex = new(
        "\"isReplay\":\\s*(true)",
        RegexOptions.Compiled
    );

    private static Regex ReplayRegex() => _replayRegex;

    private static readonly Regex _apiKeyRegex = new(
        "\"INNERTUBE_API_KEY\":\\s*\"([^\"]*)\"",
        RegexOptions.Compiled
    );

    private static Regex ApiKeyRegex() => _apiKeyRegex;

    private static readonly Regex _clientVersionRegex = new(
        "\"INNERTUBE_CONTEXT_CLIENT_VERSION\":\\s*\"([^\"]*)\"",
        RegexOptions.Compiled
    );

    private static Regex ClientVersionRegex() => _clientVersionRegex;

    private static readonly Regex _continuationRegex = new(
        "\"continuation\":\\s*\"([^\"]*)\"",
        RegexOptions.Compiled
    );

    private static Regex ContinuationRegex() => _continuationRegex;

    private static readonly Regex _amountCurrencyRegex = new(
        @"([€$£¥])?\s*([\d.,]+)\s*(?:([A-Z]{3}))?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static Regex AmountCurrencyRegex() => _amountCurrencyRegex;

    private static readonly Regex _milestoneMonthsRegex = new(
        @"member for (\d+) months?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static Regex MilestoneMonthsRegex() => _milestoneMonthsRegex;

    private static readonly Regex _giftedCountRegex = new(
        @"Gifted (\d+|a) .*? membership",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static Regex GiftedCountRegex() => _giftedCountRegex;

    private static readonly Regex _giftedSentCountRegex = new(
        @"^Sent (\d+) .*? gift memberships?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static Regex GiftedSentCountRegex() => _giftedSentCountRegex;

    private static readonly Regex _newMemberLevelRegex = new(
        @"Welcome to (.*?) membership",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static Regex NewMemberLevelRegex() => _newMemberLevelRegex;

    private static readonly Regex _giftRedemptionGifterRegex = new(
        @"^(.*?) gifted you",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static Regex GiftRedemptionGifterRegex() => _giftRedemptionGifterRegex;
#endif

    #endregion

    // Helper extension method to convert message parts back to a simple string
    private static string ToSimpleString(this IEnumerable<Contracts.Models.MessagePart>? parts) // Use contract type
    {
        return string.Join(
            "",
            parts?.Select(p =>
                p switch
                {
                    Contracts.Models.TextPart tp => tp.Text,
                    Contracts.Models.EmojiPart ep => ep.EmojiText,
                    _ => "",
                }
            ) ?? []
        );
    }

    // Helper class for currency symbol/code mapping (remains the same)
    private static class CurrencyHelper
    {
        private static readonly Dictionary<string, string> s_symbolToCode = new(
            StringComparer.OrdinalIgnoreCase
        )
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
            // Add more as needed
        };

        public static string GetCodeFromSymbolOrCode(string symbolOrCode)
        {
            if (string.IsNullOrWhiteSpace(symbolOrCode))
                return "USD"; // Default

            // Check if it's already a 3-letter code
            if (symbolOrCode.Length == 3 && symbolOrCode.All(char.IsLetter))
            {
                return symbolOrCode.ToUpperInvariant();
            }

            // Check symbol map
            if (s_symbolToCode.TryGetValue(symbolOrCode, out string? code))
            {
                return code;
            }

            // Specific common fallbacks if needed (already covered above, but explicit doesn't hurt)
            if (symbolOrCode == "$")
                return "USD";
            if (symbolOrCode == "¥")
                return "JPY"; // Could be CNY too, JPY is often default in YT context

            // Final fallback: return the input uppercase (maybe it's a less common code)
            return symbolOrCode.ToUpperInvariant();
        }
    }
}
