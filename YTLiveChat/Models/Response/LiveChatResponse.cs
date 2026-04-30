/*
 * ==========================================================================
 *  SEMI-AUTO-GENERATED C# Data Models for YouTube Live Chat API Responses
 * ==========================================================================
 *
 *  Purpose:
 *  Defines internal C# record types to deserialize JSON responses from the
 *  YouTube Live Chat InnerTube API endpoint (live_chat/get_live_chat).
 *
 *  Structure:
 *  - A single root record `LiveChatResponse` represents the entire JSON response.
 *  - Contains nullable properties for all observed top-level JSON objects.
 *  - Numerous reusable record types capture common nested structures.
 *  - `JsonPropertyName` attributes map C# PascalCase properties to JSON camelCase keys.
 *
 *  Usage:
 *  - Internal models used by YTHttpClient and Parser.
 *  - Check non-null properties on a deserialized `LiveChatResponse` instance
 *    to determine the specific kind of data received.
 *
 *  Note on Polymorphism:
 *  Fields like `item` in `AddChatItemAction`, `replacementItem` in
 *  `ReplaceChatItemAction`, and `payload` in `Mutation` are handled by
 *  container records listing potential renderer types as nullable properties.
 */
using System.Text.Json;
using System.Text.Json.Nodes; // Required for JsonObject fallback
using System.Text.Json.Serialization;

namespace YTLiveChat.Models.Response;

// ==========================================================================
// Reusable Base Classes & Common Structures
// ==========================================================================

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public record AccessibilityData
{
    [JsonPropertyName("label")]
    public string? Label { get; init; }
}

public record Accessibility
{
    [JsonPropertyName("accessibilityData")]
    public AccessibilityData? AccessibilityData { get; init; }

    [JsonPropertyName("label")]
    public string? Label { get; init; }
}

public record Icon
{
    [JsonPropertyName("iconType")]
    public string? IconType { get; init; }
}

public record SimpleText
{
    [JsonPropertyName("simpleText")]
    public string? Text { get; init; }
}

public record Thumbnail
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }
}

public record ThumbnailList
{
    [JsonPropertyName("thumbnails")]
    public List<Thumbnail>? Thumbnails { get; init; }

    /// <summary>
    /// Accessibility label present on some thumbnail containers (e.g. authorPhoto in ticker items).
    /// On ticker paid-message outer renderers the label carries the author's @handle.
    /// </summary>
    [JsonPropertyName("accessibility")]
    public Accessibility? Accessibility { get; init; }
}

public record WebCommandMetadata
{
    [JsonPropertyName("ignoreNavigation")]
    public bool IgnoreNavigation { get; init; }

    [JsonPropertyName("rootVe")]
    public int RootVe { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("webPageType")]
    public string? WebPageType { get; init; }
}

public record CommandMetadata
{
    [JsonPropertyName("webCommandMetadata")]
    public WebCommandMetadata? WebCommandMetadata { get; init; }
}

public record LiveChatItemContextMenuEndpoint
{
    [JsonPropertyName("params")]
    public string? Params { get; init; }
}

public record ContextMenuEndpoint
{
    [JsonPropertyName("clickTrackingParams")]
    public string? ClickTrackingParams { get; init; }

    [JsonPropertyName("commandMetadata")]
    public CommandMetadata? CommandMetadata { get; init; }

    [JsonPropertyName("liveChatItemContextMenuEndpoint")]
    public LiveChatItemContextMenuEndpoint? Endpoint { get; init; }
}

// Represents a text segment or emoji within a message
// Using JsonConverter for polymorphism as done in the original project.
[JsonConverter(typeof(Helpers.MessageRunConverter))] // Keep existing converter if suitable
public abstract record MessageRun;

public record MessageText : MessageRun
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("navigationEndpoint")]
    public NavigationEndpoint? NavigationEndpoint { get; init; }

    [JsonPropertyName("bold")]
    public bool Bold { get; init; }

    [JsonPropertyName("italics")]
    public bool Italics { get; init; }

    [JsonPropertyName("deemphasize")]
    public bool Deemphasize { get; init; }

    [JsonPropertyName("fontFace")]
    public string? FontFace { get; init; }

    [JsonPropertyName("textColor")]
    public long? TextColor { get; init; }
}

public record MessageEmoji : MessageRun
{
    [JsonPropertyName("emoji")]
    public Emoji? Emoji { get; init; }

    [JsonPropertyName("variantIds")]
    public List<string>? VariantIds { get; init; }

    [JsonPropertyName("isCustomEmoji")]
    public bool IsCustomEmoji { get; init; }
}

public record Message
{
    [JsonPropertyName("runs")]
    public List<MessageRun>? Runs { get; init; }
}

/// <summary>
/// Represents a text structure that can either be a simple string
/// or a collection of runs (complex formatting).
/// </summary>
public record HeaderText // Made public as it's part of a public model's property
{
    [JsonPropertyName("simpleText")]
    public string? SimpleText { get; init; }

    [JsonPropertyName("runs")]
    public List<MessageRun>? Runs { get; init; }
}

public record CustomThumbnail
{
    [JsonPropertyName("thumbnails")]
    public List<Thumbnail>? Thumbnails { get; init; }
}

public record AuthorBadgeContainer // NEW RECORD
{
    // This property name MUST match the key in the JSON array elements
    [JsonPropertyName("liveChatAuthorBadgeRenderer")]
    public LiveChatAuthorBadgeRenderer? LiveChatAuthorBadgeRenderer { get; init; }
}

public record LiveChatAuthorBadgeRenderer
{
    [JsonPropertyName("customThumbnail")]
    public CustomThumbnail? CustomThumbnail { get; init; }

    [JsonPropertyName("tooltip")]
    public string? Tooltip { get; init; }

    [JsonPropertyName("accessibility")]
    public Accessibility? Accessibility { get; init; }

    [JsonPropertyName("icon")]
    public Icon? Icon { get; init; }
}

public record EmojiImage
{
    [JsonPropertyName("thumbnails")]
    public List<Thumbnail>? Thumbnails { get; init; }

    [JsonPropertyName("accessibility")]
    public Accessibility? Accessibility { get; init; }
}

public record Emoji
{
    [JsonPropertyName("emojiId")]
    public string? EmojiId { get; init; }

    [JsonPropertyName("shortcuts")]
    public List<string>? Shortcuts { get; init; }

    [JsonPropertyName("searchTerms")]
    public List<string>? SearchTerms { get; init; }

    [JsonPropertyName("image")]
    public EmojiImage? Image { get; init; }

    [JsonPropertyName("isCustomEmoji")]
    public bool IsCustomEmoji { get; init; }

    [JsonPropertyName("supportsSkinTone")]
    public bool SupportsSkinTone { get; init; }

    [JsonPropertyName("variantIds")]
    public List<string>? VariantIds { get; init; }
}

public record InvalidationId
{
    [JsonPropertyName("objectSource")]
    public int ObjectSource { get; init; }

    [JsonPropertyName("objectId")]
    public string? ObjectId { get; init; }

    [JsonPropertyName("topic")]
    public string? Topic { get; init; }

    [JsonPropertyName("subscribeToGcmTopics")]
    public bool SubscribeToGcmTopics { get; init; }

    [JsonPropertyName("protoCreationTimestampMs")]
    public string? ProtoCreationTimestampMs { get; init; }
}

public record InvalidationContinuationData
{
    [JsonPropertyName("invalidationId")]
    public InvalidationId? InvalidationId { get; init; }

    [JsonPropertyName("timeoutMs")]
    public int TimeoutMs { get; init; }

    [JsonPropertyName("continuation")]
    public string? Continuation { get; init; }

    [JsonPropertyName("clickTrackingParams")]
    public string? ClickTrackingParams { get; init; }
}

public record MainAppWebResponseContext
{
    [JsonPropertyName("loggedOut")]
    public bool LoggedOut { get; init; }

    [JsonPropertyName("trackingParam")]
    public string? TrackingParam { get; init; }
}

public record Param
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}

public record ServiceTrackingParam
{
    [JsonPropertyName("service")]
    public string? Service { get; init; }

    [JsonPropertyName("params")]
    public List<Param>? Params { get; init; }
}

public record WebResponseContextExtensionData
{
    [JsonPropertyName("hasDecorated")]
    public bool HasDecorated { get; init; }
}

public record ResponseContext
{
    [JsonPropertyName("visitorData")]
    public string? VisitorData { get; init; }

    [JsonPropertyName("serviceTrackingParams")]
    public List<ServiceTrackingParam>? ServiceTrackingParams { get; init; }

    [JsonPropertyName("mainAppWebResponseContext")]
    public MainAppWebResponseContext? MainAppWebResponseContext { get; init; }

    [JsonPropertyName("webResponseContextExtensionData")]
    public WebResponseContextExtensionData? WebResponseContextExtensionData { get; init; }
}

// Base for all item renderers that can appear in chat
public record MessageRendererBase
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("timestampUsec")]
    public string? TimestampUsec { get; init; }

    [JsonPropertyName("authorName")]
    public SimpleText? AuthorName { get; init; }

    [JsonPropertyName("authorPhoto")]
    public ThumbnailList? AuthorPhoto { get; init; }

    [JsonPropertyName("authorBadges")]
    public List<AuthorBadgeContainer>? AuthorBadges { get; init; }

    [JsonPropertyName("authorExternalChannelId")]
    public string? AuthorExternalChannelId { get; init; }

    [JsonPropertyName("contextMenuEndpoint")]
    public ContextMenuEndpoint? ContextMenuEndpoint { get; init; }

    [JsonPropertyName("contextMenuAccessibility")]
    public Accessibility? ContextMenuAccessibility { get; init; }

    [JsonPropertyName("trackingParams")]
    public string? TrackingParams { get; init; }
}

public record LiveChatTextMessageRenderer : MessageRendererBase
{
    [JsonPropertyName("message")]
    public Message? Message { get; init; }

    [JsonPropertyName("beforeContentButtons")]
    public List<BeforeContentButtonContainer>? BeforeContentButtons { get; init; }
}

public record BeforeContentButtonContainer
{
    [JsonPropertyName("buttonViewModel")]
    public ButtonViewModel? ButtonViewModel { get; init; }
}

public record ButtonViewModel
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }
}

public record PurchaseAmountText // Used by PaidMessage and PaidSticker
{
    [JsonPropertyName("simpleText")]
    public string? Text { get; init; }
}

public record LiveChatPaidMessageRenderer : MessageRendererBase
{
    [JsonPropertyName("message")]
    public Message? Message { get; init; } // Optional message

    [JsonPropertyName("purchaseAmountText")]
    public PurchaseAmountText? PurchaseAmountText { get; init; }

    [JsonPropertyName("headerBackgroundColor")]
    public long HeaderBackgroundColor { get; init; }

    [JsonPropertyName("headerTextColor")]
    public long HeaderTextColor { get; init; }

    [JsonPropertyName("bodyBackgroundColor")]
    public long BodyBackgroundColor { get; init; }

    [JsonPropertyName("bodyTextColor")]
    public long BodyTextColor { get; init; }

    [JsonPropertyName("authorNameTextColor")]
    public long AuthorNameTextColor { get; init; }

    [JsonPropertyName("timestampColor")]
    public long TimestampColor { get; init; }

    [JsonPropertyName("isV2Style")]
    public bool IsV2Style { get; init; }

    [JsonPropertyName("textInputBackgroundColor")]
    public long TextInputBackgroundColor { get; init; }

    /// <summary>
    /// Viewer leaderboard rank badge shown on super chats by top-ranking channel-point holders
    /// (e.g. title "#1"). Present on ~5% of paid messages. Use <see cref="LeaderboardBadgeButtonViewModel.Title"/>
    /// to extract the rank string.
    /// </summary>
    [JsonPropertyName("leaderboardBadge")]
    public LeaderboardBadge? LeaderboardBadge { get; init; }

    // Interactive client-side UI buttons (creatorHeartButton, replyButton, pdgLikeButton)
    // are intentionally not modelled — they carry no read-only data for consumers.
}

public record Sticker
{
    [JsonPropertyName("thumbnails")]
    public List<Thumbnail>? Thumbnails { get; init; }

    [JsonPropertyName("accessibility")]
    public Accessibility? Accessibility { get; init; }
}

/// <summary>
/// Linear gradient applied to the sticker card background.
/// Colors are ARGB values (32-bit unsigned packed into long).
/// Stops are parallel arrays: <see cref="Colors"/>[i] at <see cref="Positions"/>[i].
/// </summary>
public record StickerBackgroundGradient
{
    [JsonPropertyName("colors")]
    public List<long>? Colors { get; init; }

    [JsonPropertyName("startX")]
    public double StartX { get; init; }

    [JsonPropertyName("startY")]
    public double StartY { get; init; }

    [JsonPropertyName("endX")]
    public double EndX { get; init; }

    [JsonPropertyName("endY")]
    public double EndY { get; init; }

    /// <summary>Stop offsets in [0,1], parallel to <see cref="Colors"/>.</summary>
    [JsonPropertyName("positions")]
    public List<double>? Positions { get; init; }
}

/// <summary>
/// Educational celebration text and icon shown on 1st-purchase Super Stickers.
/// Text example: "Let's celebrate their 1st Super on a live stream".
/// Image is a client-side resource symbol (e.g. "CELEBRATION").
/// </summary>
public record BumperUserEduContentViewModel
{
    [JsonPropertyName("text")]
    public ViewModelStyledText? Text { get; init; }

    [JsonPropertyName("image")]
    public ViewModelClientResourceImage? Image { get; init; }
}

public record BumperContent
{
    [JsonPropertyName("bumperUserEduContentViewModel")]
    public BumperUserEduContentViewModel? BumperUserEduContentViewModel { get; init; }
}

public record LiveChatItemBumperViewModel
{
    [JsonPropertyName("content")]
    public BumperContent? Content { get; init; }
    // pdgPurchasedBumperLoggingDirectives is logging-only — not modelled
}

public record StickerLowerBumper
{
    [JsonPropertyName("liveChatItemBumperViewModel")]
    public LiveChatItemBumperViewModel? LiveChatItemBumperViewModel { get; init; }
}

public record LiveChatPaidStickerRenderer : MessageRendererBase
{
    [JsonPropertyName("sticker")]
    public Sticker? Sticker { get; init; }

    [JsonPropertyName("purchaseAmountText")]
    public PurchaseAmountText? PurchaseAmountText { get; init; }

    [JsonPropertyName("moneyChipBackgroundColor")]
    public long MoneyChipBackgroundColor { get; init; }

    [JsonPropertyName("moneyChipTextColor")]
    public long MoneyChipTextColor { get; init; }

    [JsonPropertyName("backgroundColor")]
    public long BackgroundColor { get; init; }

    [JsonPropertyName("authorNameTextColor")]
    public long AuthorNameTextColor { get; init; }

    [JsonPropertyName("stickerDisplayWidth")]
    public int StickerDisplayWidth { get; init; }

    [JsonPropertyName("stickerDisplayHeight")]
    public int StickerDisplayHeight { get; init; }

    [JsonPropertyName("isV2Style")]
    public bool IsV2Style { get; init; }

    /// <summary>
    /// Overlay image shown on 1st-purchase Super Stickers (PDG novelty celebration feature).
    /// Contains a static thumbnail list (animation .webp). Decorative only.
    /// </summary>
    [JsonPropertyName("headerOverlayImage")]
    public ThumbnailList? HeaderOverlayImage { get; init; }

    /// <summary>
    /// Lower bumper shown on 1st-purchase Super Stickers ("Let's celebrate their 1st Super…").
    /// Contains educational celebration text and a client-side icon. Decorative only.
    /// </summary>
    [JsonPropertyName("lowerBumper")]
    public StickerLowerBumper? LowerBumper { get; init; }

    /// <summary>
    /// Logging/tracking directives for the PDG novelty celebration feature.
    /// Present only when <see cref="HeaderOverlayImage"/> is set. Not used by the parser.
    /// Intentionally left as opaque blob — structure is pure tracking infrastructure.
    /// </summary>
    [JsonPropertyName("pdgPurchasedNoveltyLoggingDirectives")]
    public JsonElement? PdgPurchasedNoveltyLoggingDirectives { get; init; }

    /// <summary>
    /// Pre-formatted description text for the sticker (e.g. "Sent [StickerName]").
    /// Present on ~98% of paid stickers in ticker context.
    /// </summary>
    [JsonPropertyName("purchaseText")]
    public Message? PurchaseText { get; init; }

    /// <summary>
    /// Linear gradient applied to the sticker card background.
    /// Present on most paid stickers. Colours are ARGB; see <see cref="StickerBackgroundGradient"/>.
    /// </summary>
    [JsonPropertyName("backgroundGradient")]
    public StickerBackgroundGradient? BackgroundGradient { get; init; }
}

public record LiveChatMembershipItemRenderer : MessageRendererBase
{
    [JsonPropertyName("headerPrimaryText")]
    public HeaderText? HeaderPrimaryText { get; init; }

    [JsonPropertyName("headerSubtext")]
    public HeaderText? HeaderSubtext { get; init; } // Can be SimpleText or Message

    [JsonPropertyName("message")]
    public Message? Message { get; init; }

    /// <summary>
    /// True on milestone membership items that carry no message body (observed on ~3.7% of instances).
    /// </summary>
    [JsonPropertyName("empty")]
    public bool Empty { get; init; }
}

public record SponsorshipsHeaderContainer
{
    [JsonPropertyName("liveChatSponsorshipsHeaderRenderer")]
    public LiveChatSponsorshipsHeaderRenderer? LiveChatSponsorshipsHeaderRenderer { get; init; }
}

public record Source // Image source, used within IconSource
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }
    // [JsonPropertyName("clientResource")] public ClientResource? ClientResource { get; init; } // Simplified for now
    // [JsonPropertyName("processor")] public Processor? Processor { get; init; } // Simplified for now
}

public record IconSource // Used for header image in sponsorships
{
    [JsonPropertyName("sources")]
    public List<Source>? Sources { get; init; }
}

public record LiveChatSponsorshipsHeaderRenderer // Used within Gift Purchase
{
    [JsonPropertyName("authorName")]
    public SimpleText? AuthorName { get; init; }

    [JsonPropertyName("authorPhoto")]
    public ThumbnailList? AuthorPhoto { get; init; }

    [JsonPropertyName("primaryText")]
    public HeaderText? PrimaryText { get; init; }

    [JsonPropertyName("authorBadges")]
    public List<AuthorBadgeContainer>? AuthorBadges { get; init; }

    [JsonPropertyName("contextMenuEndpoint")]
    public ContextMenuEndpoint? ContextMenuEndpoint { get; init; }

    [JsonPropertyName("contextMenuAccessibility")]
    public Accessibility? ContextMenuAccessibility { get; init; }

    [JsonPropertyName("image")]
    public IconSource? Image { get; init; }
}

public record LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer : MessageRendererBase // Note: Inherits Base for consistency, though some fields might be in Header
{
    // Timestamp, AuthorChannelId are in base
    [JsonPropertyName("header")]
    public SponsorshipsHeaderContainer? Header { get; init; }
    // ID might be in base or header, needs verification
}

public record LiveChatSponsorshipsGiftRedemptionAnnouncementRenderer : MessageRendererBase
{
    // ID, Timestamp, AuthorChannelId, AuthorName, ThumbnailList, Badges, ContextMenu in base
    [JsonPropertyName("message")]
    public Message? Message { get; init; } // The "Welcome!" message
}

public record LiveChatPlaceholderItemRenderer // Used for deleted/pending messages
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("timestampUsec")]
    public string? TimestampUsec { get; init; }
}

// ── YouTube Jewels / virtual gifting ─────────────────────────────────────────

// Shared styled-text container used by view-model renderers (content + optional style ranges).
public record ViewModelStyledText
{
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("styleRuns")]
    public List<ViewModelStyleRun>? StyleRuns { get; init; }
}

public record ViewModelStyleRun
{
    [JsonPropertyName("startIndex")]
    public int StartIndex { get; init; }

    [JsonPropertyName("length")]
    public int Length { get; init; }
}

// Image container that references a client-side resource (symbol name + ARGB color tint)
// rather than a URL-based thumbnail. Used in giftMessageViewModel.image and
// liveChatTickerFanzoneViewModel.tickerIcon.
public record ViewModelClientResourceImage
{
    [JsonPropertyName("sources")]
    public List<ViewModelClientResourceSource>? Sources { get; init; }
}

public record ViewModelClientResourceSource
{
    [JsonPropertyName("clientResource")]
    public ViewModelClientResource? ClientResource { get; init; }
}

public record ViewModelClientResource
{
    [JsonPropertyName("imageName")]
    public string? ImageName { get; init; }

    [JsonPropertyName("imageColor")]
    public long? ImageColor { get; init; }
}

// avatarViewModel — used as giftMessageViewModel.authorAvatar.avatarViewModel
public record AvatarViewModel
{
    [JsonPropertyName("image")]
    public IconSource? Image { get; init; }
    // avatarImageSize is a display hint ("AVATAR_SIZE_XS"), not needed
}

public record AvatarViewModelContainer
{
    [JsonPropertyName("avatarViewModel")]
    public AvatarViewModel? AvatarViewModel { get; init; }
}

// giftMessageViewModel — virtual gift sent by a viewer using YouTube Jewels
public record GiftMessageViewModel
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("text")]
    public ViewModelStyledText? Text { get; init; }

    [JsonPropertyName("authorName")]
    public ViewModelStyledText? AuthorName { get; init; }

    [JsonPropertyName("image")]
    public ViewModelClientResourceImage? Image { get; init; }

    [JsonPropertyName("imageA11yLabel")]
    public string? ImageA11yLabel { get; init; }

    /// <summary>
    /// URL-based gift item image (~45% of gifts). Distinct from <see cref="Image"/>
    /// which is a client-side resource symbol; this carries a real CDN thumbnail URL.
    /// </summary>
    [JsonPropertyName("giftImage")]
    public IconSource? GiftImage { get; init; }

    [JsonPropertyName("giftImageA11yLabel")]
    public string? GiftImageA11yLabel { get; init; }

    /// <summary>
    /// Author avatar as an avatarViewModel container (~45% of gifts).
    /// Access the URL via <c>AuthorAvatar?.AvatarViewModel?.Image?.Sources</c>.
    /// </summary>
    [JsonPropertyName("authorAvatar")]
    public AvatarViewModelContainer? AuthorAvatar { get; init; }

    [JsonPropertyName("rendererContext")]
    public JsonElement? RendererContext { get; init; }
}

// ─────────────────────────────────────────────────────────────────────────────

// Polymorphic container for the 'item' in AddChatItemAction
public record AddChatItemActionItem
{
    [JsonPropertyName("liveChatTextMessageRenderer")]
    public LiveChatTextMessageRenderer? LiveChatTextMessageRenderer { get; init; }

    [JsonPropertyName("liveChatPaidMessageRenderer")]
    public LiveChatPaidMessageRenderer? LiveChatPaidMessageRenderer { get; init; }

    [JsonPropertyName("liveChatMembershipItemRenderer")]
    public LiveChatMembershipItemRenderer? LiveChatMembershipItemRenderer { get; init; }

    [JsonPropertyName("liveChatPaidStickerRenderer")]
    public LiveChatPaidStickerRenderer? LiveChatPaidStickerRenderer { get; init; }

    [JsonPropertyName("liveChatSponsorshipsGiftPurchaseAnnouncementRenderer")]
    public LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer? LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer { get; init; }

    [JsonPropertyName("liveChatSponsorshipsGiftRedemptionAnnouncementRenderer")]
    public LiveChatSponsorshipsGiftRedemptionAnnouncementRenderer? LiveChatSponsorshipsGiftRedemptionAnnouncementRenderer { get; init; }

    [JsonPropertyName("liveChatPlaceholderItemRenderer")]
    public LiveChatPlaceholderItemRenderer? LiveChatPlaceholderItemRenderer { get; init; }

    [JsonPropertyName("liveChatViewerEngagementMessageRenderer")]
    public LiveChatViewerEngagementMessageRenderer? LiveChatViewerEngagementMessageRenderer { get; init; }

    [JsonPropertyName("giftMessageViewModel")]
    public GiftMessageViewModel? GiftMessageViewModel { get; init; }
}

public record AddChatItemAction
{
    [JsonPropertyName("item")]
    public AddChatItemActionItem? Item { get; init; }

    [JsonPropertyName("clientId")]
    public string? ClientId { get; init; }
}

public record LiveChatTickerShowItemRenderer
{
    [JsonPropertyName("liveChatPaidMessageRenderer")]
    public LiveChatPaidMessageRenderer? LiveChatPaidMessageRenderer { get; init; }

    [JsonPropertyName("liveChatMembershipItemRenderer")]
    public LiveChatMembershipItemRenderer? LiveChatMembershipItemRenderer { get; init; }

    [JsonPropertyName("liveChatPaidStickerRenderer")]
    public LiveChatPaidStickerRenderer? LiveChatPaidStickerRenderer { get; init; }

    [JsonPropertyName("liveChatSponsorshipsGiftPurchaseAnnouncementRenderer")]
    public LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer? LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer { get; init; }
}

public record ShowLiveChatItemEndpoint
{
    [JsonPropertyName("renderer")]
    public LiveChatTickerShowItemRenderer? Renderer { get; init; }
}

public record TickerShowItemEndpoint
{
    [JsonPropertyName("showLiveChatItemEndpoint")]
    public ShowLiveChatItemEndpoint? ShowLiveChatItemEndpoint { get; init; }
}

public record LeaderboardBadgeButtonViewModel
{
    /// <summary>Rank string, e.g. "#1".</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }
}

public record LeaderboardBadge
{
    [JsonPropertyName("buttonViewModel")]
    public LeaderboardBadgeButtonViewModel? ButtonViewModel { get; init; }
}

public record LiveChatTickerPaidMessageItemRenderer
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("showItemEndpoint")]
    public TickerShowItemEndpoint? ShowItemEndpoint { get; init; }

    [JsonPropertyName("authorExternalChannelId")]
    public string? AuthorExternalChannelId { get; init; }

    [JsonPropertyName("authorPhoto")]
    public ThumbnailList? AuthorPhoto { get; init; }

    /// <summary>The @handle of the author, e.g. "@TurtleCubes".</summary>
    [JsonPropertyName("authorUsername")]
    public SimpleText? AuthorUsername { get; init; }

    [JsonPropertyName("startBackgroundColor")]
    public long StartBackgroundColor { get; init; }

    [JsonPropertyName("endBackgroundColor")]
    public long EndBackgroundColor { get; init; }

    [JsonPropertyName("amountTextColor")]
    public long AmountTextColor { get; init; }

    /// <summary>Ticker bar display duration in seconds (int, distinct from the action's string durationSec).</summary>
    [JsonPropertyName("durationSec")]
    public int DurationSec { get; init; }

    [JsonPropertyName("fullDurationSec")]
    public int FullDurationSec { get; init; }

    [JsonPropertyName("trackingParams")]
    public string? TrackingParams { get; init; }
}

public record LiveChatTickerSponsorItemRenderer
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("showItemEndpoint")]
    public TickerShowItemEndpoint? ShowItemEndpoint { get; init; }

    [JsonPropertyName("authorExternalChannelId")]
    public string? AuthorExternalChannelId { get; init; }

    /// <summary>Profile photo of the member shown in the ticker bar.</summary>
    [JsonPropertyName("sponsorPhoto")]
    public ThumbnailList? SponsorPhoto { get; init; }

    /// <summary>Detail text, e.g. "Member" or "sent 10 Gift Memberships".</summary>
    [JsonPropertyName("detailText")]
    public HeaderText? DetailText { get; init; }

    [JsonPropertyName("detailTextColor")]
    public long DetailTextColor { get; init; }

    /// <summary>Optional icon beside the detail text (e.g. STAR_CIRCLE_RIBBON for gift purchases).</summary>
    [JsonPropertyName("detailIcon")]
    public Icon? DetailIcon { get; init; }

    [JsonPropertyName("startBackgroundColor")]
    public long StartBackgroundColor { get; init; }

    [JsonPropertyName("endBackgroundColor")]
    public long EndBackgroundColor { get; init; }

    /// <summary>Ticker bar display duration in seconds.</summary>
    [JsonPropertyName("durationSec")]
    public int DurationSec { get; init; }

    [JsonPropertyName("fullDurationSec")]
    public int FullDurationSec { get; init; }

    [JsonPropertyName("trackingParams")]
    public string? TrackingParams { get; init; }
}

public record LiveChatTickerPaidStickerItemRenderer
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("showItemEndpoint")]
    public TickerShowItemEndpoint? ShowItemEndpoint { get; init; }

    [JsonPropertyName("authorExternalChannelId")]
    public string? AuthorExternalChannelId { get; init; }

    [JsonPropertyName("authorPhoto")]
    public ThumbnailList? AuthorPhoto { get; init; }

    /// <summary>Sticker image(s) shown directly in the ticker bar.</summary>
    [JsonPropertyName("tickerThumbnails")]
    public List<ThumbnailList>? TickerThumbnails { get; init; }

    [JsonPropertyName("startBackgroundColor")]
    public long StartBackgroundColor { get; init; }

    [JsonPropertyName("endBackgroundColor")]
    public long EndBackgroundColor { get; init; }

    /// <summary>Ticker bar display duration in seconds.</summary>
    [JsonPropertyName("durationSec")]
    public int DurationSec { get; init; }

    [JsonPropertyName("fullDurationSec")]
    public int FullDurationSec { get; init; }

    [JsonPropertyName("trackingParams")]
    public string? TrackingParams { get; init; }
}

public record AddLiveChatTickerItemActionItem
{
    [JsonPropertyName("liveChatTickerPaidMessageItemRenderer")]
    public LiveChatTickerPaidMessageItemRenderer? LiveChatTickerPaidMessageItemRenderer { get; init; }

    [JsonPropertyName("liveChatTickerSponsorItemRenderer")]
    public LiveChatTickerSponsorItemRenderer? LiveChatTickerSponsorItemRenderer { get; init; }

    [JsonPropertyName("liveChatTickerPaidStickerItemRenderer")]
    public LiveChatTickerPaidStickerItemRenderer? LiveChatTickerPaidStickerItemRenderer { get; init; }
}

public record AddLiveChatTickerItemAction
{
    [JsonPropertyName("item")]
    public AddLiveChatTickerItemActionItem? Item { get; init; }

    [JsonPropertyName("durationSec")]
    public string? DurationSec { get; init; }
}

public record RemoveChatItemAction
{
    [JsonPropertyName("targetItemId")]
    public string? TargetItemId { get; init; }
}

// Polymorphic container for replacement item
public record ReplacementItem
{
    [JsonPropertyName("liveChatTextMessageRenderer")]
    public LiveChatTextMessageRenderer? LiveChatTextMessageRenderer { get; init; }

    [JsonPropertyName("liveChatPlaceholderItemRenderer")]
    public LiveChatPlaceholderItemRenderer? LiveChatPlaceholderItemRenderer { get; init; }
    // Add other potential replacement renderers if needed
}

public record ReplaceChatItemAction
{
    [JsonPropertyName("targetItemId")]
    public string? TargetItemId { get; init; }

    [JsonPropertyName("replacementItem")]
    public ReplacementItem? ReplacementItem { get; init; }
}

public record RemoveChatItemByAuthorAction
{
    [JsonPropertyName("externalChannelId")]
    public string? ExternalChannelId { get; init; }
}

// =============================================
// Poll models (updateLiveChatPollAction, showLiveChatActionPanelAction)
// =============================================

public record PollChoice
{
    [JsonPropertyName("text")]
    public Message? Text { get; init; }

    [JsonPropertyName("selected")]
    public bool Selected { get; init; }

    [JsonPropertyName("voteRatio")]
    public double VoteRatio { get; init; }

    [JsonPropertyName("votePercentage")]
    public SimpleText? VotePercentage { get; init; }
}

public record PollHeaderRenderer
{
    [JsonPropertyName("pollQuestion")]
    public Message? PollQuestion { get; init; }

    [JsonPropertyName("thumbnail")]
    public ThumbnailList? Thumbnail { get; init; }

    [JsonPropertyName("metadataText")]
    public Message? MetadataText { get; init; }

    [JsonPropertyName("liveChatPollType")]
    public string? LiveChatPollType { get; init; }
}

public record PollHeader
{
    [JsonPropertyName("pollHeaderRenderer")]
    public PollHeaderRenderer? PollHeaderRenderer { get; init; }
}

public record PollRenderer
{
    [JsonPropertyName("liveChatPollId")]
    public string? LiveChatPollId { get; init; }

    [JsonPropertyName("choices")]
    public List<PollChoice>? Choices { get; init; }

    [JsonPropertyName("header")]
    public PollHeader? Header { get; init; }
}

public record PollToUpdate
{
    [JsonPropertyName("pollRenderer")]
    public PollRenderer? PollRenderer { get; init; }
}

public record UpdateLiveChatPollAction
{
    [JsonPropertyName("pollToUpdate")]
    public PollToUpdate? PollToUpdate { get; init; }
}

public record LiveChatActionPanelRendererContents
{
    [JsonPropertyName("pollRenderer")]
    public PollRenderer? PollRenderer { get; init; }
}

public record LiveChatActionPanelRenderer
{
    [JsonPropertyName("contents")]
    public LiveChatActionPanelRendererContents? Contents { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }
}

public record PanelToShow
{
    [JsonPropertyName("liveChatActionPanelRenderer")]
    public LiveChatActionPanelRenderer? LiveChatActionPanelRenderer { get; init; }
}

public record ShowLiveChatActionPanelAction
{
    [JsonPropertyName("panelToShow")]
    public PanelToShow? PanelToShow { get; init; }
}

public record CloseLiveChatActionPanelAction
{
    [JsonPropertyName("targetPanelId")]
    public string? TargetPanelId { get; init; }

    /// <summary>
    /// When true the panel should not fire its dismiss command on close (observed on poll close).
    /// </summary>
    [JsonPropertyName("skipOnDismissCommand")]
    public bool SkipOnDismissCommand { get; init; }
}

// =============================================
// Banner models (addBannerToLiveChatCommand, removeBannerForLiveChatCommand)
// =============================================

public record LiveChatBannerHeaderRenderer
{
    [JsonPropertyName("icon")]
    public Icon? Icon { get; init; }

    [JsonPropertyName("text")]
    public Message? Text { get; init; }
}

public record LiveChatBannerHeaderContainer
{
    [JsonPropertyName("liveChatBannerHeaderRenderer")]
    public LiveChatBannerHeaderRenderer? LiveChatBannerHeaderRenderer { get; init; }
}

/// <summary>
/// The command payload inside a <see cref="BannerInlineButtonRenderer"/>.
/// Carries either a <c>watchEndpoint</c> ("Go now" → direct video link) or a
/// <c>urlEndpoint</c> ("Learn more" → support page link).
/// </summary>
public record BannerButtonCommand
{
    [JsonPropertyName("commandMetadata")]
    public CommandMetadata? CommandMetadata { get; init; }

    [JsonPropertyName("watchEndpoint")]
    public WatchEndpoint? WatchEndpoint { get; init; }

    [JsonPropertyName("urlEndpoint")]
    public UrlEndpoint? UrlEndpoint { get; init; }
}

public record BannerInlineButtonRenderer
{
    [JsonPropertyName("text")]
    public Message? Text { get; init; }

    [JsonPropertyName("command")]
    public BannerButtonCommand? Command { get; init; }

    [JsonPropertyName("style")]
    public string? Style { get; init; }

    [JsonPropertyName("size")]
    public string? Size { get; init; }

    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; init; }

    [JsonPropertyName("accessibility")]
    public Accessibility? Accessibility { get; init; }
}

public record InlineActionButtonContainer
{
    [JsonPropertyName("buttonRenderer")]
    public BannerInlineButtonRenderer? ButtonRenderer { get; init; }
}

public record LiveChatBannerRedirectRenderer
{
    [JsonPropertyName("bannerMessage")]
    public Message? BannerMessage { get; init; }

    [JsonPropertyName("authorPhoto")]
    public ThumbnailList? AuthorPhoto { get; init; }

    [JsonPropertyName("inlineActionButton")]
    public InlineActionButtonContainer? InlineActionButton { get; init; }
}

/// <summary>
/// The structured text payload inside a <c>liveChatBannerChatSummaryRenderer</c>.
/// Runs typically contain: bold title, newline, deemphasized disclaimer, newline, body text.
/// </summary>
public record LiveChatBannerChatSummaryRenderer
{
    [JsonPropertyName("liveChatSummaryId")]
    public string? LiveChatSummaryId { get; init; }

    [JsonPropertyName("chatSummary")]
    public Message? ChatSummary { get; init; }

    // UI-only interaction fields — not modelled, silently absorbed by deserializer
    // collapsedStateEntityKey, dislikeFeedbackButton, likeFeedbackButton, icon
}

/// <summary>
/// Pinned Q&amp;A question banner (<c>liveChatCallForQuestionsRenderer</c>).
/// Shown when a streamer highlights a viewer's question for the audience.
/// </summary>
public record LiveChatCallForQuestionsRenderer
{
    [JsonPropertyName("questionMessage")]
    public Message? QuestionMessage { get; init; }

    [JsonPropertyName("creatorAuthorName")]
    public SimpleText? CreatorAuthorName { get; init; }

    [JsonPropertyName("creatorAvatar")]
    public ThumbnailList? CreatorAvatar { get; init; }

    [JsonPropertyName("featureLabel")]
    public SimpleText? FeatureLabel { get; init; }
}

public record LiveChatBannerRendererContents
{
    [JsonPropertyName("liveChatTextMessageRenderer")]
    public LiveChatTextMessageRenderer? LiveChatTextMessageRenderer { get; init; }

    [JsonPropertyName("liveChatBannerRedirectRenderer")]
    public LiveChatBannerRedirectRenderer? LiveChatBannerRedirectRenderer { get; init; }

    [JsonPropertyName("liveChatBannerChatSummaryRenderer")]
    public LiveChatBannerChatSummaryRenderer? LiveChatBannerChatSummaryRenderer { get; init; }

    [JsonPropertyName("liveChatCallForQuestionsRenderer")]
    public LiveChatCallForQuestionsRenderer? LiveChatCallForQuestionsRenderer { get; init; }
}

/// <summary>Auto-collapse timer present on pinned-message banners.</summary>
public record BannerAutoCollapseDelay
{
    /// <summary>Seconds until the banner auto-collapses (string-encoded integer).</summary>
    [JsonPropertyName("seconds")]
    public string? Seconds { get; init; }
}

/// <summary>
/// Properties embedded inside <see cref="LiveChatBannerRenderer"/>.
/// Observed on pinned-message and chat-summary banners (auto-collapse timer, ephemeral flag).
/// </summary>
public record LiveChatBannerRendererProperties
{
    /// <summary>When true the banner auto-dismisses (also observed at the renderer level for chat-summary banners).</summary>
    [JsonPropertyName("isEphemeral")]
    public bool IsEphemeral { get; init; }

    [JsonPropertyName("autoCollapseDelay")]
    public BannerAutoCollapseDelay? AutoCollapseDelay { get; init; }

    [JsonPropertyName("bannerCollapsedStateEntityKey")]
    public string? BannerCollapsedStateEntityKey { get; init; }
}

/// <summary>
/// Properties at the <see cref="AddBannerToLiveChatCommand"/> level for ephemeral banners
/// (e.g. cross-channel redirect and raid notifications).
/// </summary>
public record AddBannerCommandProperties
{
    /// <summary>When true the banner auto-dismisses after <see cref="BannerTimeoutMs"/> ms.</summary>
    [JsonPropertyName("isEphemeral")]
    public bool IsEphemeral { get; init; }

    /// <summary>Duration in milliseconds before the ephemeral banner is dismissed (string-encoded).</summary>
    [JsonPropertyName("bannerTimeoutMs")]
    public string? BannerTimeoutMs { get; init; }
}

public record LiveChatBannerRenderer
{
    [JsonPropertyName("header")]
    public LiveChatBannerHeaderContainer? Header { get; init; }

    [JsonPropertyName("contents")]
    public LiveChatBannerRendererContents? Contents { get; init; }

    [JsonPropertyName("actionId")]
    public string? ActionId { get; init; }

    [JsonPropertyName("bannerType")]
    public string? BannerType { get; init; }

    /// <summary>Whether this banner can be stacked alongside another active banner.</summary>
    [JsonPropertyName("isStackable")]
    public bool IsStackable { get; init; }

    /// <summary>True when the viewer is the channel creator (viewer-side flag, not author metadata).</summary>
    [JsonPropertyName("viewerIsCreator")]
    public bool ViewerIsCreator { get; init; }

    /// <summary>Timing and state properties for pinned-message banners (auto-collapse delay).</summary>
    [JsonPropertyName("bannerProperties")]
    public LiveChatBannerRendererProperties? BannerProperties { get; init; }
}

public record BannerRendererContainer
{
    [JsonPropertyName("liveChatBannerRenderer")]
    public LiveChatBannerRenderer? LiveChatBannerRenderer { get; init; }
}

public record AddBannerToLiveChatCommand
{
    [JsonPropertyName("bannerRenderer")]
    public BannerRendererContainer? BannerRenderer { get; init; }

    /// <summary>
    /// Ephemeral-banner properties (auto-dismiss timeout) present on redirect banners.
    /// Located at the command level, not inside the renderer.
    /// </summary>
    [JsonPropertyName("bannerProperties")]
    public AddBannerCommandProperties? BannerProperties { get; init; }
}

public record RemoveBannerForLiveChatCommand
{
    [JsonPropertyName("targetActionId")]
    public string? TargetActionId { get; init; }
}

// Polymorphic container for actions
public record Action
{
    [JsonPropertyName("addChatItemAction")]
    public AddChatItemAction? AddChatItemAction { get; init; }

    [JsonPropertyName("removeChatItemAction")]
    public RemoveChatItemAction? RemoveChatItemAction { get; init; }

    [JsonPropertyName("replaceChatItemAction")]
    public ReplaceChatItemAction? ReplaceChatItemAction { get; init; }

    [JsonPropertyName("removeChatItemByAuthorAction")]
    public RemoveChatItemByAuthorAction? RemoveChatItemByAuthorAction { get; init; }

    [JsonPropertyName("addLiveChatTickerItemAction")]
    public AddLiveChatTickerItemAction? AddLiveChatTickerItemAction { get; init; }

    [JsonPropertyName("changeEngagementPanelVisibilityAction")]
    public JsonElement? ChangeEngagementPanelVisibilityAction { get; init; }

    [JsonPropertyName("signalAction")]
    public SignalAction? SignalAction { get; init; }

    [JsonPropertyName("markChatItemsByAuthorAsDeletedAction")]
    public RemoveChatItemByAuthorAction? MarkChatItemsByAuthorAsDeletedAction { get; init; }

    [JsonPropertyName("updateLiveChatPollAction")]
    public UpdateLiveChatPollAction? UpdateLiveChatPollAction { get; init; }

    [JsonPropertyName("showLiveChatActionPanelAction")]
    public ShowLiveChatActionPanelAction? ShowLiveChatActionPanelAction { get; init; }

    [JsonPropertyName("closeLiveChatActionPanelAction")]
    public CloseLiveChatActionPanelAction? CloseLiveChatActionPanelAction { get; init; }

    [JsonPropertyName("addBannerToLiveChatCommand")]
    public AddBannerToLiveChatCommand? AddBannerToLiveChatCommand { get; init; }

    [JsonPropertyName("removeBannerForLiveChatCommand")]
    public RemoveBannerForLiveChatCommand? RemoveBannerForLiveChatCommand { get; init; }

    // Gift animation overlay widgets — companion to giftMessageViewModel, purely visual.
    [JsonPropertyName("addInteractivityWidgetAction")]
    public JsonElement? AddInteractivityWidgetAction { get; init; }

    [JsonPropertyName("updateOrAddInteractivityWidgetAction")]
    public JsonElement? UpdateOrAddInteractivityWidgetAction { get; init; }

    // Fallback for unhandled actions
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

public record LiveChatContinuation
{
    [JsonPropertyName("continuations")]
    public List<Continuation>? Continuations { get; init; }

    [JsonPropertyName("actions")]
    public List<Action>? Actions { get; init; }

    [JsonPropertyName("trackingParams")]
    public string? TrackingParams { get; init; }
}

public record Continuation // Container for different continuation types
{
    [JsonPropertyName("invalidationContinuationData")]
    public InvalidationContinuationData? InvalidationContinuationData { get; init; }

    [JsonPropertyName("timedContinuationData")]
    public TimedContinuationData? TimedContinuationData { get; init; }
    // Add other continuation types if observed (e.g., liveChatReplayContinuationData)
}

public record TimedContinuationData // Another type of continuation token
{
    [JsonPropertyName("timeoutMs")]
    public int TimeoutMs { get; init; }

    [JsonPropertyName("continuation")]
    public string? Continuation { get; init; }

    [JsonPropertyName("clickTrackingParams")]
    public string? ClickTrackingParams { get; init; }
}

// --- Framework Updates Structures ---
public record Timestamp
{
    [JsonPropertyName("seconds")]
    public string? Seconds { get; init; }

    [JsonPropertyName("nanos")]
    public int Nanos { get; init; }
}

public record MutationPayload // Polymorphic container for mutation payloads
{
    // Add specific payloads as they are identified (e.g., emoji fountain, toolbars)
    // [JsonPropertyName("emojiFountainDataEntity")] public EmojiFountainDataEntity? EmojiFountainDataEntity { get; init; }
    // [JsonPropertyName("engagementToolbarStateEntityPayload")] public EngagementToolbarStateEntityPayload? EngagementToolbarStateEntityPayload { get; init; }
    // [JsonPropertyName("replyCountEntity")] public ReplyCountEntity? ReplyCountEntity { get; init; }
    // [JsonPropertyName("booleanEntity")] public BooleanEntity? BooleanEntity { get; init; }

    // Fallback using JsonObject or ExtensionData
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

public record Mutation
{
    [JsonPropertyName("entityKey")]
    public string? EntityKey { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; } // e.g., "ENTITY_MUTATION_TYPE_REPLACE"

    [JsonPropertyName("payload")]
    public MutationPayload? Payload { get; init; }
}

public record EntityBatchUpdate
{
    [JsonPropertyName("mutations")]
    public List<Mutation>? Mutations { get; init; }

    [JsonPropertyName("timestamp")]
    public Timestamp? Timestamp { get; init; }
}

public record FrameworkUpdates
{
    [JsonPropertyName("entityBatchUpdate")]
    public EntityBatchUpdate? EntityBatchUpdate { get; init; }
}

// --- End Framework Updates ---

public record LiveChatStreamingResponseExtension
{
    [JsonPropertyName("lastPublishAtUsec")]
    public string? LastPublishAtUsec { get; init; }
}

public record ContinuationContents
{
    [JsonPropertyName("liveChatContinuation")]
    public LiveChatContinuation? LiveChatContinuation { get; init; }

    [JsonPropertyName("frameworkUpdates")]
    public FrameworkUpdates? FrameworkUpdates { get; init; }

    // These might be redundant if present at root, kept for robustness
    [JsonPropertyName("liveChatStreamingResponseExtension")]
    public LiveChatStreamingResponseExtension? LiveChatStreamingResponseExtension { get; init; }

    [JsonPropertyName("responseContext")]
    public ResponseContext? ResponseContext { get; init; }

    [JsonPropertyName("trackingParams")]
    public string? TrackingParams { get; init; }
}

// --- Structures specifically related to Reaction Control Panel (Type_4 from original) ---
// Simplified here as they are less critical than chat messages for basic functionality
// Add more detail if reaction panel interaction is needed.
public record ReactionControlPanelOverlayViewModel
{
    [JsonPropertyName("reactionControlPanel")]
    public JsonObject? ReactionControlPanel { get; init; } // Use JsonObject for simplicity

    [JsonPropertyName("liveReactionsSettingEntityKey")]
    public string? LiveReactionsSettingEntityKey { get; init; }

    [JsonPropertyName("emojiFountain")]
    public JsonObject? EmojiFountain { get; init; } // Use JsonObject for simplicity
    // Add loggingDirectives etc. if needed
}

// --- Structures specifically related to Signal Action (Type_5 from original) ---
public record SignalAction
{
    [JsonPropertyName("signal")]
    public string? Signal { get; init; } // e.g., "HIDE_LIVE_CHAT"
}

// --- Structures specifically related to Participant Renderer (Type_6 from original) ---
public record LiveChatParticipantRenderer
{
    [JsonPropertyName("authorName")]
    public SimpleText? AuthorName { get; init; }

    [JsonPropertyName("authorPhoto")]
    public ThumbnailList? AuthorPhoto { get; init; }

    [JsonPropertyName("authorBadges")]
    public List<AuthorBadgeContainer>? AuthorBadges { get; init; }
}

// --- Navigation Endpoint ---
public record WatchEndpoint
{
    [JsonPropertyName("videoId")]
    public string? VideoId { get; init; }

    [JsonPropertyName("nofollow")]
    public bool Nofollow { get; init; }
}

public record NavigationEndpoint
{
    [JsonPropertyName("clickTrackingParams")]
    public string? ClickTrackingParams { get; init; }

    [JsonPropertyName("commandMetadata")]
    public CommandMetadata? CommandMetadata { get; init; }

    [JsonPropertyName("urlEndpoint")]
    public UrlEndpoint? UrlEndpoint { get; init; }

    [JsonPropertyName("watchEndpoint")]
    public WatchEndpoint? WatchEndpoint { get; init; }
}

public record UrlEndpoint
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("target")]
    public string? Target { get; init; } // e.g., "TARGET_NEW_WINDOW"

    [JsonPropertyName("nofollow")]
    public bool Nofollow { get; init; }
}

public record LiveChatViewerEngagementMessageRenderer
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("timestampUsec")]
    public string? TimestampUsec { get; init; }

    [JsonPropertyName("icon")]
    public Icon? Icon { get; init; }

    [JsonPropertyName("message")]
    public Message? Message { get; init; }

    [JsonPropertyName("actionButton")]
    public EngagementActionButton? ActionButton { get; init; }
}

public record EngagementActionButton
{
    [JsonPropertyName("buttonRenderer")]
    public EngagementButtonRenderer? ButtonRenderer { get; init; }
}

public record EngagementButtonRenderer
{
    [JsonPropertyName("navigationEndpoint")]
    public NavigationEndpoint? NavigationEndpoint { get; init; }
}

// ==========================================================================
// The Root API Response Record
// ==========================================================================
public record LiveChatResponse
{
    [JsonPropertyName("responseContext")]
    public ResponseContext? ResponseContext { get; init; }

    [JsonPropertyName("continuationContents")]
    public ContinuationContents? ContinuationContents { get; init; }

    [JsonPropertyName("liveChatStreamingResponseExtension")]
    public LiveChatStreamingResponseExtension? LiveChatStreamingResponseExtension { get; init; }

    [JsonPropertyName("frameworkUpdates")]
    public FrameworkUpdates? FrameworkUpdates { get; init; }

    [JsonPropertyName("trackingParams")]
    public string? TrackingParams { get; init; }

    // --- Properties corresponding to unique top-level structures (less common) ---
    [JsonPropertyName("invalidationContinuationData")]
    public InvalidationContinuationData? InvalidationContinuationData { get; init; } // Type_1 like

    // [JsonPropertyName("emojiPickerRenderer")] public EmojiPickerRenderer? EmojiPickerRenderer { get; init; } // Type_3 like - Simplified for now
    [JsonPropertyName("reactionControlPanelOverlayViewModel")]
    public ReactionControlPanelOverlayViewModel? ReactionControlPanelOverlayViewModel { get; init; } // Type_4 like

    [JsonPropertyName("clickTrackingParams")]
    public string? ClickTrackingParamsType5 { get; init; } // From Type_5 - Renamed slightly to avoid conflict

    [JsonPropertyName("signalAction")]
    public SignalAction? SignalAction { get; init; } // Type_5 like

    [JsonPropertyName("liveChatParticipantRenderer")]
    public LiveChatParticipantRenderer? LiveChatParticipantRenderer { get; init; } // Type_6 like (single participant)
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
