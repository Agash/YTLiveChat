using System.Text.RegularExpressions;
using System.Text.Json;
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
        string liveId = TryExtractLiveId(raw) ?? throw new Exception("Live Stream ID not found");
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

    private static string? TryExtractLiveId(string raw)
    {
        Match canonicalLinkResult = LiveIdRegex().Match(raw);
        if (canonicalLinkResult.Success)
        {
            return canonicalLinkResult.Groups[1].Value;
        }

        Match canonicalBaseUrlResult = CanonicalBaseUrlLiveIdRegex().Match(raw);
        if (canonicalBaseUrlResult.Success)
        {
            return canonicalBaseUrlResult.Groups[1].Value;
        }

        Match chatTopicResult = ChatTopicLiveIdRegex().Match(raw);
        if (chatTopicResult.Success)
        {
            return chatTopicResult.Groups[1].Value;
        }

        Match videoDetailsResult = VideoDetailsLiveIdRegex().Match(raw);
        if (videoDetailsResult.Success)
        {
            return videoDetailsResult.Groups[1].Value;
        }

        return null;
    }

    /// <summary>
    /// Best-effort check to determine whether the fetched watch page represents an actively broadcasting livestream.
    /// Returns false for replay/upcoming pages and when no clear live-state marker can be found.
    /// </summary>
    public static bool IsActivelyBroadcastingLivePage(string raw)
    {
        if (ReplayRegex().IsMatch(raw))
        {
            return false;
        }

        Match upcomingResult = IsUpcomingRegex().Match(raw);
        if (
            upcomingResult.Success
            && upcomingResult.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        Match isLiveNowResult = IsLiveNowRegex().Match(raw);
        if (isLiveNowResult.Success)
        {
            return isLiveNowResult
                .Groups[1]
                .Value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        Match isLiveResult = IsLiveRegex().Match(raw);
        return isLiveResult.Success
            && isLiveResult.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts stream candidates from a channel streams page.
    /// </summary>
    public static IReadOnlyList<StreamPageCandidate> ExtractStreamCandidatesFromStreamsPage(string raw)
    {
        // Prefer structured JSON traversal when available; regex extraction remains as fallback.
        IReadOnlyList<StreamPageCandidate> jsonCandidates = ExtractStreamCandidatesFromStreamsPageJson(
            raw
        );
        if (jsonCandidates.Count > 0)
        {
            return jsonCandidates;
        }

        Dictionary<string, StreamPageCandidate> byId = new(StringComparer.Ordinal);
        List<string> order = [];

        MatchCollection idMatches = StreamsVideoIdRegex().Matches(raw);
        for (int i = 0; i < idMatches.Count; i++)
        {
            Match idMatch = idMatches[i];
            if (!idMatch.Success)
            {
                continue;
            }

            string liveId = idMatch.Groups[1].Value;
            int start = idMatch.Index;
            int end = i + 1 < idMatches.Count ? idMatches[i + 1].Index : raw.Length;
            int windowLength = end - start;
            if (windowLength <= 0 || end > raw.Length)
            {
                continue;
            }

            string window = raw.Substring(start, windowLength);
            string? overlayStyle = TryGetGroupValue(StreamsOverlayStyleRegex().Match(window), 1);
            string? viewCountText = TryExtractViewCountText(window);
            long? upcomingStartTime = null;
            string? startRaw = TryGetGroupValue(StreamsUpcomingStartTimeRegex().Match(window), 1);
            if (long.TryParse(startRaw, out long parsedStart))
            {
                upcomingStartTime = parsedStart;
            }

            string viewCount = viewCountText ?? string.Empty;
            bool isLive = string.Equals(overlayStyle, "LIVE", StringComparison.OrdinalIgnoreCase)
                || viewCount.IndexOf("watching", StringComparison.OrdinalIgnoreCase) >= 0;

            bool isUpcoming =
                string.Equals(overlayStyle, "UPCOMING", StringComparison.OrdinalIgnoreCase)
                || upcomingStartTime.HasValue
                || viewCount.IndexOf("waiting", StringComparison.OrdinalIgnoreCase) >= 0;

            StreamPageCandidate candidate = new(
                liveId,
                isLive,
                isUpcoming,
                upcomingStartTime,
                viewCountText
            );

            if (!byId.ContainsKey(liveId))
            {
                byId[liveId] = candidate;
                order.Add(liveId);
                continue;
            }

            StreamPageCandidate existing = byId[liveId];
            byId[liveId] = new(
                existing.LiveId,
                existing.IsLive || candidate.IsLive,
                existing.IsUpcoming || candidate.IsUpcoming,
                MergeUpcomingStartTime(existing.UpcomingStartTime, candidate.UpcomingStartTime),
                existing.ViewCountText ?? candidate.ViewCountText
            );
        }

        List<StreamPageCandidate> result = new(order.Count);
        foreach (string id in order)
        {
            result.Add(byId[id]);
        }

        return result;
    }

    private static IReadOnlyList<StreamPageCandidate> ExtractStreamCandidatesFromStreamsPageJson(
        string raw
    )
    {
        string? json = ExtractJsonObjectAfterMarker(raw, "ytInitialData");
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(json!);
            Dictionary<string, StreamPageCandidate> byId = new(StringComparer.Ordinal);
            List<string> order = [];

            CollectVideoRenderers(doc.RootElement, byId, order);

            List<StreamPageCandidate> result = new(order.Count);
            foreach (string id in order)
            {
                result.Add(byId[id]);
            }

            return result;
        }
        catch
        {
            return [];
        }
    }

    private static void CollectVideoRenderers(
        JsonElement element,
        Dictionary<string, StreamPageCandidate> byId,
        List<string> order
    )
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (property.NameEquals("videoRenderer"))
                    {
                        AddStreamCandidateFromVideoRenderer(property.Value, byId, order);
                    }

                    CollectVideoRenderers(property.Value, byId, order);
                }
                break;
            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray())
                {
                    CollectVideoRenderers(item, byId, order);
                }
                break;
        }
    }

    private static void AddStreamCandidateFromVideoRenderer(
        JsonElement videoRenderer,
        Dictionary<string, StreamPageCandidate> byId,
        List<string> order
    )
    {
        if (
            !videoRenderer.TryGetProperty("videoId", out JsonElement videoIdElement)
            || videoIdElement.ValueKind != JsonValueKind.String
        )
        {
            return;
        }

        string? liveId = videoIdElement.GetString();
        if (string.IsNullOrWhiteSpace(liveId))
        {
            return;
        }
        string liveIdNonNull = liveId!;

        string? overlayStyle = ExtractOverlayStyle(videoRenderer);
        string? viewCountText = ExtractViewCountText(videoRenderer);
        long? upcomingStartTime = ExtractUpcomingStartTime(videoRenderer);

        bool isLive = string.Equals(overlayStyle, "LIVE", StringComparison.OrdinalIgnoreCase)
            || (
                viewCountText?.IndexOf("watching", StringComparison.OrdinalIgnoreCase) >= 0
            );
        bool isUpcoming = string.Equals(overlayStyle, "UPCOMING", StringComparison.OrdinalIgnoreCase)
            || upcomingStartTime.HasValue
            || (viewCountText?.IndexOf("waiting", StringComparison.OrdinalIgnoreCase) >= 0);

        StreamPageCandidate candidate = new(
            liveIdNonNull,
            isLive,
            isUpcoming,
            upcomingStartTime,
            viewCountText
        );

        if (!byId.ContainsKey(liveIdNonNull))
        {
            byId[liveIdNonNull] = candidate;
            order.Add(liveIdNonNull);
            return;
        }

        StreamPageCandidate existing = byId[liveIdNonNull];
        byId[liveIdNonNull] = new(
            existing.LiveId,
            existing.IsLive || candidate.IsLive,
            existing.IsUpcoming || candidate.IsUpcoming,
            MergeUpcomingStartTime(existing.UpcomingStartTime, candidate.UpcomingStartTime),
            existing.ViewCountText ?? candidate.ViewCountText
        );
    }

    private static string? ExtractOverlayStyle(JsonElement videoRenderer)
    {
        if (
            !videoRenderer.TryGetProperty("thumbnailOverlays", out JsonElement overlays)
            || overlays.ValueKind != JsonValueKind.Array
        )
        {
            return null;
        }

        foreach (JsonElement overlay in overlays.EnumerateArray())
        {
            if (
                overlay.TryGetProperty(
                    "thumbnailOverlayTimeStatusRenderer",
                    out JsonElement statusRenderer
                )
                && statusRenderer.TryGetProperty("style", out JsonElement styleElement)
                && styleElement.ValueKind == JsonValueKind.String
            )
            {
                return styleElement.GetString();
            }
        }

        return null;
    }

    /// <summary>
    /// Best-effort classification for inaccessible live pages (members-only / login-required / unplayable).
    /// Returns null when no known inaccessible marker is found.
    /// </summary>
    public static string? DetectInaccessibleLiveReason(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        bool hasUnplayableStatus =
            raw.IndexOf("\"playabilityStatus\":{\"status\":\"UNPLAYABLE\"", StringComparison.Ordinal)
            >= 0
            || raw.IndexOf("\"status\":\"UNPLAYABLE\"", StringComparison.Ordinal) >= 0;
        bool hasMembersOnlyMarker =
            raw.IndexOf("playerLegacyDesktopYpcOfferRenderer", StringComparison.OrdinalIgnoreCase)
            >= 0
            || raw.IndexOf("BADGE_STYLE_TYPE_MEMBERS_ONLY", StringComparison.OrdinalIgnoreCase) >= 0
            || raw.IndexOf("SPONSORSHIP_STAR", StringComparison.OrdinalIgnoreCase) >= 0;
        if (hasUnplayableStatus && hasMembersOnlyMarker)
        {
            return "members-only";
        }

        if (raw.IndexOf("\"status\":\"LOGIN_REQUIRED\"", StringComparison.Ordinal) >= 0)
        {
            return "login-required";
        }

        if (hasUnplayableStatus)
        {
            return "unplayable";
        }

        if (raw.IndexOf("\"status\":\"ERROR\"", StringComparison.Ordinal) >= 0)
        {
            return "error";
        }

        return null;
    }

    private static string? ExtractViewCountText(JsonElement videoRenderer)
    {
        if (
            videoRenderer.TryGetProperty("shortViewCountText", out JsonElement shortViewCount)
            && TryReadTextFromRunsOrSimple(shortViewCount, out string? shortText)
        )
        {
            return shortText;
        }

        return videoRenderer.TryGetProperty("viewCountText", out JsonElement viewCount)
            && TryReadTextFromRunsOrSimple(viewCount, out string? viewText)
            ? viewText
            : null;
    }

    private static long? ExtractUpcomingStartTime(JsonElement videoRenderer)
    {
        if (
            !videoRenderer.TryGetProperty("upcomingEventData", out JsonElement upcomingData)
            || upcomingData.ValueKind != JsonValueKind.Object
            || !upcomingData.TryGetProperty("startTime", out JsonElement startTimeElement)
            || startTimeElement.ValueKind != JsonValueKind.String
        )
        {
            return null;
        }

        return long.TryParse(startTimeElement.GetString(), out long parsedStart)
            ? parsedStart
            : null;
    }

    private static bool TryReadTextFromRunsOrSimple(JsonElement element, out string? text)
    {
        text = null;

        if (
            element.TryGetProperty("simpleText", out JsonElement simpleText)
            && simpleText.ValueKind == JsonValueKind.String
        )
        {
            text = simpleText.GetString();
            return !string.IsNullOrWhiteSpace(text);
        }

        if (
            !element.TryGetProperty("runs", out JsonElement runs)
            || runs.ValueKind != JsonValueKind.Array
        )
        {
            return false;
        }

        List<string> parts = [];
        foreach (JsonElement run in runs.EnumerateArray())
        {
            if (
                run.TryGetProperty("text", out JsonElement textElement)
                && textElement.ValueKind == JsonValueKind.String
            )
            {
                string? part = textElement.GetString();
                if (!string.IsNullOrEmpty(part))
                {
                    parts.Add(part!);
                }
            }
        }

        if (parts.Count == 0)
        {
            return false;
        }

        text = string.Concat(parts);
        return !string.IsNullOrWhiteSpace(text);
    }

    private static string? ExtractJsonObjectAfterMarker(string raw, string marker)
    {
        int markerIndex = raw.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return null;
        }

        int equalsIndex = raw.IndexOf('=', markerIndex);
        if (equalsIndex < 0)
        {
            return null;
        }

        int start = raw.IndexOf('{', equalsIndex);
        if (start < 0)
        {
            return null;
        }

        bool inString = false;
        bool escaping = false;
        int depth = 0;

        for (int i = start; i < raw.Length; i++)
        {
            char ch = raw[i];

            if (inString)
            {
                if (escaping)
                {
                    escaping = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaping = true;
                    continue;
                }

                if (ch == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (ch == '"')
            {
                inString = true;
                continue;
            }

            if (ch == '{')
            {
                depth++;
                continue;
            }

            if (ch != '}')
            {
                continue;
            }

            depth--;
            if (depth == 0)
            {
                return raw.Substring(start, i - start + 1);
            }
        }

        return null;
    }

    private static string? TryExtractViewCountText(string window)
    {
        Match shortText = StreamsShortViewCountSimpleTextRegex().Match(window);
        if (shortText.Success)
        {
            return shortText.Groups[1].Value;
        }

        Match viewText = StreamsViewCountSimpleTextRegex().Match(window);
        if (viewText.Success)
        {
            return viewText.Groups[1].Value;
        }

        Match shortRunText = StreamsShortViewCountRunRegex().Match(window);
        return shortRunText.Success ? shortRunText.Groups[1].Value : null;
    }

    private static long? MergeUpcomingStartTime(long? left, long? right)
    {
        if (!left.HasValue)
        {
            return right;
        }

        if (!right.HasValue)
        {
            return left;
        }

        return Math.Min(left.Value, right.Value);
    }

    private static string? TryGetGroupValue(Match match, int groupIndex) =>
        match.Success ? match.Groups[groupIndex].Value : null;

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

    private static AddChatItemActionItem? GetTickerBackedAddChatItem(Action action)
    {
        AddLiveChatTickerItemActionItem? tickerItem = action.AddLiveChatTickerItemAction?.Item;
        if (tickerItem == null)
        {
            return null;
        }

        LiveChatPaidMessageRenderer? paidRenderer = tickerItem
            .LiveChatTickerPaidMessageItemRenderer
            ?.ShowItemEndpoint
            ?.ShowLiveChatItemEndpoint
            ?.Renderer
            ?.LiveChatPaidMessageRenderer;
        if (paidRenderer != null)
        {
            return new AddChatItemActionItem { LiveChatPaidMessageRenderer = paidRenderer };
        }

        LiveChatMembershipItemRenderer? membershipRenderer = tickerItem
            .LiveChatTickerSponsorItemRenderer
            ?.ShowItemEndpoint
            ?.ShowLiveChatItemEndpoint
            ?.Renderer
            ?.LiveChatMembershipItemRenderer;
        if (membershipRenderer != null)
        {
            return new AddChatItemActionItem
            {
                LiveChatMembershipItemRenderer = membershipRenderer,
            };
        }

        LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer? giftPurchaseRenderer = tickerItem
            .LiveChatTickerSponsorItemRenderer
            ?.ShowItemEndpoint
            ?.ShowLiveChatItemEndpoint
            ?.Renderer
            ?.LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer;
        if (giftPurchaseRenderer != null)
        {
            return new AddChatItemActionItem
            {
                LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer = giftPurchaseRenderer,
            };
        }

        LiveChatPaidStickerRenderer? paidStickerRenderer = tickerItem
            .LiveChatTickerPaidStickerItemRenderer
            ?.ShowItemEndpoint
            ?.ShowLiveChatItemEndpoint
            ?.Renderer
            ?.LiveChatPaidStickerRenderer;
        return paidStickerRenderer != null
            ? new AddChatItemActionItem { LiveChatPaidStickerRenderer = paidStickerRenderer }
            : null;
    }

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
    private static (decimal AmountValue, string Currency) ParseAmount(string? amountString) =>
        CurrencyParser.Parse(amountString);

    private static string? TryExtractTierNameFromHeaderSubtextRuns(
        IEnumerable<Models.Response.MessageRun>? runs
    )
    {
        if (runs == null)
        {
            return null;
        }

        string? first = null;
        string? middle = null;
        string? last = null;
        int count = 0;
        foreach (Models.Response.MessageRun run in runs)
        {
            if (run is not Models.Response.MessageText textRun)
            {
                continue;
            }

            string? value = textRun.Text;
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            string nonNullValue = value!;

            count++;
            switch (count)
            {
                case 1:
                    first = nonNullValue;
                    break;
                case 2:
                    middle = nonNullValue.Trim();
                    break;
                case 3:
                    last = nonNullValue.Trim();
                    break;
                default:
                    return null;
            }
        }

        return count != 3 || first == null || middle == null || last == null ? null
            : middle.Length == 0 ? null
            : (last == "!" || last == "！") && first.EndsWith(" ", StringComparison.Ordinal)
                ? middle
            : null;
    }

    /// <summary>
    /// Converts a raw YouTube Action into a StreamWeaver ChatItem contract.
    /// </summary>
    public static Contracts.Models.ChatItem? ToChatItem(this Action action) // Return contract type
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        bool isTickerAction = false;
        AddChatItemActionItem? item = action.AddChatItemAction?.Item;
        if (item == null)
        {
            item = GetTickerBackedAddChatItem(action);
            isTickerAction = item != null;
        }

        if (item == null)
            return null;

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
                        Label =
                            badgeContainer.LiveChatAuthorBadgeRenderer?.Tooltip
                            ?? badgeContainer
                                .LiveChatAuthorBadgeRenderer
                                ?.Accessibility
                                ?.AccessibilityData
                                ?.Label
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

        author.Badge = authorBadge;

        // --- Message Content ---
        Contracts.Models.MessagePart[] messageParts = []; // Use contract type
        int? viewerLeaderboardRank = null;
        if (item.LiveChatTextMessageRenderer?.Message?.Runs != null)
        {
            messageParts = item.LiveChatTextMessageRenderer.Message.Runs.ToMessageParts();

            string? rankTitle = item
                .LiveChatTextMessageRenderer.BeforeContentButtons?.FirstOrDefault()
                ?.ButtonViewModel?.Title;
            if (
                rankTitle is string rankTitleValue
                && !string.IsNullOrWhiteSpace(rankTitleValue)
                && rankTitleValue.StartsWith("#", StringComparison.Ordinal)
            )
            {
                string rankValue = rankTitleValue.Substring(1);
                if (int.TryParse(rankValue, out int rank))
                {
                    viewerLeaderboardRank = rank;
                }
            }
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
                if (membershipItem.Message?.Runs != null)
                {
                    messageParts = membershipItem.Message.Runs.ToMessageParts();
                }
                else if (membershipItem.HeaderSubtext?.Runs != null)
                {
                    // Preserve structured welcome/system text parts as fallback.
                    messageParts = membershipItem.HeaderSubtext.Runs.ToMessageParts();
                }

                string? levelNameFromBadge = baseRenderer
                    .AuthorBadges?.FirstOrDefault(b =>
                        b.LiveChatAuthorBadgeRenderer?.CustomThumbnail != null
                    )
                    ?.LiveChatAuthorBadgeRenderer?.Tooltip;
                if (string.IsNullOrWhiteSpace(levelNameFromBadge))
                {
                    levelNameFromBadge = baseRenderer
                        .AuthorBadges?.FirstOrDefault(b =>
                            b.LiveChatAuthorBadgeRenderer?.CustomThumbnail != null
                        )
                        ?.LiveChatAuthorBadgeRenderer?.Accessibility?.AccessibilityData?.Label;
                }

                // Correctly parse HeaderSubtext when it has runs
                string? parsedHeaderSubtext = membershipItem.HeaderSubtext?.SimpleText; // Try simple text first
                if (
                    string.IsNullOrEmpty(parsedHeaderSubtext)
                    && membershipItem.HeaderSubtext?.Runs != null
                )
                {
                    // If simple text is null/empty but runs exist, combine them
                    parsedHeaderSubtext = membershipItem
                        .HeaderSubtext.Runs.ToMessageParts()
                        .ToSimpleString();
                }

                membershipInfo = new()
                {
                    LevelName = "Member",
                    MembershipBadgeLabel = levelNameFromBadge,
                    HeaderSubtext = parsedHeaderSubtext, // Use the correctly parsed subtext
                    HeaderPrimaryText = membershipItem
                        .HeaderPrimaryText?.Runs?.ToMessageParts()
                        ?.ToSimpleString(),
                };

                bool isGenericOrNonTierBadge =
                    string.Equals(
                        membershipInfo.LevelName,
                        "Member",
                        StringComparison.OrdinalIgnoreCase
                    )
                    || string.Equals(
                        membershipInfo.LevelName,
                        "New member",
                        StringComparison.OrdinalIgnoreCase
                    );
                bool badgeIndicatesNew =
                    levelNameFromBadge?.IndexOf("new member", StringComparison.OrdinalIgnoreCase)
                    >= 0;

                // Adjust New Member Detection to also check the parsedHeaderSubtext for "Welcome"
                if (
                    membershipInfo.HeaderPrimaryText?.StartsWith(
                        "Welcome",
                        StringComparison.OrdinalIgnoreCase
                    ) == true
                    || membershipInfo.HeaderSubtext?.StartsWith(
                        "New member",
                        StringComparison.OrdinalIgnoreCase
                    ) == true
                    || membershipInfo.HeaderSubtext?.StartsWith(
                        "Welcome to",
                        StringComparison.OrdinalIgnoreCase
                    ) == true // Check for "Welcome to" in HeaderSubtext
                    || badgeIndicatesNew
                )
                {
                    membershipInfo.EventType = Contracts.Models.MembershipEventType.New;

                    string? tierFromRuns = TryExtractTierNameFromHeaderSubtextRuns(
                        membershipItem.HeaderSubtext?.Runs
                    );
                    if (!string.IsNullOrWhiteSpace(tierFromRuns))
                    {
                        membershipInfo.LevelName = tierFromRuns!;
                    }

                    // Attempt to parse level name from full text if still generic.
                    if (isGenericOrNonTierBadge)
                    {
                        if (!string.IsNullOrEmpty(membershipInfo.HeaderSubtext))
                        {
                            // Regex for "Welcome to {LevelName}!" in HeaderSubtext
                            Match subtextLevelMatch = NewMemberLevelFromSubtextRegex()
                                .Match(membershipInfo.HeaderSubtext);
                            if (subtextLevelMatch.Success)
                            {
                                membershipInfo.LevelName = subtextLevelMatch.Groups[1].Value.Trim();
                            }
                        }

                        // Fallback to HeaderPrimaryText if not found in HeaderSubtext
                        if (!string.IsNullOrEmpty(membershipInfo.HeaderPrimaryText))
                        {
                            Match primaryTextLevelMatch = NewMemberLevelRegex()
                                .Match(membershipInfo.HeaderPrimaryText);
                            if (primaryTextLevelMatch.Success)
                            {
                                membershipInfo.LevelName = primaryTextLevelMatch
                                    .Groups[1]
                                    .Value.Trim();
                            }
                        }
                    }
                }
                else if (
                    membershipInfo.HeaderPrimaryText?.IndexOf(
                        "member for",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
                )
                {
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
                                        ?? badgeContainer
                                            .LiveChatAuthorBadgeRenderer
                                            ?.Accessibility
                                            ?.AccessibilityData
                                            ?.Label
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
                    LevelName = "Member",
                    MembershipBadgeLabel = levelNameFromBadge,
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
                    LevelName = "Member",
                    MembershipBadgeLabel = levelNameFromBadge,
                    EventType = Contracts.Models.MembershipEventType.GiftRedemption,
                    HeaderPrimaryText = giftRedemption
                        .Message?.Runs?.ToMessageParts()
                        ?.ToSimpleString(), // Usually "Welcome!" or similar
                    RecipientUsername = author.Name, // Author of this event is the recipient
                };

                if (giftRedemption.Message?.Runs != null)
                {
                    messageParts = giftRedemption.Message.Runs.ToMessageParts();
                }

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
            ViewerLeaderboardRank = viewerLeaderboardRank,
            IsTicker = isTickerAction,
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
        (
            List<(Contracts.Models.ChatItem Item, int ActionIndex)> indexedItems,
            string? continuation
        ) = ParseLiveChatResponseWithActionIndex(response);
        List<Contracts.Models.ChatItem> items = [.. indexedItems.Select(i => i.Item)];
        return (items, continuation);
    }

    /// <summary>
    /// Parses the full API response to extract chat items with their source action index
    /// and the next continuation token.
    /// </summary>
    public static (
        List<(Contracts.Models.ChatItem Item, int ActionIndex)> Items,
        string? Continuation
    ) ParseLiveChatResponseWithActionIndex(LiveChatResponse? response)
    {
        List<Action>? actions = response?.ContinuationContents?.LiveChatContinuation?.Actions;
        List<(Contracts.Models.ChatItem Item, int ActionIndex)> items =
            actions == null ? [] : new(actions.Count);
        if (actions != null)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                Contracts.Models.ChatItem? parsedItem = actions[i].ToChatItem();
                if (parsedItem != null)
                {
                    items.Add((parsedItem, i));
                }
            }
        }

        Continuation? nextContinuation =
            response?.ContinuationContents?.LiveChatContinuation?.Continuations?.FirstOrDefault();
        string? continuationToken =
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
        "<link[^>]*rel=\"canonical\"[^>]*href=\"[^\"]*watch\\?v=([A-Za-z0-9_-]{11})[^\"]*\"",
        RegexOptions.Compiled
    )]
    private static partial Regex LiveIdRegex();

    [GeneratedRegex(
        "\"canonicalBaseUrl\":\\s*\"[^\\\"]*watch\\?v=([A-Za-z0-9_-]{11})",
        RegexOptions.Compiled
    )]
    private static partial Regex CanonicalBaseUrlLiveIdRegex();

    [GeneratedRegex("\"topic\":\\s*\"chat~([A-Za-z0-9_-]{11})\"", RegexOptions.Compiled)]
    private static partial Regex ChatTopicLiveIdRegex();

    [GeneratedRegex(
        "\"videoDetails\":\\s*\\{[^\\}]*\"videoId\":\\s*\"([A-Za-z0-9_-]{11})\"",
        RegexOptions.Compiled
    )]
    private static partial Regex VideoDetailsLiveIdRegex();

    [GeneratedRegex("\"isReplay\":\\s*(true)", RegexOptions.Compiled)]
    private static partial Regex ReplayRegex();

    [GeneratedRegex("\"INNERTUBE_API_KEY\":\\s*\"([^\"]*)\"", RegexOptions.Compiled)]
    private static partial Regex ApiKeyRegex();

    [GeneratedRegex("\"INNERTUBE_CONTEXT_CLIENT_VERSION\":\\s*\"([^\"]*)\"", RegexOptions.Compiled)]
    private static partial Regex ClientVersionRegex();

    [GeneratedRegex("\"continuation\":\\s*\"([^\"]*)\"", RegexOptions.Compiled)]
    private static partial Regex ContinuationRegex();

    [GeneratedRegex("\"isLiveNow\":\\s*(true|false)", RegexOptions.Compiled)]
    private static partial Regex IsLiveNowRegex();

    [GeneratedRegex("\"isUpcoming\":\\s*(true|false)", RegexOptions.Compiled)]
    private static partial Regex IsUpcomingRegex();

    [GeneratedRegex("\"isLive\":\\s*(true|false)", RegexOptions.Compiled)]
    private static partial Regex IsLiveRegex();

    [GeneratedRegex("\"videoId\":\"([A-Za-z0-9_-]{11})\"", RegexOptions.Compiled)]
    private static partial Regex StreamsVideoIdRegex();

    [GeneratedRegex(
        "\"thumbnailOverlayTimeStatusRenderer\":\\{[\\s\\S]*?\"style\":\"(LIVE|UPCOMING|DEFAULT)\"",
        RegexOptions.Compiled
    )]
    private static partial Regex StreamsOverlayStyleRegex();

    [GeneratedRegex("\"upcomingEventData\":\\{\"startTime\":\"(\\d+)\"", RegexOptions.Compiled)]
    private static partial Regex StreamsUpcomingStartTimeRegex();

    [GeneratedRegex(
        "\"shortViewCountText\":\\{\"simpleText\":\"([^\"]+)\"",
        RegexOptions.Compiled
    )]
    private static partial Regex StreamsShortViewCountSimpleTextRegex();

    [GeneratedRegex(
        "\"viewCountText\":\\{\"simpleText\":\"([^\"]+)\"",
        RegexOptions.Compiled
    )]
    private static partial Regex StreamsViewCountSimpleTextRegex();

    [GeneratedRegex(
        "\"shortViewCountText\":\\{\"runs\":\\[\\{\"text\":\"([^\"]+)\"",
        RegexOptions.Compiled
    )]
    private static partial Regex StreamsShortViewCountRunRegex();

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

    [GeneratedRegex(@"Welcome to (.*?)!", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NewMemberLevelFromSubtextRegex();

    // Regex to extract gifter name from redemption message like "GifterName gifted you..."
    [GeneratedRegex(@"^(.*?) gifted you", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GiftRedemptionGifterRegex();
#else
    // Fallback for .NET Standard 2.1
    private static readonly Regex _liveIdRegex = new(
        "<link[^>]*rel=\"canonical\"[^>]*href=\"[^\"]*watch\\?v=([A-Za-z0-9_-]{11})[^\"]*\"",
        RegexOptions.Compiled
    );

    private static Regex LiveIdRegex() => _liveIdRegex;

    private static readonly Regex _canonicalBaseUrlLiveIdRegex = new(
        "\"canonicalBaseUrl\":\\s*\"[^\\\"]*watch\\?v=([A-Za-z0-9_-]{11})",
        RegexOptions.Compiled
    );

    private static Regex CanonicalBaseUrlLiveIdRegex() => _canonicalBaseUrlLiveIdRegex;

    private static readonly Regex _chatTopicLiveIdRegex = new(
        "\"topic\":\\s*\"chat~([A-Za-z0-9_-]{11})\"",
        RegexOptions.Compiled
    );

    private static Regex ChatTopicLiveIdRegex() => _chatTopicLiveIdRegex;

    private static readonly Regex _videoDetailsLiveIdRegex = new(
        "\"videoDetails\":\\s*\\{[^\\}]*\"videoId\":\\s*\"([A-Za-z0-9_-]{11})\"",
        RegexOptions.Compiled
    );

    private static Regex VideoDetailsLiveIdRegex() => _videoDetailsLiveIdRegex;

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

    private static readonly Regex _isLiveNowRegex = new(
        "\"isLiveNow\":\\s*(true|false)",
        RegexOptions.Compiled
    );

    private static Regex IsLiveNowRegex() => _isLiveNowRegex;

    private static readonly Regex _isUpcomingRegex = new(
        "\"isUpcoming\":\\s*(true|false)",
        RegexOptions.Compiled
    );

    private static Regex IsUpcomingRegex() => _isUpcomingRegex;

    private static readonly Regex _isLiveRegex = new(
        "\"isLive\":\\s*(true|false)",
        RegexOptions.Compiled
    );

    private static Regex IsLiveRegex() => _isLiveRegex;

    private static readonly Regex _streamsVideoIdRegex = new(
        "\"videoId\":\"([A-Za-z0-9_-]{11})\"",
        RegexOptions.Compiled
    );

    private static Regex StreamsVideoIdRegex() => _streamsVideoIdRegex;

    private static readonly Regex _streamsOverlayStyleRegex = new(
        "\"thumbnailOverlayTimeStatusRenderer\":\\{[\\s\\S]*?\"style\":\"(LIVE|UPCOMING|DEFAULT)\"",
        RegexOptions.Compiled
    );

    private static Regex StreamsOverlayStyleRegex() => _streamsOverlayStyleRegex;

    private static readonly Regex _streamsUpcomingStartTimeRegex = new(
        "\"upcomingEventData\":\\{\"startTime\":\"(\\d+)\"",
        RegexOptions.Compiled
    );

    private static Regex StreamsUpcomingStartTimeRegex() => _streamsUpcomingStartTimeRegex;

    private static readonly Regex _streamsShortViewCountSimpleTextRegex = new(
        "\"shortViewCountText\":\\{\"simpleText\":\"([^\"]+)\"",
        RegexOptions.Compiled
    );

    private static Regex StreamsShortViewCountSimpleTextRegex() => _streamsShortViewCountSimpleTextRegex;

    private static readonly Regex _streamsViewCountSimpleTextRegex = new(
        "\"viewCountText\":\\{\"simpleText\":\"([^\"]+)\"",
        RegexOptions.Compiled
    );

    private static Regex StreamsViewCountSimpleTextRegex() => _streamsViewCountSimpleTextRegex;

    private static readonly Regex _streamsShortViewCountRunRegex = new(
        "\"shortViewCountText\":\\{\"runs\":\\[\\{\"text\":\"([^\"]+)\"",
        RegexOptions.Compiled
    );

    private static Regex StreamsShortViewCountRunRegex() => _streamsShortViewCountRunRegex;

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

    private static readonly Regex _newMemberLevelFromSubtextRegex = new(
        @"Welcome to (.*?)!",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static Regex NewMemberLevelFromSubtextRegex() => _newMemberLevelFromSubtextRegex;

    private static readonly Regex _giftRedemptionGifterRegex = new(
        @"^(.*?) gifted you",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static Regex GiftRedemptionGifterRegex() => _giftRedemptionGifterRegex;
#endif

    #endregion

    // Helper extension method to convert message parts back to a simple string
    internal static string ToSimpleString(this IEnumerable<Contracts.Models.MessagePart>? parts) // Use contract type
    {
        return string.Join(
            "",
            parts?.Select(p =>
                p switch
                {
                    Contracts.Models.TextPart tp => tp.Text,
                    Contracts.Models.EmojiPart ep => ep.EmojiText,
                    Contracts.Models.ImagePart ip => ip.Alt,
                    _ => "",
                }
            ) ?? []
        );
    }
}

internal readonly record struct StreamPageCandidate(
    string LiveId,
    bool IsLive,
    bool IsUpcoming,
    long? UpcomingStartTime,
    string? ViewCountText
);
