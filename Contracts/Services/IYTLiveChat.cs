using YTLiveChat.Contracts.Models;

namespace YTLiveChat.Contracts.Services
{
    public interface IYTLiveChat
    {
        public event EventHandler<InitialPageLoadedEventArgs>? InitialPageLoaded;
        public event EventHandler<ChatStoppedEventArgs>? ChatStopped;
        public event EventHandler<ChatReceivedEventArgs>? ChatReceived;
        public event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;

        public void Start(string? handle = null, string? channelId = null, string? liveId = null, bool overwrite = false);
        public void Stop();

    }

    public class InitialPageLoadedEventArgs : EventArgs
    {
        public required string LiveId { get; set; }
    }

    public class ChatStoppedEventArgs : EventArgs
    {
        public string? Reason { get; set; }
    }

    public class ChatReceivedEventArgs : EventArgs
    {
        public required ChatItem ChatItem { get; set; }
    }

    public class ErrorOccurredEventArgs(Exception exception) : ErrorEventArgs(exception)
    {
    }
}
