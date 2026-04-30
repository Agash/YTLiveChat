namespace YTLiveChat.Contracts.Models;

/// <summary>
/// Discriminates the type of a banner produced by <c>addBannerToLiveChatCommand</c>.
/// </summary>
public enum BannerType
{
    /// <summary>Unrecognised banner type.</summary>
    Unknown = 0,

    /// <summary>A pinned chat message (<c>LIVE_CHAT_BANNER_TYPE_PINNED_MESSAGE</c>).</summary>
    PinnedMessage = 1,

    /// <summary>
    /// A cross-channel redirect, shown when a stream ends and viewers are directed to another live
    /// (<c>LIVE_CHAT_BANNER_TYPE_CROSS_CHANNEL_REDIRECT</c>).
    /// </summary>
    CrossChannelRedirect = 2,

    /// <summary>
    /// An AI-generated chat summary banner (<c>LIVE_CHAT_BANNER_TYPE_CHAT_SUMMARY</c>).
    /// Shown periodically during long streams as an experimental YouTube feature.
    /// </summary>
    ChatSummary = 3,

    /// <summary>
    /// A pinned Q&amp;A question banner (<c>liveChatCallForQuestionsRenderer</c>).
    /// Shown when a streamer highlights a viewer's question for the audience.
    /// </summary>
    CallForQuestions = 4,
}

/// <summary>
/// Base class for all banner types produced by <c>addBannerToLiveChatCommand</c>.
/// Pattern-match on <see cref="PinnedMessageBannerItem"/> or
/// <see cref="CrossChannelRedirectBannerItem"/> to access type-specific properties.
/// <code>
/// if (e.Banner is CrossChannelRedirectBannerItem redirect)
/// {
///     // redirect.RedirectChannelHandle, redirect.RedirectVideoId
/// }
/// else if (e.Banner is PinnedMessageBannerItem pinned)
/// {
///     // pinned.Author, pinned.Message, pinned.PinnedBy, ...
/// }
/// </code>
/// </summary>
public abstract class BannerItem
{
    /// <summary>
    /// The server-assigned action ID used to match a subsequent
    /// <c>removeBannerForLiveChatCommand</c> (see <see cref="Services.BannerRemovedEventArgs.TargetActionId"/>).
    /// </summary>
    public required string ActionId { get; set; }

    /// <summary>
    /// The kind of banner. Use this to decide which concrete subclass to cast to,
    /// or pattern-match directly on the subclass type.
    /// </summary>
    public BannerType BannerType { get; set; }
}

/// <summary>
/// A pinned chat message banner (<c>LIVE_CHAT_BANNER_TYPE_PINNED_MESSAGE</c>).
/// Produced when a channel owner or moderator pins a message in live chat.
/// </summary>
public sealed class PinnedMessageBannerItem : BannerItem
{
    /// <summary>
    /// The concatenated "Pinned by @handle" text from the banner header, or null if absent.
    /// </summary>
    public string? PinnedBy { get; set; }

    /// <summary>
    /// The author of the pinned message.
    /// </summary>
    public required Author Author { get; set; }

    /// <summary>
    /// The content of the pinned message.
    /// </summary>
    public required MessagePart[] Message { get; set; }

    /// <summary>
    /// The original chat item ID of the pinned message.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp of the pinned message.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Whether the author of the pinned message is a verified channel.</summary>
    public bool IsVerified { get; set; }

    /// <summary>Whether the author of the pinned message is a chat moderator.</summary>
    public bool IsModerator { get; set; }

    /// <summary>Whether the author of the pinned message is the channel owner.</summary>
    public bool IsOwner { get; set; }
}

/// <summary>
/// An AI-generated chat summary banner (<c>LIVE_CHAT_BANNER_TYPE_CHAT_SUMMARY</c>).
/// Shown periodically during long streams as an experimental YouTube feature. The summary
/// text is auto-generated from recent chat messages.
/// </summary>
public sealed class ChatSummaryBannerItem : BannerItem
{
    /// <summary>
    /// The server-assigned summary identifier (distinct from <see cref="BannerItem.ActionId"/>).
    /// </summary>
    public string? SummaryId { get; set; }

    /// <summary>
    /// The full <c>chatSummary.runs</c> content as structured message parts, preserving bold,
    /// deemphasized, and plain text runs. Typically contains: a bold title run, a newline,
    /// a deemphasized disclaimer run, a newline, and the body text run.
    /// Concatenate <see cref="TextPart.Text"/> values for a plain-text summary.
    /// </summary>
    public required MessagePart[] Summary { get; set; }
}

/// <summary>
/// A pinned Q&amp;A question banner (<c>liveChatCallForQuestionsRenderer</c>).
/// Shown when a streamer highlights a viewer's question for the chat audience.
/// </summary>
public sealed class CallForQuestionsBannerItem : BannerItem
{
    /// <summary>
    /// The question text as structured message parts (may include emoji).
    /// </summary>
    public required MessagePart[] QuestionMessage { get; set; }

    /// <summary>
    /// The streamer's @handle (e.g. <c>"@Alofokeradioshow"</c>), or null if absent.
    /// </summary>
    public string? CreatorHandle { get; set; }

    /// <summary>
    /// Thumbnail image of the streamer's profile photo.
    /// </summary>
    public ImagePart? CreatorAvatar { get; set; }

    /// <summary>
    /// Feature label text (typically <c>"Q&amp;A"</c>).
    /// </summary>
    public string? FeatureLabel { get; set; }
}

/// <summary>
/// Discriminates the two observed variants of a
/// <see cref="CrossChannelRedirectBannerItem"/>.
/// </summary>
public enum CrossChannelRedirectType
{
    /// <summary>Unrecognised or future variant.</summary>
    Unknown = 0,

    /// <summary>
    /// The current stream ended and the channel owner is redirecting viewers to another
    /// live stream ("Don't miss out! People are going to watch something from @Handle").
    /// The button is "Go now" (<c>watchEndpoint</c>); <see cref="CrossChannelRedirectBannerItem.RedirectVideoId"/>
    /// is always non-null for this variant.
    /// </summary>
    Redirect = 1,

    /// <summary>
    /// Another channel's viewers have joined this stream and are watching together
    /// ("@Handle and their viewers just joined. Say hello!").
    /// The button is "Learn more" (<c>urlEndpoint</c>); <see cref="CrossChannelRedirectBannerItem.RedirectVideoId"/>
    /// is always null for this variant.
    /// </summary>
    Raid = 2,
}

/// <summary>
/// A cross-channel redirect banner (<c>LIVE_CHAT_BANNER_TYPE_CROSS_CHANNEL_REDIRECT</c>).
/// Two distinct variants exist — check <see cref="RedirectType"/> to tell them apart:
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="CrossChannelRedirectType.Redirect"/> — the stream ended and the channel
///       owner is sending viewers away to another live stream ("Don't miss out!…"). The "Go now"
///       button carries a direct <c>watchEndpoint</c>; <see cref="RedirectVideoId"/> is non-null.
///       Pass it to <c>IYTLiveChat.Start(liveId: ...)</c> to follow immediately.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="CrossChannelRedirectType.Raid"/> — another channel's viewers have joined this
///       stream and are watching together ("@Handle and their viewers just joined. Say hello!"). The
///       "Learn more" button links to a support page; <see cref="RedirectVideoId"/> is null.
///       Use <see cref="RedirectChannelHandle"/> with <c>Start(handle: ...)</c> to connect to
///       that channel's current stream separately.
///     </description>
///   </item>
/// </list>
/// </summary>
public sealed class CrossChannelRedirectBannerItem : BannerItem
{
    /// <summary>
    /// Whether the channel owner is redirecting viewers away to another stream (<see cref="CrossChannelRedirectType.Redirect"/>)
    /// or another channel's viewers have joined this stream (<see cref="CrossChannelRedirectType.Raid"/>).
    /// Use this instead of null-checking <see cref="RedirectVideoId"/> to branch on intent.
    /// </summary>
    public CrossChannelRedirectType RedirectType { get; set; }

    /// <summary>
    /// The @handle of the channel involved (e.g. <c>"@TakanashiKiara"</c>).
    /// Extracted from the bold text run in the banner message.
    /// For <see cref="CrossChannelRedirectType.Redirect"/>, pass to
    /// <c>IYTLiveChat.Start(handle: ...)</c> as an alternative to <see cref="RedirectVideoId"/>.
    /// For <see cref="CrossChannelRedirectType.Raid"/>, use it to look up that channel's
    /// current livestream via <c>Start(handle: ...)</c>.
    /// </summary>
    public required string RedirectChannelHandle { get; set; }

    /// <summary>
    /// The video ID of the destination livestream.
    /// Non-null only for <see cref="CrossChannelRedirectType.Redirect"/> ("Go now" button).
    /// Null for <see cref="CrossChannelRedirectType.Raid"/> ("Learn more" button).
    /// Pass to <c>IYTLiveChat.Start(liveId: ...)</c> to connect directly when non-null.
    /// </summary>
    public string? RedirectVideoId { get; set; }

    /// <summary>
    /// Thumbnail image of the other channel's profile photo.
    /// </summary>
    public ImagePart? ChannelPhoto { get; set; }

    /// <summary>
    /// The full banner message as structured parts, e.g.:
    /// <list type="bullet">
    ///   <item><description><see cref="CrossChannelRedirectType.Redirect"/>: "Don't miss out! People are going to watch something from " + "@TakanashiKiara" (bold)</description></item>
    ///   <item><description><see cref="CrossChannelRedirectType.Raid"/>: "@holoen_ceciliaimmergreen" (bold) + " and their viewers just joined. Say hello!"</description></item>
    /// </list>
    /// Concatenate <see cref="TextPart.Text"/> values for a plain-text summary.
    /// </summary>
    public required MessagePart[] BannerMessage { get; set; }
}
