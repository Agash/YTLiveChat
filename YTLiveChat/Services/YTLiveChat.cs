using System.Text.Json;

using Microsoft.Extensions.Options;

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
    private static readonly SemaphoreSlim s_debugLogLock = new(1, 1); // Semaphore for async file access
    private readonly bool _isDebugLoggingEnabled;
    private readonly string _debugLogFilePath;
    private static readonly JsonSerializerOptions s_debugJsonOptions = new()
    {
        WriteIndented = false,
    }; // Compact JSON for logging

    // --- End Debug Logging Fields ---

    public YTLiveChat(IOptions<YTLiveChatOptions> options, YTHttpClientFactory httpClientFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClientFactory =
            httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

        // --- Initialize Debug Logging ---
        _isDebugLoggingEnabled = _options.Value.DebugLogReceivedJsonItems;
        _debugLogFilePath =
            _options.Value.DebugLogFilePath
            ?? Path.Combine(AppContext.BaseDirectory, "ytlivechat_debug_items.jsonl");

        // Enable logging in DEBUG builds regardless of config setting for easier development
#if DEBUG
        _isDebugLoggingEnabled = true;
        Console.WriteLine($"[YTLiveChat DEBUG] Debug build detected, enabling JSON logging.");
#endif

        if (_isDebugLoggingEnabled)
        {
            Console.WriteLine(
                $"[YTLiveChat DEBUG] JSON Item logging enabled. Output Path: {_debugLogFilePath}"
            );
            EnsureDebugLogDirectoryExists();
        }
        // --- End Initialize Debug Logging ---
    }

    private void EnsureDebugLogDirectoryExists()
    {
        try
        {
            string? directory = Path.GetDirectoryName(_debugLogFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Console.WriteLine(
                    $"[YTLiveChat DEBUG] Created directory for debug log: {directory}"
                );
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[YTLiveChat DEBUG] Error ensuring debug log directory exists ('{_debugLogFilePath}'): {ex.Message}. Debug logging might fail."
            );
            // Optionally disable logging if directory setup fails:
            // _isDebugLoggingEnabled = false;
        }
    }

    public void Start(
        string? handle = null,
        string? channelId = null,
        string? liveId = null,
        bool overwrite = false
    )
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            Console.WriteLine("[YTLiveChat] Start called but already running.");
            if (!overwrite)
            {
                return; // Don't restart if already running unless overwrite is true
            }

            Console.WriteLine("[YTLiveChat] Overwrite requested, stopping previous instance...");
            Stop(); // Stop existing task before restarting
        }

        _cancellationTokenSource = new CancellationTokenSource();
        CancellationToken token = _cancellationTokenSource.Token; // Capture token

        // Run the main loop in a background thread
        _ = Task.Run(
            async () => await StartAsync(handle, channelId, liveId, overwrite, token),
            token
        );
    }

    private async Task StartAsync(
        string? handle,
        string? channelId,
        string? liveId,
        bool overwrite,
        CancellationToken cancellationToken
    )
    {
        Console.WriteLine("[YTLiveChat] Starting chat listener task...");
        try
        {
            _fetchOptions = await GetOptionsAsync(handle, channelId, liveId, overwrite); // Assign directly

            if (string.IsNullOrEmpty(_fetchOptions?.LiveId))
            {
                OnErrorOccurred(
                    new ErrorOccurredEventArgs(
                        new ArgumentException(
                            "Failed to retrieve valid initial FetchOptions (LiveId is missing)."
                        )
                    )
                );
                OnChatStopped(new() { Reason = "Failed to get initial options" });
                return;
            }

            OnInitialPageLoaded(new() { LiveId = _fetchOptions.LiveId });
            Console.WriteLine(
                $"[YTLiveChat] Initial page loaded for Live ID: {_fetchOptions.LiveId}. Starting polling..."
            );

            using YTHttpClient httpClient = _httpClientFactory.Create();
            using PeriodicTimer timer = new(
                TimeSpan.FromMilliseconds(_options.Value.RequestFrequency)
            );

            // Initial delay before first fetch? Optional.
            // await Task.Delay(100, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_fetchOptions == null || string.IsNullOrEmpty(_fetchOptions.Continuation))
                {
                    Console.Error.WriteLine(
                        "[YTLiveChat] Error: Fetch options or continuation token missing. Stopping poll."
                    );
                    OnErrorOccurred(
                        new ErrorOccurredEventArgs(
                            new InvalidOperationException("Continuation token lost.")
                        )
                    );
                    OnChatStopped(new() { Reason = "Continuation token missing" });
                    break; // Exit loop if continuation is lost
                }

                bool tick = await timer.WaitForNextTickAsync(cancellationToken);
                if (!tick)
                    break; // Timer stopped or task cancelled

                try
                {
                    // Use the instance field _fetchOptions directly
                    (LiveChatResponse? response, string? rawJson) =
                        await httpClient.GetLiveChatAsync(_fetchOptions);

                    // --- Log Raw JSON Item Actions if Enabled ---
                    if (
                        _isDebugLoggingEnabled
                        && response?.ContinuationContents?.LiveChatContinuation?.Actions != null
                    )
                    {
                        // Log only the action part containing items for clarity
                        await LogRawJsonActionsAsync(
                            response.ContinuationContents.LiveChatContinuation.Actions
                        );
                    }
                    // --- End Log Raw JSON ---

                    if (response != null)
                    {
                        (List<Contracts.Models.ChatItem> items, string? continuation) =
                            Parser.ParseLiveChatResponse(response);

                        foreach (Contracts.Models.ChatItem item in items)
                        {
                            OnChatReceived(new() { ChatItem = item });
                        }

                        if (!string.IsNullOrEmpty(continuation))
                        {
                            // Update the continuation token in the instance field
                            _fetchOptions = _fetchOptions with
                            {
                                Continuation = continuation,
                            };
                        }
                        else
                        {
                            // Handle case where no continuation is returned (stream ended?)
                            Console.WriteLine(
                                "[YTLiveChat] No continuation token received in response. Stopping poll."
                            );
                            OnChatStopped(new() { Reason = "Stream ended or continuation lost" });
                            break;
                        }
                    }
                    else
                    {
                        // Error fetching/deserializing, potentially logged in YTHttpClient
                        OnErrorOccurred(
                            new ErrorOccurredEventArgs(
                                new Exception("Failed to fetch or deserialize live chat data.")
                            )
                        );
                        Console.Error.WriteLine(
                            $"[YTLiveChat] Fetch/deserialize error occurred. See previous logs. Retrying after delay..."
                        );
                        await Task.Delay(5000, cancellationToken); // Delay before retrying
                    }
                }
                catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    OnErrorOccurred(new ErrorOccurredEventArgs(httpEx));
                    Console.Error.WriteLine("[YTLiveChat] Received Forbidden (403). Stream might be region locked or require login. Stopping.");
                    OnChatStopped(new() { Reason = "Received Forbidden (403)" });
                    break; // Exit loop
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("[YTLiveChat] Polling loop cancelled.");
                    break; // Exit loop cleanly
                }
                catch (Exception ex) // General catch-all
                {
                    OnErrorOccurred(new ErrorOccurredEventArgs(ex));
                    Console.Error.WriteLine($"[YTLiveChat] Error during poll cycle: {ex.GetType().Name} - {ex.Message}");
                    Console.Error.WriteLine("[YTLiveChat] Retrying after delay...");
                    await Task.Delay(5000, cancellationToken); // Delay before retrying general errors
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("[YTLiveChat] StartAsync task initialization cancelled.");
            // Don't raise ChatStopped here, it will be handled in the finally block
        }
        catch (Exception ex) // Catch errors during initial GetOptionsAsync
        {
            OnErrorOccurred(new ErrorOccurredEventArgs(ex));
            Console.Error.WriteLine(
                $"[YTLiveChat] Critical error during initialization: {ex.Message}"
            );
            OnChatStopped(new() { Reason = $"Initialization error: {ex.Message}" });
        }
        finally
        {
            // Ensure ChatStopped is raised if the task ends due to cancellation or loop exit
            if (cancellationToken.IsCancellationRequested)
            {
                OnChatStopped(new() { Reason = "Operation Cancelled" });
            }
            else if (ChatStopped == null || ChatStopped.GetInvocationList().Length == 0) // Check if already stopped for other reasons
            {
                // If loop finished naturally without cancellation, it might mean stream ended.
                // The loop should break and raise ChatStopped with a reason if continuation is lost.
                // This path might indicate an unexpected loop termination.
                OnChatStopped(new() { Reason = "Polling loop finished unexpectedly" });
            }

            Console.WriteLine("[YTLiveChat] Chat listener task finished.");
        }
    }

    // Updated Debug Logging to log only relevant AddChatItem actions
    private async Task LogRawJsonActionsAsync(List<Models.Response.Action> actions)
    {
        if (!_isDebugLoggingEnabled || actions == null)
            return;

        List<AddChatItemActionItem?> itemsToLog = [.. actions
            .Where(a => a.AddChatItemAction?.Item != null)
            .Select(a => a.AddChatItemAction!.Item)];

        if (itemsToLog.Count == 0)
            return;

        await s_debugLogLock.WaitAsync(); // Lock file access
        try
        {
            using StreamWriter writer = new(
                _debugLogFilePath,
                append: true,
                System.Text.Encoding.UTF8
            );
            foreach (AddChatItemActionItem? item in itemsToLog)
            {
                string jsonLine = JsonSerializer.Serialize(item, s_debugJsonOptions);
                await writer.WriteLineAsync(jsonLine);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[YTLiveChat DEBUG] Error writing raw JSON actions to log file '{_debugLogFilePath}': {ex.Message}"
            );
        }
        finally
        {
            s_debugLogLock.Release();
        }
    }

    public void Stop()
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            Console.WriteLine("[YTLiveChat] Stop requested.");
            _cancellationTokenSource.Cancel();
            // ChatStopped event will be raised by the StartAsync task's finally block upon cancellation.
        }
        else
        {
            Console.WriteLine("[YTLiveChat] Stop called but already stopped or stopping.");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Stop(); // Ensure cancellation is requested
            _cancellationTokenSource?.Dispose();
            s_debugLogLock.Dispose(); // Dispose semaphore
        }
    }

    protected virtual void OnInitialPageLoaded(InitialPageLoadedEventArgs e) =>
        InitialPageLoaded?.Invoke(this, e);

    protected virtual void OnChatStopped(ChatStoppedEventArgs e) => ChatStopped?.Invoke(this, e);

    protected virtual void OnChatReceived(ChatReceivedEventArgs e) => ChatReceived?.Invoke(this, e);

    protected virtual void OnErrorOccurred(ErrorOccurredEventArgs e) =>
        ErrorOccurred?.Invoke(this, e);

    private async Task<FetchOptions?> GetOptionsAsync(
        string? handle = null,
        string? channelId = null,
        string? liveId = null,
        bool overwrite = false
    )
    {
        // Use instance field _fetchOptions
        if (_fetchOptions == null || overwrite)
        {
            Console.WriteLine("[YTLiveChat] Fetching initial options...");
            try
            {
                using YTHttpClient httpClient = _httpClientFactory.Create();
                string pageHtml = await httpClient.GetOptionsAsync(handle, channelId, liveId);
                _fetchOptions = Parser.GetOptionsFromLivePage(pageHtml); // This might throw if parsing fails
                Console.WriteLine(
                    $"[YTLiveChat] Successfully parsed initial options. Live ID: {_fetchOptions?.LiveId}"
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[YTLiveChat] Failed to get/parse initial options: {ex.Message}"
                );
                _fetchOptions = null; // Ensure it's null on failure
                OnErrorOccurred(
                    new ErrorOccurredEventArgs(
                        new InvalidOperationException(
                            $"Failed to initialize from YouTube page: {ex.Message}",
                            ex
                        )
                    )
                );
            }
        }
        else
        {
            Console.WriteLine("[YTLiveChat] Using cached initial options.");
        }

        // Return the instance field (which might be null if initialization failed)
        return _fetchOptions;
    }
}
