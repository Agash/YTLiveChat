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

public record AuthorPhoto
{
    [JsonPropertyName("thumbnails")]
    public List<Thumbnail>? Thumbnails { get; init; }
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

    [JsonPropertyName("fontFace")]
    public string? FontFace { get; init; }
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
    public AuthorPhoto? AuthorPhoto { get; init; }

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
    // [JsonPropertyName("beforeContentButtons")] public List<ButtonContainer>? BeforeContentButtons { get; init; } // Include if needed
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

    // [JsonPropertyName("creatorHeartButton")]
    // public CreatorHeartButton? CreatorHeartButton { get; init; } // Include if needed
    // [JsonPropertyName("replyButton")] public PdgReplyButton? ReplyButton { get; init; } // Include if needed
}

public record Sticker
{
    [JsonPropertyName("thumbnails")]
    public List<Thumbnail>? Thumbnails { get; init; }

    [JsonPropertyName("accessibility")]
    public Accessibility? Accessibility { get; init; }
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
}

public record LiveChatMembershipItemRenderer : MessageRendererBase
{
    [JsonPropertyName("headerPrimaryText")]
    public HeaderText? HeaderPrimaryText { get; init; }

    [JsonPropertyName("headerSubtext")]
    public HeaderText? HeaderSubtext { get; init; } // Can be SimpleText or Message

    [JsonPropertyName("message")]
    public Message? Message { get; init; }
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
    public AuthorPhoto? AuthorPhoto { get; init; }

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
    // ID, Timestamp, AuthorChannelId, AuthorName, AuthorPhoto, Badges, ContextMenu in base
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

    // Add other potential renderers here if observed (e.g., viewer engagement, banners moved inside items)
    [JsonPropertyName("liveChatViewerEngagementMessageRenderer")]
    public JsonObject? LiveChatViewerEngagementMessageRenderer { get; init; } // Fallback
}

public record AddChatItemAction
{
    [JsonPropertyName("item")]
    public AddChatItemActionItem? Item { get; init; }

    [JsonPropertyName("clientId")]
    public string? ClientId { get; init; }
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

    // Add other potential actions (addBanner, markChatItemsByAuthorAsDeletedAction etc.)
    [JsonPropertyName("markChatItemsByAuthorAsDeletedAction")]
    public RemoveChatItemByAuthorAction? MarkChatItemsByAuthorAsDeletedAction { get; init; } // Similar structure to remove by author

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
    public AuthorPhoto? AuthorPhoto { get; init; }

    [JsonPropertyName("authorBadges")]
    public List<AuthorBadgeContainer>? AuthorBadges { get; init; }
}

// --- Navigation Endpoint ---
public record NavigationEndpoint
{
    [JsonPropertyName("clickTrackingParams")]
    public string? ClickTrackingParams { get; init; }

    [JsonPropertyName("commandMetadata")]
    public CommandMetadata? CommandMetadata { get; init; }

    [JsonPropertyName("urlEndpoint")]
    public UrlEndpoint? UrlEndpoint { get; init; }
    // Add other endpoint types like browseEndpoint, watchEndpoint if needed
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
