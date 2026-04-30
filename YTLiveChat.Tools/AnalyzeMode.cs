using System.Text.Json;

/// <summary>
/// Field-level baseline diffing across all renderer types and locations.
/// Flags NEW fields (observed in logs but absent from the C# response model),
/// unknown renderer types, and badge composition breakdowns.
/// </summary>
internal static class AnalyzeMode
{
    // ── Baselines ─────────────────────────────────────────────────────────────
    // Built from the C# response model records in LiveChatResponse.cs.
    // clickTrackingParams is a ubiquitous tracking sibling — included everywhere
    // so it never fires as a false-positive NEW field.

    private static readonly HashSet<string> MessageRendererBaseFields =
    [
        "id", "timestampUsec",
        "authorName", "authorPhoto", "authorBadges", "authorExternalChannelId",
        "contextMenuEndpoint", "contextMenuAccessibility",
        "trackingParams", "clickTrackingParams",
    ];

    private static readonly Dictionary<string, HashSet<string>> Baselines =
        new(StringComparer.Ordinal)
        {
            // ── addChatItemAction item renderers ──────────────────────────────
            ["liveChatTextMessageRenderer"] =
            [
                ..MessageRendererBaseFields,
                "message",
                "beforeContentButtons",
            ],
            ["liveChatPaidMessageRenderer"] =
            [
                ..MessageRendererBaseFields,
                "message",
                "purchaseAmountText",
                "headerBackgroundColor",
                "headerTextColor",
                "bodyBackgroundColor",
                "bodyTextColor",
                "authorNameTextColor",
                "timestampColor",
                "isV2Style",
                "textInputBackgroundColor",
                "leaderboardBadge",
                // UI interaction widgets — server-rendered buttons, no data value for a read-only library
                "creatorHeartButton",
                "replyButton",
                "pdgLikeButton",
            ],
            ["liveChatPaidStickerRenderer"] =
            [
                ..MessageRendererBaseFields,
                "sticker",
                "purchaseAmountText",
                "moneyChipBackgroundColor",
                "moneyChipTextColor",
                "backgroundColor",
                "authorNameTextColor",
                "stickerDisplayWidth",
                "stickerDisplayHeight",
                "isV2Style",
                // 1st-purchase novelty fields (~4% of stickers): decorative overlay + bumper + tracking
                "headerOverlayImage",
                "lowerBumper",
                "pdgPurchasedNoveltyLoggingDirectives",
                // Present on ~98% of ticker stickers: purchase description + visual gradient
                "purchaseText",
                "backgroundGradient",
                // UI-only interaction widget (same as on liveChatPaidMessageRenderer)
                "creatorHeartButton",
            ],
            ["liveChatMembershipItemRenderer"] =
            [
                ..MessageRendererBaseFields,
                "headerPrimaryText",
                "headerSubtext",
                "message",
                // Present (~3.7%) on milestone items with no message body
                "empty",
            ],
            ["liveChatSponsorshipsGiftPurchaseAnnouncementRenderer"] =
            [
                ..MessageRendererBaseFields,
                "header",
            ],
            ["liveChatSponsorshipsGiftRedemptionAnnouncementRenderer"] =
            [
                ..MessageRendererBaseFields,
                "message",
            ],
            ["liveChatPlaceholderItemRenderer"] =
            [
                "id",
                "timestampUsec",
                "clickTrackingParams",
            ],
            // Loosely known — the model uses JsonObject fallback for these
            ["liveChatViewerEngagementMessageRenderer"] =
            [
                "id", "timestampUsec",
                "message", "actionButton", "icon",
                "trackingParams", "clickTrackingParams",
                // Observed on ~4% of instances (e.g. guidelines messages that are dismissable)
                "contextMenuEndpoint", "contextMenuAccessibility",
            ],
            ["liveChatModeChangeMessageRenderer"] =
            [
                "id", "timestampUsec",
                "text", "subtext", "icon",
                "trackingParams", "clickTrackingParams",
            ],

            // ── addBannerToLiveChatCommand ────────────────────────────────────
            ["liveChatBannerRenderer"] =
            [
                "header", "contents",
                "actionId", "viewerIsCreator", "targetId",
                "isStackable", "backgroundType", "bannerProperties", "bannerType",
                "clickTrackingParams",
                // UI-only collapse/expand commands (observed on pinned-message banners)
                "onCollapseCommand", "onExpandCommand",
            ],
            ["liveChatBannerRedirectRenderer"] =
            [
                "bannerMessage", "authorPhoto",
                "inlineActionButton", "bannerActionButton", "contextMenuButton",
                "clickTrackingParams",
                // Logging/accessibility context — observed on ~12% of redirect banners
                "rendererContext",
            ],
            ["liveChatBannerChatSummaryRenderer"] =
            [
                "liveChatSummaryId", "chatSummary",
                // UI-only interaction fields: feedback buttons + icon
                "collapsedStateEntityKey",
                "dislikeFeedbackButton", "likeFeedbackButton",
                "icon",
                "trackingParams", "clickTrackingParams",
            ],
            ["liveChatCallForQuestionsRenderer"] =
            [
                "questionMessage", "creatorAuthorName", "creatorAvatar", "featureLabel",
                "clickTrackingParams",
                // UI-only: separator dot ("·") and overflow menu button
                "contentSeparator", "overflowMenuButton",
            ],

            // ── Poll (showLiveChatActionPanelAction / updateLiveChatPollAction) ─
            ["pollRenderer"] =
            [
                "choices", "liveChatPollId", "header",
                "trackingParams", "clickTrackingParams",
            ],
            ["pollHeaderRenderer"] =
            [
                "pollQuestion", "thumbnail", "metadataText", "liveChatPollType",
                "trackingParams", "clickTrackingParams",
            ],

            // ── YouTube Jewels virtual gift ───────────────────────────────────
            ["giftMessageViewModel"] =
            [
                "id", "text", "authorName", "image", "imageA11yLabel", "rendererContext",
                // Present on ~45% of gifts: URL-based gift image, its a11y label, and author avatar
                "giftImage", "giftImageA11yLabel", "authorAvatar",
            ],

            // ── addLiveChatTickerItemAction outer item renderers ──────────────
            ["liveChatTickerPaidMessageItemRenderer"] =
            [
                "id",
                "showItemEndpoint",
                "clickTrackingParams",
                "trackingParams",
                "authorExternalChannelId",
                "authorPhoto",
                "authorUsername",
                "startBackgroundColor",
                "endBackgroundColor",
                "amountTextColor",
                "durationSec",
                "fullDurationSec",
                // Animation / engagement tracking — UI-only, no data value for a read-only library
                "animationOrigin",
                "dynamicStateData",
                "openEngagementPanelCommand",
            ],
            ["liveChatTickerSponsorItemRenderer"] =
            [
                "id",
                "showItemEndpoint",
                "clickTrackingParams",
                "trackingParams",
                "authorExternalChannelId",
                "sponsorPhoto",
                "detailText",
                "detailTextColor",
                "detailIcon",
                "startBackgroundColor",
                "endBackgroundColor",
                "durationSec",
                "fullDurationSec",
            ],
            ["liveChatTickerPaidStickerItemRenderer"] =
            [
                "id",
                "showItemEndpoint",
                "clickTrackingParams",
                "trackingParams",
                "authorExternalChannelId",
                "authorPhoto",
                "tickerThumbnails",
                "startBackgroundColor",
                "endBackgroundColor",
                "durationSec",
                "fullDurationSec",
            ],

            // ── Creator Goal ticker chip ──────────────────────────────────────────
            ["liveChatTickerCreatorGoalViewModel"] =
            [
                "id",
                "initialTickerText",
                "tickerIcon",
                "creatorGoalEntityKey",
                "shouldShowCountIncrementAnimation",
                "a11yLabel",
                "onClickCommand",
                "loggingDirectives",
                "clickTrackingParams",
                "trackingParams",
            ],
        };

    // ── Deep scan baselines ───────────────────────────────────────────────────
    // Used by the two recursive scanners that walk every action at any depth.

    // All known field names inside run objects.
    private static readonly HashSet<string> KnownRunFields = new(StringComparer.Ordinal)
    {
        "text", "bold", "italics", "strikethrough", "emoji",
        "navigationEndpoint", "fontFace",
        // Per-run ARGB color tint (observed on liveChatBannerRedirectRenderer.bannerMessage runs)
        "textColor",
        // De-emphasized styling (observed on liveChatBannerChatSummaryRenderer.chatSummary runs)
        "deemphasize",
    };

    // Every JSON property name we have ever seen or modelled, at any nesting depth.
    // Any key not in this set that appears in a log will be flagged as NEW.
    private static readonly HashSet<string> AllKnownJsonKeys = new(StringComparer.Ordinal)
    {
        // ── Top-level InnerTube continuation response ─────────────────────────
        "continuationContents", "liveChatContinuation", "actions",
        "responseContext", "mainAppWebResponseContext", "loggedOut",
        "webResponseContextExtensionData", "hasDecorated",

        // ── Top-level action keys ─────────────────────────────────────────────
        "clickTrackingParams", "trackingParams",
        "addChatItemAction",
        "addLiveChatTickerItemAction",
        "addBannerToLiveChatCommand",
        "removeChatItemAction",
        "replaceChatItemAction",
        "removeChatItemByAuthorAction",
        "markChatItemsByAuthorAsDeletedAction",
        "showLiveChatActionPanelAction",
        "updateLiveChatPollAction",
        "closeLiveChatActionPanelAction",
        // Fanzone ticker chip — membership-event UI chip, no data content
        "showFanzoneTickerChipCommand",
        "removeFanzoneTickerChipCommand",
        "removeBannerForLiveChatCommand",
        // Creator goal ticker chip — Super Chat goal progress chip
        "showCreatorGoalTickerChipCommand",
        "liveChatReportModerationStateCommand",
        "signalAction",
        "changeEngagementPanelVisibilityAction",
        // Gift animation overlay widgets — companion to giftMessageViewModel, purely visual
        "addInteractivityWidgetAction",
        "updateOrAddInteractivityWidgetAction",

        // ── Action payload fields ─────────────────────────────────────────────
        "item",
        "targetItemId",
        "replacementItem",
        "externalChannelId",
        "panelToShow",
        "pollToUpdate",
        "targetPanelId",
        "skipOnDismissCommand",

        // ── Renderer container / wrapper keys ─────────────────────────────────
        "giftMessageViewModel",
        "liveChatTextMessageRenderer",
        "liveChatPaidMessageRenderer",
        "liveChatPaidStickerRenderer",
        "liveChatMembershipItemRenderer",
        "liveChatSponsorshipsGiftPurchaseAnnouncementRenderer",
        "liveChatSponsorshipsGiftRedemptionAnnouncementRenderer",
        "liveChatPlaceholderItemRenderer",
        "liveChatViewerEngagementMessageRenderer",
        "liveChatModeChangeMessageRenderer",
        "liveChatTickerPaidMessageItemRenderer",
        "liveChatTickerSponsorItemRenderer",
        "liveChatTickerPaidStickerItemRenderer",
        "liveChatBannerRenderer",
        "liveChatBannerHeaderRenderer",
        "liveChatBannerRedirectRenderer",
        "liveChatBannerChatSummaryRenderer",
        "liveChatCallForQuestionsRenderer",
        "liveChatActionPanelRenderer",
        "liveChatSponsorshipsHeaderRenderer",
        "liveChatItemContextMenuRenderer",
        "pollRenderer",
        "pollHeaderRenderer",
        "bannerRenderer",
        "contents",
        "renderer",
        "header",

        // ── Common renderer base fields ───────────────────────────────────────
        "id", "timestampUsec",
        "authorName", "authorPhoto", "authorBadges", "authorExternalChannelId",
        "contextMenuEndpoint", "contextMenuAccessibility",
        "message",

        // ── Rich text (runs + run-object fields) ──────────────────────────────
        "simpleText", "runs",
        "text", "bold", "italics", "strikethrough", "fontFace",
        "emoji", "emojiId", "shortcuts", "searchTerms", "image",
        "isCustomEmoji", "supportsSkinTone", "variantIds",
        "navigationEndpoint",

        // ── Thumbnail / image ─────────────────────────────────────────────────
        "thumbnails", "url", "width", "height",

        // ── Accessibility ─────────────────────────────────────────────────────
        "accessibility", "accessibilityData", "label",

        // ── Badge ─────────────────────────────────────────────────────────────
        "liveChatAuthorBadgeRenderer", "customThumbnail", "tooltip",
        "icon", "iconType",

        // ── Paid message ──────────────────────────────────────────────────────
        "purchaseAmountText",
        "headerBackgroundColor", "headerTextColor",
        "bodyBackgroundColor", "bodyTextColor",
        "authorNameTextColor", "timestampColor",
        "isV2Style", "textInputBackgroundColor",
        "leaderboardBadge", "creatorHeartButton", "replyButton", "pdgLikeButton",

        // ── Paid sticker ──────────────────────────────────────────────────────
        "sticker",
        "moneyChipBackgroundColor", "moneyChipTextColor", "backgroundColor",
        "stickerDisplayWidth", "stickerDisplayHeight",
        "headerOverlayImage", "lowerBumper", "pdgPurchasedNoveltyLoggingDirectives",
        // Present on ~98% of ticker stickers
        "purchaseText", "backgroundGradient",

        // ── Membership ────────────────────────────────────────────────────────
        "headerPrimaryText", "headerSubtext",
        "empty", "beforeContentButtons",

        // ── Gift sponsorship ──────────────────────────────────────────────────
        "primaryText", "primaryThumbnail",

        // ── Viewer engagement / mode change ───────────────────────────────────
        "actionButton", "subtext",

        // ── Ticker outer items ────────────────────────────────────────────────
        "showItemEndpoint", "showLiveChatItemEndpoint",
        "authorUsername", "sponsorPhoto",
        "detailText", "detailTextColor", "detailIcon",
        "startBackgroundColor", "endBackgroundColor", "amountTextColor",
        "durationSec", "fullDurationSec",
        "tickerThumbnails",
        "animationOrigin", "dynamicStateData", "openEngagementPanelCommand",

        // ── Banner ────────────────────────────────────────────────────────────
        "actionId", "viewerIsCreator", "targetId",
        "isStackable", "backgroundType", "bannerProperties", "bannerType",
        "autoCollapseDelay", "seconds", "bannerCollapsedStateEntityKey",
        "inlineActionButton", "bannerActionButton", "contextMenuButton",
        "bannerMessage",
        // Chat summary banner fields
        "liveChatSummaryId", "chatSummary",
        "collapsedStateEntityKey", "dislikeFeedbackButton", "likeFeedbackButton",
        // Call-for-questions banner fields
        "questionMessage", "creatorAuthorName", "creatorAvatar", "featureLabel",

        // ── Poll ──────────────────────────────────────────────────────────────
        "liveChatPollId", "choices", "selected",
        "signinEndpoint", "signInEndpoint", "nextEndpoint",
        "liveChatPollQuestion", "pollQuestion", "metadataText",
        "liveChatPollType", "thumbnail",
        "voteCount", "voteRatioIfSelected", "voteRatio", "votePercentage",

        // ── Leaderboard badge ─────────────────────────────────────────────────
        "buttonViewModel", "title", "iconName", "accessibilityText", "onTap",

        // ── InnerTube navigation / button infrastructure ───────────────────────
        "commandMetadata", "webCommandMetadata",
        "webPageType", "rootVe", "ignoreNavigation",
        "urlEndpoint", "target",
        "watchEndpoint", "videoId",
        "liveChatItemContextMenuEndpoint", "params",
        "serviceEndpoint", "feedbackEndpoint", "feedbackToken",
        "uiActions", "hideEnclosingContainer",
        "buttonRenderer", "style", "size", "isDisabled", "command",

        // ── Context menu items ────────────────────────────────────────────────
        "menuItems", "menuNavigationItemRenderer", "menuServiceItemRenderer",
        "defaultText",

        // ── Thumbnail sources (used in poll header, heart viewmodel, etc.) ───
        "sources", "clientResource", "imageName", "imageColor",

        // ── Logging / tracking / renderer context ─────────────────────────────
        "loggingDirectives", "visibility", "types", "clientId",
        "loggingContext", "rendererContext",
        "accessibilityContext", "commandContext",

        // ── Fanzone ticker chip (membership-event UI chip) ─────────────────────
        "fanzoneTickerChip", "liveChatTickerFanzoneViewModel",
        "tickerIcon", "endTimestampMs",
        "hack",  // removeFanzoneTickerChipCommand payload

        // ── Creator Goal ticker chip (showCreatorGoalTickerChipCommand) ──────────
        "creatorGoalTickerChip", "liveChatTickerCreatorGoalViewModel",
        "initialTickerText", "creatorGoalEntityKey",
        "shouldShowCountIncrementAnimation", "a11yLabel",
        "onClickCommand",
        // Engagement-panel chain (content path → progressCountA11yLabel)
        "engagementPanel", "engagementPanelSectionListRenderer",
        "sectionListRenderer",
        "creatorGoalProgressFlowViewModel", "progressFlowButton", "progressCountA11yLabel",
        "liveChatPurchaseMessageEndpoint", "titleFormatted",
        // Header/dialog path (help text shown when viewer clicks "?") — UI-only, not surfaced
        "engagementPanelTitleHeaderRenderer",
        "commandExecutorCommand", "commands",
        "liveChatDialogEndpoint", "liveChatDialogRenderer", "dialogMessages", "confirmButton",
        // Panel routing and presentation config
        "engagementPanelPresentationConfigs", "engagementPanelPopupPresentationConfig",
        "hideEngagementPanelEndpoint",

        // ── Toast / notification action ───────────────────────────────────────
        "liveChatAddToToastAction",
        "notificationActionRenderer", "responseText",

        // ── Run text styling ──────────────────────────────────────────────────
        "textColor",

        // ── YouTube Jewels gift view model ────────────────────────────────────
        "imageA11yLabel",
        // Present on ~45% of gifts: URL-based gift image + a11y label + author avatar
        "giftImage", "giftImageA11yLabel", "authorAvatar",

        // ── Content + styleRuns (ViewModel text containers) ───────────────────
        "content", "styleRuns", "startIndex", "length",

        // ── Engagement panel / reply thread system ────────────────────────────
        "showEngagementPanelEndpoint", "identifier", "surface", "tag",
        "globalConfiguration",
        "engagementPanelPopupPresentationConfig", "engagementPanelPresentationConfigs",
        "popupType", "innertubeCommand",
        "replyCountEntityKey", "replyCountPlaceholder",
        "engagementStateEntityKey", "engagementStateKey",
        "isFullWidth", "useGreenPath",

        // ── Reply button view models ──────────────────────────────────────────
        "pdgReplyButtonViewModel", "replyIcon",

        // ── Creator heart view model ──────────────────────────────────────────
        "creatorHeartViewModel", "creatorThumbnail",
        "heartedIcon", "unheartedIcon",
        "heartedAccessibilityLabel", "unheartedAccessibilityLabel", "heartedHoverText",
        "borderImageProcessor", "imageTint", "processor", "color",

        // ── Like / PDG like view model ────────────────────────────────────────
        "pdgLikeViewModel",
        "toggleButton", "toggleButtonViewModel",
        "defaultButtonViewModel", "toggledButtonViewModel",
        "likeCountEntityKey", "likeIcon", "likedIcon",
        "likesEmptyStateText", "emptyStateText",
        "buttonSize", "customBackgroundColor", "customFontColor", "type", "iconTrailing",

        // ── Ticker state animation ────────────────────────────────────────────
        "stateSlideDirection", "stateSlideDurationMs",
        "stateUpdateDelayAfterMs", "stateUpdateDelayBeforeMs",

        // ── Banner / command properties ───────────────────────────────────────
        "bannerTimeoutMs", "isEphemeral", "targetActionId",

        // ── Navigation extras ─────────────────────────────────────────────────
        "nofollow", "playerParams", "gestures",

        // ── 1st-purchase bumper view model ────────────────────────────────────
        "liveChatItemBumperViewModel", "bumperUserEduContentViewModel",
        "pdgPurchasedBumperLoggingDirectives",

        // ── Moderation / report state ─────────────────────────────────────────
        // (liveChatReportModerationStateCommand payload — opaque, not modelled)
        "moderationState",

        // ── Banner UI controls ────────────────────────────────────────────────
        // onCollapseCommand/onExpandCommand: fold animation on pinned-message banners
        "onCollapseCommand", "onExpandCommand",
        // contentSeparator: "·" separator dot in liveChatCallForQuestionsRenderer
        // overflowMenuButton: "⋮" overflow menu in liveChatCallForQuestionsRenderer
        "contentSeparator", "overflowMenuButton",

        // ── Run text styling ──────────────────────────────────────────────────
        // deemphasize: subdued style on summary banner runs (already in MessageText model)
        "deemphasize",

        // ── addInteractivityWidgetAction / updateOrAddInteractivityWidgetAction ─
        // These two action types are already classified as silent (purely visual gift
        // animations). All keys below live inside their payloads and are listed here
        // to suppress false-positive "Unknown JSON Key" reports in the analyzer.
        "interactivityWidgetRenderer", "widgetRenderer",
        "elementRenderer", "compatibilityOptions",
        "liveChatId", "liveChatAuthorExternalChannelId",
        "enterAnimation", "exitAnimation",
        "giftA11yLabel",
        "giftAttributionItemViewModel", "attributionImage",
        "giftOverlayItemViewModel", "overlayImage",
        "imageDisplayHeight", "imageDisplayWidth",
        "overlayImageDisplayHeight", "overlayImageDisplayWidth",
        "overlayImageHeightPercentageOfWindow",
        "shouldScaleOverlayImageDynamically",
        "contentMode",
        "companionWidgetRenderer",
        "timeoutMs", "priority", "queueId", "preloadImages",
        "position", "matrix", "layout", "rows", "columns", "packedData",
        "specialPlacement",
        "onWidgetShown",
        "comboCount", "comboDecorationImage", "displayImmediately",
        "entityUpdateCommand", "entityBatchUpdate", "mutations",
        "booleanEntity", "entityKey", "key", "payload", "value",
        "startX", "startY", "endX", "endY",
        "colors", "positions",
        "apiUrl", "sendPost",
        "elementsCommand", "setEntityCommand", "entity",
        "avatarViewModel", "avatarImageSize", "circular",
        "multiSelectorThumbnailRow",
    };

    // ── Data structures ───────────────────────────────────────────────────────

    private sealed class FieldEntry
    {
        public int Count;
        public string Example = string.Empty;
    }

    private sealed class RendererStats
    {
        public string Location = string.Empty;
        public string RendererType = string.Empty;
        public int TotalCount;
        // All field names observed → count + one example value
        public readonly Dictionary<string, FieldEntry> Fields = new(StringComparer.Ordinal);
        // Badge composition
        public int BadgeCustomThumbnailCount;
        public int BadgeIconTypeCount;
        public int BadgeUnknownCount;
    }

    // ── Entry point ───────────────────────────────────────────────────────────

    public static int Run(string[] args)
    {
        List<string> paths = [];
        bool verbose = false;

        foreach (string arg in args)
        {
            if (arg.Equals("--verbose", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-v", StringComparison.OrdinalIgnoreCase))
            {
                verbose = true;
                continue;
            }

            if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase))
            {
                PrintUsage();
                return 0;
            }

            paths.Add(arg);
        }

        if (paths.Count == 0)
        {
            Console.WriteLine("No log paths provided. Enter one or more paths or directories separated by ';' or ',':");
            Console.Write("> ");
            string? input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
            {
                foreach (string segment in input.Split([';', ','], StringSplitOptions.RemoveEmptyEntries))
                {
                    string p = segment.Trim();
                    if (!string.IsNullOrWhiteSpace(p))
                        paths.Add(p);
                }
            }
        }

        if (paths.Count == 0)
        {
            PrintUsage();
            return 1;
        }

        // Expand any directory paths to their contained *.jsonl files
        List<string> expandedPaths = [.. LogReader.ExpandPaths(paths)];

        // (location, rendererType) → stats
        // We use a stable insertion-order dictionary so sections print in encounter order
        Dictionary<string, RendererStats> stats = new(StringComparer.Ordinal);
        // Action type counts (for top-level summary)
        Dictionary<string, int> actionCounts = new(StringComparer.Ordinal);
        // Renderer types we have no baseline for at all
        HashSet<string> unknownRendererTypes = new(StringComparer.Ordinal);
        // Deep scan: any run-object field not in KnownRunFields, keyed by full JSON path
        Dictionary<string, FieldEntry> unknownRunFields = new(StringComparer.Ordinal);
        // Deep scan: any JSON property name not in AllKnownJsonKeys, keyed by name
        Dictionary<string, FieldEntry> unknownJsonKeys = new(StringComparer.Ordinal);
        List<string> parseErrors = [];
        int totalActions = 0;

        foreach (string path in expandedPaths)
        {
            if (!File.Exists(path))
            {
                parseErrors.Add($"Missing file: {path}");
                continue;
            }

            try
            {
                foreach (JsonElement action in LogReader.ReadActions(path))
                {
                    totalActions++;
                    string? actionType = LogReader.GetActionType(action);
                    if (actionType == null)
                        continue;

                    Increment(actionCounts, actionType);

                    if (actionType == "addChatItemAction")
                    {
                        ProcessAddChatItem(action, stats, unknownRendererTypes);
                    }
                    else if (actionType == "addLiveChatTickerItemAction")
                    {
                        ProcessTickerItem(action, stats, unknownRendererTypes);
                    }
                    else if (actionType == "addBannerToLiveChatCommand")
                    {
                        ProcessBannerAction(action, stats, unknownRendererTypes);
                    }
                    else if (actionType is "showLiveChatActionPanelAction" or "updateLiveChatPollAction")
                    {
                        ProcessPollAction(actionType, action, stats, unknownRendererTypes);
                    }

                    // Deep recursive scans on every action regardless of type:
                    // catches new keys and new run fields at any nesting depth.
                    WalkForUnknownKeys(action, unknownJsonKeys);
                    WalkForUnknownRunFields(actionType, action, unknownRunFields);
                }
            }
            catch (Exception ex)
            {
                parseErrors.Add($"{path}: {ex.Message}");
            }
        }

        // ── Print report ──────────────────────────────────────────────────────

        Console.WriteLine();
        WriteSectionHeader($"FIELD ANALYSIS REPORT — {expandedPaths.Count} file(s), {totalActions:N0} actions");
        Console.WriteLine();

        // Top-level action summary
        Console.WriteLine("Action type counts:");
        foreach (KeyValuePair<string, int> kv in actionCounts.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.Ordinal))
            Console.WriteLine($"  {kv.Value,7:N0}  {kv.Key}");
        Console.WriteLine();

        // Group stats by location
        IEnumerable<IGrouping<string, RendererStats>> groups = stats.Values
            .GroupBy(s => s.Location, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        bool anyNewFields = false;

        foreach (IGrouping<string, RendererStats> group in groups)
        {
            WriteSectionHeader($"Location: {group.Key}");
            Console.WriteLine();

            foreach (RendererStats rs in group.OrderByDescending(s => s.TotalCount))
            {
                bool hasBaseline = Baselines.TryGetValue(rs.RendererType, out HashSet<string>? baseline);
                Console.Write("  ");
                WriteColor(rs.RendererType, ConsoleColor.Cyan);
                Console.WriteLine($"  ({rs.TotalCount:N0} instances){(hasBaseline ? "" : "  [NO BASELINE]")}");

                // Sort fields: NEW first, then known alphabetically
                IEnumerable<KeyValuePair<string, FieldEntry>> ordered = rs.Fields
                    .OrderBy(f =>
                    {
                        bool known = hasBaseline && baseline!.Contains(f.Key);
                        return known ? 1 : 0; // NEW first
                    })
                    .ThenByDescending(f => f.Value.Count)
                    .ThenBy(f => f.Key, StringComparer.Ordinal);

                foreach (KeyValuePair<string, FieldEntry> field in ordered)
                {
                    bool known = hasBaseline && baseline!.Contains(field.Key);
                    double pct = rs.TotalCount > 0 ? field.Value.Count * 100.0 / rs.TotalCount : 0;
                    string tag = known ? "[known]" : "[NEW]  ";

                    Console.Write("    ");
                    if (!known)
                    {
                        WriteColor(tag, ConsoleColor.Yellow);
                        anyNewFields = true;
                    }
                    else if (verbose)
                    {
                        WriteColor(tag, ConsoleColor.DarkGray);
                    }
                    else
                    {
                        continue; // skip known fields in non-verbose mode
                    }

                    Console.Write($"  {field.Key,-40}  {field.Value.Count,6:N0}/{rs.TotalCount:N0}  ({pct,5:F1}%)");
                    if (!known)
                        Console.Write($"  eg: {field.Value.Example}");
                    Console.WriteLine();
                }

                // Report baseline fields never seen in this location
                if (hasBaseline && verbose)
                {
                    foreach (string knownField in baseline!.OrderBy(x => x, StringComparer.Ordinal))
                    {
                        if (!rs.Fields.ContainsKey(knownField))
                        {
                            Console.Write("    ");
                            WriteColor("[missing]", ConsoleColor.DarkGray);
                            Console.WriteLine($"  {knownField,-40}  (never observed in this file)");
                        }
                    }
                }

                // Badge breakdown
                if (rs.BadgeCustomThumbnailCount + rs.BadgeIconTypeCount + rs.BadgeUnknownCount > 0)
                {
                    int total = rs.BadgeCustomThumbnailCount + rs.BadgeIconTypeCount + rs.BadgeUnknownCount;
                    Console.WriteLine($"    [badges]   customThumbnail={rs.BadgeCustomThumbnailCount:N0}  iconType={rs.BadgeIconTypeCount:N0}  unknown={rs.BadgeUnknownCount:N0}  (total badge instances={total:N0})");
                }

                Console.WriteLine();
            }
        }

        if (!anyNewFields && !verbose)
        {
            Console.WriteLine("  (No new fields detected. Run with --verbose to see all known fields.)");
            Console.WriteLine();
        }

        // Unknown renderer types
        WriteSectionHeader("Unknown Renderer Types (no baseline, not in known set)");
        Console.WriteLine();
        HashSet<string> allKnown = new(Baselines.Keys, StringComparer.Ordinal);
        bool anyUnknown = false;
        foreach (string rt in unknownRendererTypes.OrderBy(x => x, StringComparer.Ordinal))
        {
            if (!allKnown.Contains(rt))
            {
                anyUnknown = true;
                Console.Write("  ");
                WriteColor($"[UNKNOWN]  {rt}", ConsoleColor.Red);
                Console.WriteLine();
            }
        }

        if (!anyUnknown)
            Console.WriteLine("  (none)");

        Console.WriteLine();

        // Unknown run fields — new fields inside runs[] at any depth, any action
        WriteSectionHeader("Unknown Run Fields (any depth, any action)");
        Console.WriteLine();
        if (unknownRunFields.Count == 0)
        {
            Console.WriteLine("  (none)");
        }
        else
        {
            foreach (KeyValuePair<string, FieldEntry> kv in unknownRunFields.OrderByDescending(x => x.Value.Count).ThenBy(x => x.Key, StringComparer.Ordinal))
            {
                Console.Write("  ");
                WriteColor("[NEW]  ", ConsoleColor.Yellow);
                Console.WriteLine($"{kv.Key,-80}  {kv.Value.Count:N0}x  eg: {kv.Value.Example}");
            }
        }

        Console.WriteLine();

        // Unknown JSON keys — any property name not in AllKnownJsonKeys, at any depth
        WriteSectionHeader("Unknown JSON Keys (any depth, any action)");
        Console.WriteLine();
        if (unknownJsonKeys.Count == 0)
        {
            Console.WriteLine("  (none)");
        }
        else
        {
            foreach (KeyValuePair<string, FieldEntry> kv in unknownJsonKeys.OrderByDescending(x => x.Value.Count).ThenBy(x => x.Key, StringComparer.Ordinal))
            {
                Console.Write("  ");
                WriteColor("[NEW]  ", ConsoleColor.Yellow);
                Console.WriteLine($"{kv.Key,-50}  {kv.Value.Count:N0}x  eg: {kv.Value.Example}");
            }
        }

        Console.WriteLine();

        if (parseErrors.Count > 0)
        {
            WriteSectionHeader("Parse Errors");
            foreach (string e in parseErrors)
                Console.WriteLine($"  {e}");
            Console.WriteLine();
            return 2;
        }

        return 0;
    }

    // ── Action processors ─────────────────────────────────────────────────────

    private static void ProcessAddChatItem(
        JsonElement action,
        Dictionary<string, RendererStats> stats,
        HashSet<string> unknownRendererTypes)
    {
        if (!action.TryGetProperty("addChatItemAction", out JsonElement addChat) ||
            !addChat.TryGetProperty("item", out JsonElement item))
        {
            return;
        }

        if (!LogReader.TryGetSingleRenderer(item, out string? rendererType, out JsonElement rendererValue) ||
            rendererType == null)
        {
            return;
        }

        ObserveRenderer("addChatItemAction", rendererType, rendererValue, stats, unknownRendererTypes);
    }

    private static void ProcessTickerItem(
        JsonElement action,
        Dictionary<string, RendererStats> stats,
        HashSet<string> unknownRendererTypes)
    {
        if (!action.TryGetProperty("addLiveChatTickerItemAction", out JsonElement tickerAction) ||
            !tickerAction.TryGetProperty("item", out JsonElement tickerItem))
        {
            return;
        }

        if (LogReader.TryGetSingleRenderer(tickerItem, out string? outerRenderer, out JsonElement outerValue) &&
            outerRenderer != null)
        {
            ObserveRenderer("ticker.item", outerRenderer, outerValue, stats, unknownRendererTypes);
        }

        if (LogReader.TryGetNestedShowRenderer(tickerItem, out string? nestedRenderer, out JsonElement nestedValue) &&
            nestedRenderer != null)
        {
            ObserveRenderer("ticker.showLiveChatItemEndpoint", nestedRenderer, nestedValue, stats, unknownRendererTypes);
        }
    }

    private static void ProcessBannerAction(
        JsonElement action,
        Dictionary<string, RendererStats> stats,
        HashSet<string> unknownRendererTypes)
    {
        if (!action.TryGetProperty("addBannerToLiveChatCommand", out JsonElement bannerCmd) ||
            !bannerCmd.TryGetProperty("bannerRenderer", out JsonElement bannerRenderer) ||
            !bannerRenderer.TryGetProperty("liveChatBannerRenderer", out JsonElement liveRenderer))
        {
            return;
        }

        ObserveRenderer("addBannerToLiveChatCommand", "liveChatBannerRenderer", liveRenderer, stats, unknownRendererTypes);

        if (liveRenderer.TryGetProperty("contents", out JsonElement contents) &&
            LogReader.TryGetSingleRenderer(contents, out string? contentRenderer, out JsonElement contentValue) &&
            contentRenderer != null)
        {
            ObserveRenderer("addBannerToLiveChatCommand.contents", contentRenderer, contentValue, stats, unknownRendererTypes);
        }
    }

    private static void ProcessPollAction(
        string actionType,
        JsonElement action,
        Dictionary<string, RendererStats> stats,
        HashSet<string> unknownRendererTypes)
    {
        JsonElement pollRenderer = default;
        bool found = false;

        if (actionType == "showLiveChatActionPanelAction" &&
            action.TryGetProperty("showLiveChatActionPanelAction", out JsonElement show) &&
            show.TryGetProperty("panelToShow", out JsonElement panel) &&
            panel.TryGetProperty("liveChatActionPanelRenderer", out JsonElement panelRenderer) &&
            panelRenderer.TryGetProperty("contents", out JsonElement contents) &&
            contents.TryGetProperty("pollRenderer", out pollRenderer))
        {
            found = true;
        }
        else if (actionType == "updateLiveChatPollAction" &&
            action.TryGetProperty("updateLiveChatPollAction", out JsonElement update) &&
            update.TryGetProperty("pollToUpdate", out JsonElement pollToUpdate) &&
            pollToUpdate.TryGetProperty("pollRenderer", out pollRenderer))
        {
            found = true;
        }

        if (found)
            ObserveRenderer(actionType, "pollRenderer", pollRenderer, stats, unknownRendererTypes);
    }

    // ── Field observation ─────────────────────────────────────────────────────

    private static void ObserveRenderer(
        string location,
        string rendererType,
        JsonElement rendererValue,
        Dictionary<string, RendererStats> stats,
        HashSet<string> unknownRendererTypes)
    {
        string key = $"{location}:{rendererType}";
        if (!stats.TryGetValue(key, out RendererStats? rs))
        {
            rs = new RendererStats { Location = location, RendererType = rendererType };
            stats[key] = rs;
        }

        rs.TotalCount++;

        if (!Baselines.ContainsKey(rendererType))
            _ = unknownRendererTypes.Add(rendererType);

        if (rendererValue.ValueKind != JsonValueKind.Object)
            return;

        foreach (JsonProperty prop in rendererValue.EnumerateObject())
        {
            if (!rs.Fields.TryGetValue(prop.Name, out FieldEntry? entry))
            {
                entry = new FieldEntry
                {
                    Example = LogReader.SummarizeValue(prop.Value, maxLength: 80),
                };
                rs.Fields[prop.Name] = entry;
            }

            entry.Count++;

            // Track badge composition from authorBadges
            if (prop.Name == "authorBadges" && prop.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement badge in prop.Value.EnumerateArray())
                {
                    if (badge.TryGetProperty("liveChatAuthorBadgeRenderer", out JsonElement badgeRenderer))
                    {
                        if (badgeRenderer.TryGetProperty("customThumbnail", out _))
                            rs.BadgeCustomThumbnailCount++;
                        else if (badgeRenderer.TryGetProperty("icon", out _))
                            rs.BadgeIconTypeCount++;
                        else
                            rs.BadgeUnknownCount++;
                    }
                    else
                    {
                        rs.BadgeUnknownCount++;
                    }
                }
            }
        }

    }

    // ── Deep recursive scanners ───────────────────────────────────────────────

    /// <summary>
    /// Recursively walks the entire JSON element and records any property name not in
    /// <see cref="AllKnownJsonKeys"/>. Keyed by the property name alone (count aggregates
    /// across all occurrences); the stored example gives enough context to locate it.
    /// </summary>
    private static void WalkForUnknownKeys(JsonElement element, Dictionary<string, FieldEntry> unknownKeys)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty prop in element.EnumerateObject())
            {
                if (!AllKnownJsonKeys.Contains(prop.Name))
                {
                    if (!unknownKeys.TryGetValue(prop.Name, out FieldEntry? entry))
                    {
                        entry = new FieldEntry { Example = LogReader.SummarizeValue(prop.Value, 90) };
                        unknownKeys[prop.Name] = entry;
                    }
                    entry.Count++;
                }
                WalkForUnknownKeys(prop.Value, unknownKeys);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
                WalkForUnknownKeys(item, unknownKeys);
        }
    }

    /// <summary>
    /// Recursively walks the entire JSON element looking for any object that has a
    /// <c>runs</c> property (a rich-text container). Within each run object, records any
    /// field name not in <see cref="KnownRunFields"/>, keyed by the full JSON path so
    /// callers can see exactly where in the tree the new field appeared.
    /// </summary>
    private static void WalkForUnknownRunFields(
        string path,
        JsonElement element,
        Dictionary<string, FieldEntry> unknownRunFields)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            // If this object is a rich-text container, scan its runs
            if (element.TryGetProperty("runs", out JsonElement runs) && runs.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement run in runs.EnumerateArray())
                {
                    if (run.ValueKind != JsonValueKind.Object)
                        continue;

                    foreach (JsonProperty runField in run.EnumerateObject())
                    {
                        if (KnownRunFields.Contains(runField.Name))
                            continue;

                        string key = $"{path}.runs[].{runField.Name}";
                        if (!unknownRunFields.TryGetValue(key, out FieldEntry? entry))
                        {
                            entry = new FieldEntry { Example = LogReader.SummarizeValue(runField.Value, 90) };
                            unknownRunFields[key] = entry;
                        }
                        entry.Count++;
                    }
                }
            }

            // Recurse into every property
            foreach (JsonProperty prop in element.EnumerateObject())
                WalkForUnknownRunFields($"{path}.{prop.Name}", prop.Value, unknownRunFields);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
                WalkForUnknownRunFields(path, item, unknownRunFields);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void WriteSectionHeader(string text)
    {
        string line = new('━', Math.Min(text.Length + 4, 72));
        WriteColor(line, ConsoleColor.DarkCyan);
        Console.WriteLine();
        WriteColor($"  {text}", ConsoleColor.DarkCyan);
        Console.WriteLine();
        WriteColor(line, ConsoleColor.DarkCyan);
        Console.WriteLine();
    }

    private static void WriteColor(string text, ConsoleColor color)
    {
        ConsoleColor prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = prev;
    }

    private static void Increment(Dictionary<string, int> dict, string key) =>
        dict[key] = dict.TryGetValue(key, out int v) ? v + 1 : 1;

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project YTLiveChat.Tools -- analyze [options] <path1> [path2 ...]");
        Console.WriteLine();
        Console.WriteLine("  Paths may be individual .jsonl files or directories (expanded to all *.jsonl within).");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --verbose / -v    Show all fields (known + new + missing). Default: new fields only.");
        Console.WriteLine("  --help            Show this message.");
        Console.WriteLine();
        Console.WriteLine("Sections in the report:");
        Console.WriteLine("  Action type counts       All top-level action/command keys seen.");
        Console.WriteLine("  Per-location renderers   Field presence per renderer per location.");
        Console.WriteLine("                           Locations: addChatItemAction, ticker.item,");
        Console.WriteLine("                           ticker.showLiveChatItemEndpoint,");
        Console.WriteLine("                           addBannerToLiveChatCommand,");
        Console.WriteLine("                           addBannerToLiveChatCommand.contents,");
        Console.WriteLine("                           showLiveChatActionPanelAction,");
        Console.WriteLine("                           updateLiveChatPollAction.");
        Console.WriteLine("  Unknown Renderer Types   Renderer keys with no baseline entry.");
        Console.WriteLine("  Unknown Run Fields       Fields inside runs[] arrays not in the known set.");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  # Analyze an entire logs directory:");
        Console.WriteLine("  dotnet run --project YTLiveChat.Tools -- analyze logs/");
        Console.WriteLine();
        Console.WriteLine("  # Analyze specific files with all fields shown:");
        Console.WriteLine("  dotnet run --project YTLiveChat.Tools -- analyze --verbose logs/watch_20260413.jsonl");
        Console.WriteLine();
        Console.WriteLine("  # Analyze old logs directory:");
        Console.WriteLine("  dotnet run --project YTLiveChat.Tools -- analyze logs/_old/");
    }
}
