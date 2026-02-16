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
    event EventHandler<LivestreamStartedEventArgs>? LivestreamStarted;

    /// <summary>
    /// Fires when the current livestream ends for the monitored target.
    /// </summary>
    event EventHandler<LivestreamEndedEventArgs>? LivestreamEnded;

    /// <summary>
    /// Fires when a raw action payload was received (including unsupported action types).
    /// </summary>
    event EventHandler<RawActionReceivedEventArgs>? RawActionReceived;

    /// <summary>
    /// Fires on any error from backend or within service
    /// </summary>
    event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;

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
