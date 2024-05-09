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
    public event EventHandler<InitialPageLoadedEventArgs>? InitialPageLoaded;

    /// <summary>
    /// Fires after Chat was stopped
    /// </summary>
    public event EventHandler<ChatStoppedEventArgs>? ChatStopped;

    /// <summary>
    /// Fires when a ChatItem was received
    /// </summary>
    public event EventHandler<ChatReceivedEventArgs>? ChatReceived;

    /// <summary>
    /// Fires on any error from backend or within service
    /// </summary>
    public event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;

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
    public void Start(string? handle = null, string? channelId = null, string? liveId = null, bool overwrite = false);

    /// <summary>
    /// Stops the listeners
    /// </summary>
    public void Stop();

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
/// EventArgs for ErrorOccurred event
/// </summary>
/// <param name="exception">Exception that triggered the event</param>
public class ErrorOccurredEventArgs(Exception exception) : ErrorEventArgs(exception)
{
}
