using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Services;
using YTLiveChat.Helpers;
using YTLiveChat.Models;
using YTLiveChat.Models.Response;

namespace YTLiveChat.Services;

internal class YTLiveChat : IYTLiveChat
{
    public event EventHandler<InitialPageLoadedEventArgs>? InitialPageLoaded;
    public event EventHandler<ChatStoppedEventArgs>? ChatStopped;
    public event EventHandler<ChatReceivedEventArgs>? ChatReceived;
    public event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;

    private FetchOptions? _fetchOptions;
    private CancellationTokenSource? _cancellationTokenSource;

    private readonly YTHttpClientFactory _httpClientFactory;
    private readonly IOptions<YTLiveChatOptions> _options;

    // --- Debug Logging Fields ---
    private static readonly SemaphoreSlim s_debugLogLock = new(1, 1); // Semaphore for file access
    private readonly bool _isDebugLoggingEnabled;
    private readonly string _debugLogFilePath;
    // --- End Debug Logging Fields ---

    public YTLiveChat(IOptions<YTLiveChatOptions> options, YTHttpClientFactory httpClientFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

        // --- Initialize Debug Logging ---
        _isDebugLoggingEnabled = _options.Value.DebugLogReceivedJsonItems;
        _debugLogFilePath = _options.Value.DebugLogFilePath ?? Path.Combine(AppContext.BaseDirectory, "ytlivechat_debug_items.jsonl");

#if DEBUG
        _isDebugLoggingEnabled = true;
#endif

        if (_isDebugLoggingEnabled)
        {
            Console.WriteLine($"[YTLiveChat DEBUG] JSON Item logging enabled. Output Path: {_debugLogFilePath}");
            // Ensure directory exists
            try
            {
                string? directory = Path.GetDirectoryName(_debugLogFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"[YTLiveChat DEBUG] Created directory for debug log: {directory}");
                }
                // Optional: Clear file on start? Or append? Append seems safer.
                // File.WriteAllText(_debugLogFilePath, ""); // Clears file on start
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[YTLiveChat DEBUG] Error ensuring debug log directory exists: {ex.Message}");
                _isDebugLoggingEnabled = false; // Disable logging if directory setup fails
            }
        }
        // --- End Initialize Debug Logging ---
    }

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
            if (string.IsNullOrEmpty(options.LiveId)) // Simplified null check
            {
                OnErrorOccurred(new ErrorOccurredEventArgs(new ArgumentException("FetchOptions invalid or LiveId empty")));
                OnChatStopped(new() { Reason = "Failed to get initial options" });
                return;
            }

            _fetchOptions = options;
            OnInitialPageLoaded(new() { LiveId = options.LiveId });

            using YTHttpClient httpClient = _httpClientFactory.Create();
            using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(_options.Value.RequestFrequency));

            while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    (GetLiveChatResponse? response, string? rawJson) = await httpClient.GetLiveChatAsync(options);

                    // --- Log Raw JSON if Enabled ---
                    if (_isDebugLoggingEnabled && !string.IsNullOrEmpty(rawJson))
                    {
                        await LogRawJsonResponseAsync(rawJson); // Log the entire response
                    }
                    // --- End Log Raw JSON ---

                    if (response != null)
                    {
                        (List<Contracts.Models.ChatItem> items, string continuation) = Parser.ParseGetLiveChatResponse(response);
                        foreach (Contracts.Models.ChatItem item in items)
                        {
                            OnChatReceived(new() { ChatItem = item });
                        }
                        options.Continuation = continuation;
                    }
                    else if (!string.IsNullOrEmpty(rawJson))
                    {
                        // Deserialization failed but we logged the raw JSON already if enabled
                        OnErrorOccurred(new ErrorOccurredEventArgs(new JsonException("Failed to deserialize YouTube response.")));
                        Console.Error.WriteLine($"[YTLiveChat] Deserialization failed. Raw JSON was logged if enabled.");
                    }
                    else
                    {
                        // Network or other error occurred in YTHttpClient, already logged there.
                        OnErrorOccurred(new ErrorOccurredEventArgs(new Exception("Failed to fetch live chat data.")));
                        await Task.Delay(5000, cancellationToken); // Simple delay on fetch failure
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("[YTLiveChat] Polling task cancelled.");
                    break;
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(new ErrorOccurredEventArgs(ex));
                    Console.Error.WriteLine($"[YTLiveChat] Error during poll cycle: {ex.Message}");
                    await Task.Delay(5000, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("[YTLiveChat] StartAsync task cancelled.");
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new ErrorOccurredEventArgs(ex));
            Console.Error.WriteLine($"[YTLiveChat] Critical error in StartAsync: {ex.Message}");
            OnChatStopped(new() { Reason = $"Critical error: {ex.Message}" });
        }
        finally
        {
            if (cancellationToken.IsCancellationRequested)
            {
                OnChatStopped(new() { Reason = "Operation Cancelled" });
            }
        }
    }

    private async Task LogRawJsonResponseAsync(string rawJsonResponse)
    {
        if (!_isDebugLoggingEnabled || string.IsNullOrEmpty(rawJsonResponse)) return;

        await s_debugLogLock.WaitAsync(); // Lock file access
        try
        {
            // Append the entire JSON response string as a new line
            using StreamWriter writer = new(_debugLogFilePath, append: true, System.Text.Encoding.UTF8);
            await writer.WriteLineAsync(rawJsonResponse);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[YTLiveChat DEBUG] Error writing raw JSON response to log file '{_debugLogFilePath}': {ex.Message}");
            // Consider disabling logging temporarily if file errors persist?
            // _isDebugLoggingEnabled = false;
        }
        finally
        {
            s_debugLogLock.Release();
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
