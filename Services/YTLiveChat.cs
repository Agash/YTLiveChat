using Microsoft.Extensions.Options;
using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Services;
using YTLiveChat.Helpers;
using YTLiveChat.Models;

namespace YTLiveChat.Services;

internal class YTLiveChat(IOptions<YTLiveChatOptions> options, YTHttpClientFactory httpClientFactory) : IYTLiveChat
{
    public event EventHandler<InitialPageLoadedEventArgs>? InitialPageLoaded;
    public event EventHandler<ChatStoppedEventArgs>? ChatStopped;
    public event EventHandler<ChatReceivedEventArgs>? ChatReceived;
    public event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;

    private FetchOptions? _fetchOptions;
    private CancellationTokenSource? _cancellationTokenSource;

    private readonly YTHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IOptions<YTLiveChatOptions> _options = options;

    public void Start(string? handle = null, string? channelId = null, string? liveId = null, bool overwrite = false)
    {
        if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(async () => await StartAsync(handle, channelId, liveId, overwrite, _cancellationTokenSource.Token));
        }
    }
    private async Task StartAsync(string? handle = null, string? channelId = null, string? liveId = null, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        try
        {
            FetchOptions options = await GetOptionsAsync(handle, channelId, liveId, overwrite);
            if (options == null || string.IsNullOrEmpty(options.LiveId))
            {
                OnErrorOccurred(new ErrorOccurredEventArgs(new ArgumentException("FetchOptions invalid")));
                OnChatStopped(new() { Reason = "Error occurred" });
                return;
            }

            OnInitialPageLoaded(new() { LiveId = options.LiveId });

            using YTHttpClient httpClient = _httpClientFactory.Create();
            using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(_options.Value.RequestFrequency));
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    Models.Response.GetLiveChatResponse? response = await httpClient.GetLiveChatAsync(options);
                    if (response != null)
                    {
                        (List<Contracts.Models.ChatItem> items, string continuation) = Parser.ParseGetLiveChatResponse(response);
                        foreach (Contracts.Models.ChatItem item in items)
                        {
                            OnChatReceived(new() { ChatItem = item });
                        }

                        options.Continuation = continuation;
                    }
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(new ErrorOccurredEventArgs(ex));
                }
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new ErrorOccurredEventArgs(ex));
            OnChatStopped(new() { Reason = "Error occurred" });
        }
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        OnChatStopped(new() { Reason = "Stop called" });
    }

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    protected virtual void OnInitialPageLoaded(InitialPageLoadedEventArgs e)
    {
        EventHandler<InitialPageLoadedEventArgs>? raiseInitialPageLoaded = InitialPageLoaded;
        raiseInitialPageLoaded?.Invoke(this, e);
    }

    protected virtual void OnChatStopped(ChatStoppedEventArgs e)
    {
        EventHandler<ChatStoppedEventArgs>? raiseChatStopped = ChatStopped;
        raiseChatStopped?.Invoke(this, e);
    }

    protected virtual void OnChatReceived(ChatReceivedEventArgs e)
    {
        EventHandler<ChatReceivedEventArgs>? raiseChatRecieved = ChatReceived;
        raiseChatRecieved?.Invoke(this, e);
    }

    protected virtual void OnErrorOccurred(ErrorOccurredEventArgs e)
    {
        EventHandler<ErrorOccurredEventArgs>? raiseErrorOccurred = ErrorOccurred;
        raiseErrorOccurred?.Invoke(this, e);
    }


    private async Task<FetchOptions> GetOptionsAsync(string? handle = null, string? channelId = null, string? liveId = null, bool overwrite = false)
    {
        if (_fetchOptions == null || overwrite)
        {
            YTHttpClient httpClient = _httpClientFactory.Create();
            string options = await httpClient.GetOptionsAsync(handle, channelId, liveId);

            _fetchOptions = Parser.GetOptionsFromLivePage(options);
        }

        return _fetchOptions;
    }
}
