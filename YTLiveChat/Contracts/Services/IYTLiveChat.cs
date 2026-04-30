using System.Text.Json;

using YTLiveChat.Contracts.Models;

namespace YTLiveChat.Contracts.Services;

/// <summary>
/// Represents the YouTube Live Chat Service
/// </summary>
public interface IYTLiveChat : IDisposable
{
    /// <summary>
    /// Fires after the initial Live page was loaded
    /// </summary>
    event EventHandler<InitialPageLoadedEventArgs>? InitialPageLoaded;

    /// <summary>
    /// Fires after Chat was stopped
    /// </summary>
    event EventHandler<ChatStoppedEventArgs>? ChatStopped;

    /// <summary>
    /// Fires when a ChatItem was received
    /// </summary>
    event EventHandler<ChatReceivedEventArgs>? ChatReceived;

    /// <summary>
    /// Fires when a livestream becomes active for the monitored target.
    /// </summary>
    [Obsolete(
        "BETA/UNSUPPORTED: Continuous livestream monitor mode may change or break at any time and is not covered by semver stability guarantees."
    )]
    event EventHandler<LivestreamStartedEventArgs>? LivestreamStarted;

    /// <summary>
    /// Fires when the current livestream ends for the monitored target.
    /// </summary>
    [Obsolete(
        "BETA/UNSUPPORTED: Continuous livestream monitor mode may change or break at any time and is not covered by semver stability guarantees."
    )]
    event EventHandler<LivestreamEndedEventArgs>? LivestreamEnded;

    /// <summary>
    /// Fires when monitor mode detects a livestream candidate but cannot initialize chat access
    /// (for example members-only/login-required/unplayable restrictions).
    /// </summary>
    [Obsolete(
        "BETA/UNSUPPORTED: Continuous livestream monitor mode may change or break at any time and is not covered by semver stability guarantees."
    )]
    event EventHandler<LivestreamInaccessibleEventArgs>? LivestreamInaccessible;

    /// <summary>
    /// Fires when a raw action payload was received (including unsupported action types).
    /// </summary>
    event EventHandler<RawActionReceivedEventArgs>? RawActionReceived;

    /// <summary>
    /// Fires when a poll opens or its live vote counts change.
    /// Produced by <c>showLiveChatActionPanelAction</c> (initial open) and
    /// <c>updateLiveChatPollAction</c> (periodic vote-count refresh).
    /// <para>
    /// Poll lifecycle — subscribe to events in this order:
    /// <list type="number">
    ///   <item><description><see cref="PollUpdated"/> where <see cref="PollItem.IsNew"/> is <see langword="true"/> — a new poll just opened; choices have 0 votes.</description></item>
    ///   <item><description><see cref="PollUpdated"/> where <see cref="PollItem.IsNew"/> is <see langword="false"/> — live vote-count updates; use <see cref="PollItem.PollId"/> to correlate.</description></item>
    ///   <item><description><see cref="PollClosed"/> — the poll panel was dismissed and the poll is over.</description></item>
    ///   <item><description><see cref="EngagementMessageReceived"/> with <see cref="EngagementMessageType.PollResult"/> — a formatted text summary (e.g. "DIG (70%) … Poll complete: 1.3K votes") appears in the chat feed shortly after <see cref="PollClosed"/>. This is a chat-level visual and is <em>not</em> required to track poll lifecycle.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    event EventHandler<PollUpdatedEventArgs>? PollUpdated;

    /// <summary>
    /// Fires when the poll panel is dismissed (<c>closeLiveChatActionPanelAction</c>).
    /// This is the authoritative signal that a poll has ended.
    /// <see cref="PollClosedEventArgs.PollId"/> matches the <see cref="PollItem.PollId"/> from
    /// the preceding <see cref="PollUpdated"/> events.
    /// <para>
    /// Shortly after this event, <see cref="EngagementMessageReceived"/> fires with
    /// <see cref="EngagementMessageType.PollResult"/> carrying the formatted result text visible in chat.
    /// You do <em>not</em> need to subscribe to that engagement event to know the poll ended —
    /// <see cref="PollClosed"/> alone is sufficient.
    /// </para>
    /// </summary>
    event EventHandler<PollClosedEventArgs>? PollClosed;

    /// <summary>
    /// Fires when a single chat message is removed (<c>removeChatItemAction</c>).
    /// </summary>
    event EventHandler<ChatItemDeletedEventArgs>? ChatItemDeleted;

    /// <summary>
    /// Fires when all messages from a specific author are removed (<c>removeChatItemByAuthorAction</c>
    /// or <c>markChatItemsByAuthorAsDeletedAction</c>).
    /// </summary>
    event EventHandler<ChatItemsDeletedByAuthorEventArgs>? ChatItemsDeletedByAuthor;

    /// <summary>
    /// Fires when a banner is added (<c>addBannerToLiveChatCommand</c>).
    /// The <see cref="BannerAddedEventArgs.Banner"/> property is one of two concrete subclasses —
    /// pattern-match to access type-specific properties:
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="Models.PinnedMessageBannerItem"/> — a pinned chat message; carries <c>Author</c>,
    ///     <c>Message</c>, <c>PinnedBy</c>, <c>Timestamp</c>, and role flags
    ///     (<c>IsOwner</c>, <c>IsModerator</c>, <c>IsVerified</c>).
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="Models.CrossChannelRedirectBannerItem"/> — a cross-channel banner; check
    ///     <c>RedirectType</c> to distinguish an owner redirect (<c>Redirect</c>, <c>RedirectVideoId</c> non-null)
    ///     from viewers raiding into this stream (<c>Raid</c>, <c>RedirectVideoId</c> null).
    ///     Pass <c>RedirectVideoId</c> or <c>RedirectChannelHandle</c> to <c>IYTLiveChat.Start</c> to follow.
    ///   </description></item>
    /// </list>
    /// Use <see cref="BannerRemovedEventArgs.TargetActionId"/> in the subsequent <see cref="BannerRemoved"/>
    /// event to correlate with <see cref="Models.BannerItem.ActionId"/>.
    /// </summary>
    event EventHandler<BannerAddedEventArgs>? BannerAdded;

    /// <summary>
    /// Fires when a banner is removed (<c>removeBannerForLiveChatCommand</c>).
    /// </summary>
    event EventHandler<BannerRemovedEventArgs>? BannerRemoved;

    /// <summary>
    /// Fires when an existing chat item is replaced with new content (<c>replaceChatItemAction</c>).
    /// The replacement is typically an updated version of the same message (e.g. after slow-mode
    /// resolves a pending message). <see cref="ChatItemReplacedEventArgs.Replacement"/> is null
    /// when the replacement item type produces no <see cref="ChatItem"/> (e.g. placeholder).
    /// </summary>
    event EventHandler<ChatItemReplacedEventArgs>? ChatItemReplaced;

    /// <summary>
    /// Fires when a viewer engagement system message is received
    /// (<c>liveChatViewerEngagementMessageRenderer</c>). These are YouTube-generated notices
    /// that appear in the chat feed but are not user messages. Inspect
    /// <see cref="EngagementItem.MessageType"/> to distinguish variants:
    /// <list type="bullet">
    ///   <item><description><see cref="EngagementMessageType.CommunityGuidelines"/> — "Welcome to live chat! Remember to guard your privacy…" shown at stream start.</description></item>
    ///   <item><description><see cref="EngagementMessageType.SubscribersOnly"/> — subscribers-only mode notice, shown when the channel restricts chat participation.</description></item>
    ///   <item><description><see cref="EngagementMessageType.PollResult"/> — formatted poll result text (e.g. "Option A (70%)\nOption B (30%)\nPoll complete: 1.3K votes") that appears in chat after a poll closes. This is the chat-level visual; subscribe to <see cref="PollClosed"/> to know when a poll ended.</description></item>
    ///   <item><description><see cref="EngagementMessageType.Unknown"/> — unrecognized engagement message type; <see cref="EngagementItem.Message"/> still contains the concatenated text.</description></item>
    /// </list>
    /// </summary>
    event EventHandler<EngagementMessageReceivedEventArgs>? EngagementMessageReceived;

    /// <summary>
    /// Fires when a viewer sends a virtual gift using YouTube Jewels
    /// (<c>giftMessageViewModel</c> in <c>addChatItemAction</c>).
    /// </summary>
    event EventHandler<GiftReceivedEventArgs>? GiftReceived;

    /// <summary>
    /// Fires when the live-chat ticker bar shows or refreshes a Super Chat creator goal chip
    /// (<c>showCreatorGoalTickerChipCommand</c>).
    /// Multiple events for the same goal share <see cref="CreatorGoalItem.EntityKey"/>.
    /// </summary>
    event EventHandler<CreatorGoalReceivedEventArgs>? CreatorGoalReceived;

    /// <summary>
    /// Fires on any error from backend or within service
    /// </summary>
    event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;

    /// <summary>
    /// Fetches the list of streams (live, upcoming, and recent past) from a channel's streams page.
    /// Returns title, thumbnail, live status, viewer/view count, scheduled time, published time, and duration for each entry.
    /// </summary>
    /// <param name="handle">Channel @handle (e.g. <c>"@IRyS"</c>).</param>
    /// <param name="channelId">Channel ID (e.g. <c>"UC8rcEBzJSleTkf_-agPM20g"</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered list of stream entries as shown on the channel's streams tab.</returns>
    /// <exception cref="ArgumentException">Thrown when neither handle nor channelId is provided.</exception>
    Task<IReadOnlyList<StreamInfo>> GetStreamsAsync(
        string? handle = null,
        string? channelId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Starts the Listeners for the LiveChat and fires InitialPageLoaded when successful. Either <paramref name="handle"/>, <paramref name="channelId"/> or <paramref name="liveId"/> must be given.
    /// </summary>
    /// <remarks>
    /// This method initially loads the stream page from whatever param was given. If called again, it'll simply register the listeners again, but not load another live stream. If another live stream should be loaded, <paramref name="overwrite"/> should be set to true.
    /// </remarks>
    /// <param name="handle">The handle of the channel (eg. "@Original151")</param>
    /// <param name="channelId">The channelId of the channel (eg. "UCtykdsdm9cBfh5JM8xscA0Q")</param>
    /// <param name="liveId">The video ID of the live video (eg. "WZafWA1NVrU")</param>
    /// <param name="overwrite"></param>
    void Start(
        string? handle = null,
        string? channelId = null,
        string? liveId = null,
        bool overwrite = false
    );

    /// <summary>
    /// Stops the listeners
    /// </summary>
    void Stop();

    /// <summary>
    /// Starts chat monitoring and asynchronously yields parsed chat items until stopped, stream ends, or cancellation is requested.
    /// This helper owns the listener lifecycle and calls <see cref="Start"/> and <see cref="Stop"/> internally.
    /// </summary>
    IAsyncEnumerable<ChatItem> StreamChatItemsAsync(
        string? handle = null,
        string? channelId = null,
        string? liveId = null,
        bool overwrite = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Starts chat monitoring and asynchronously yields raw action payloads (including unsupported action types)
    /// until stopped, stream ends, or cancellation is requested.
    /// This helper owns the listener lifecycle and calls <see cref="Start"/> and <see cref="Stop"/> internally.
    /// </summary>
    IAsyncEnumerable<RawActionReceivedEventArgs> StreamRawActionsAsync(
        string? handle = null,
        string? channelId = null,
        string? liveId = null,
        bool overwrite = false,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// EventArgs for InitialPageLoaded event
/// </summary>
public class InitialPageLoadedEventArgs : EventArgs
{
    /// <summary>
    /// Video ID selected or found
    /// </summary>
    public required string LiveId { get; set; }
}

/// <summary>
/// EventArgs for ChatStopped event
/// </summary>
public class ChatStoppedEventArgs : EventArgs
{
    /// <summary>
    /// Reason why the stop occured
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// EventArgs for ChatReceived event
/// </summary>
public class ChatReceivedEventArgs : EventArgs
{
    /// <summary>
    /// ChatItem that was received
    /// </summary>
    public required ChatItem ChatItem { get; set; }
}

/// <summary>
/// EventArgs for LivestreamStarted event.
/// </summary>
public class LivestreamStartedEventArgs : EventArgs
{
    /// <summary>
    /// Video ID selected or found for the active livestream.
    /// </summary>
    public required string LiveId { get; set; }
}

/// <summary>
/// EventArgs for LivestreamEnded event.
/// </summary>
public class LivestreamEndedEventArgs : EventArgs
{
    /// <summary>
    /// Video ID of the livestream that ended.
    /// </summary>
    public required string LiveId { get; set; }

    /// <summary>
    /// Reason why the livestream was considered ended.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// EventArgs for LivestreamInaccessible event.
/// </summary>
public class LivestreamInaccessibleEventArgs : EventArgs
{
    /// <summary>
    /// Video ID of the detected livestream candidate.
    /// </summary>
    public required string LiveId { get; set; }

    /// <summary>
    /// Best-effort reason why chat access could not be initialized.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// EventArgs for RawActionReceived event.
/// </summary>
public class RawActionReceivedEventArgs : EventArgs
{
    /// <summary>
    /// Raw action JSON payload.
    /// </summary>
    public required JsonElement RawAction { get; set; }

    /// <summary>
    /// Parsed ChatItem mapped from this action, if recognized.
    /// Null for unsupported action/renderer types.
    /// </summary>
    public ChatItem? ParsedChatItem { get; set; }
}

/// <summary>
/// EventArgs for ErrorOccurred event
/// </summary>
/// <param name="exception">Exception that triggered the event</param>
public class ErrorOccurredEventArgs(Exception exception) : ErrorEventArgs(exception) { }

/// <summary>
/// EventArgs for PollUpdated event.
/// </summary>
public class PollUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// The current poll state (question, choices, vote percentages).
    /// </summary>
    public required PollItem Poll { get; set; }
}

/// <summary>
/// EventArgs for ChatItemDeleted event.
/// </summary>
public class ChatItemDeletedEventArgs : EventArgs
{
    /// <summary>
    /// The ID of the chat item that was removed.
    /// </summary>
    public required string TargetId { get; set; }
}

/// <summary>
/// EventArgs for ChatItemsDeletedByAuthor event.
/// </summary>
public class ChatItemsDeletedByAuthorEventArgs : EventArgs
{
    /// <summary>
    /// The external channel ID of the author whose messages were removed.
    /// </summary>
    public required string ChannelId { get; set; }
}

/// <summary>
/// EventArgs for PollClosed event.
/// </summary>
public class PollClosedEventArgs : EventArgs
{
    /// <summary>
    /// The ID of the poll that was closed (matches <see cref="PollItem.PollId"/> from the preceding <c>PollUpdated</c> event).
    /// </summary>
    public required string PollId { get; set; }
}

/// <summary>
/// EventArgs for BannerAdded event.
/// </summary>
public class BannerAddedEventArgs : EventArgs
{
    /// <summary>
    /// The pinned-message banner that was added.
    /// </summary>
    public required BannerItem Banner { get; set; }
}

/// <summary>
/// EventArgs for BannerRemoved event.
/// </summary>
public class BannerRemovedEventArgs : EventArgs
{
    /// <summary>
    /// The action ID of the banner that was removed (matches <see cref="BannerItem.ActionId"/>).
    /// </summary>
    public required string TargetActionId { get; set; }
}

/// <summary>
/// EventArgs for ChatItemReplaced event.
/// </summary>
public class ChatItemReplacedEventArgs : EventArgs
{
    /// <summary>
    /// The ID of the chat item that was replaced.
    /// </summary>
    public required string TargetItemId { get; set; }

    /// <summary>
    /// The replacement chat item, or null when the replacement renderer type produces no
    /// <see cref="ChatItem"/> (e.g. a placeholder renderer).
    /// </summary>
    public ChatItem? Replacement { get; set; }
}

/// <summary>
/// EventArgs for EngagementMessageReceived event.
/// </summary>
public class EngagementMessageReceivedEventArgs : EventArgs
{
    /// <summary>
    /// The viewer engagement message that was received.
    /// </summary>
    public required EngagementItem Engagement { get; set; }
}

/// <summary>
/// EventArgs for GiftReceived event.
/// </summary>
public class GiftReceivedEventArgs : EventArgs
{
    /// <summary>
    /// The virtual gift that was sent.
    /// </summary>
    public required GiftItem Gift { get; set; }
}

/// <summary>
/// EventArgs for CreatorGoalReceived event.
/// </summary>
public class CreatorGoalReceivedEventArgs : EventArgs
{
    /// <summary>
    /// The creator goal ticker chip that was received.
    /// </summary>
    public required CreatorGoalItem CreatorGoal { get; set; }
}
