using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Services;
using YTLiveChat.Helpers;
using YTLiveChat.Models;
using YTLiveChat.Models.Response;

namespace YTLiveChat.Services;

/// <summary>
/// Service implementation for fetching and processing YouTube Live Chat messages.
/// Uses YouTube's internal "InnerTube" API.
/// </summary>
public class YTLiveChat : IYTLiveChat // Changed to public for direct instantiation
{
    // --- Events ---
    /// <inheritdoc />
    public event EventHandler<InitialPageLoadedEventArgs>? InitialPageLoaded;

    /// <inheritdoc />
    public event EventHandler<ChatStoppedEventArgs>? ChatStopped;

    /// <inheritdoc />
    public event EventHandler<ChatReceivedEventArgs>? ChatReceived;

    /// <inheritdoc />
    public event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;

    private static readonly Random s_random = new();
    private readonly YTLiveChatOptions _options;
    private readonly YTHttpClient _ytHttpClient;
    private readonly ILogger<YTLiveChat> _logger;

    private FetchOptions? _fetchOptionsInternal;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _pollingTask; // Keep track of the polling task

    private const int MaxRetryAttempts = 5;
    private const double BaseRetryDelaySeconds = 1.0;
    private const double MaxRetryDelaySeconds = 30.0;

    private static readonly SemaphoreSlim s_debugLogLock = new(1, 1);
    private readonly bool _isDebugLoggingEnabled;
    private readonly string _debugLogFilePath;
    private static readonly JsonSerializerOptions s_debugJsonOptions = new()
    {
        WriteIndented = false,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="YTLiveChat"/> class.
    /// </summary>
    /// <param name="options">The configuration options for the chat service.</param>
    /// <param name="ytHttpClient">The HTTP client instance configured for YouTube interaction.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if options or ytHttpClient is null.</exception>
    public YTLiveChat(
        YTLiveChatOptions options,
        YTHttpClient ytHttpClient,
        ILogger<YTLiveChat>? logger // Logger is optional
    )
    {
        // Classic null checks for netstandard2.1 compatibility
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }
        if (ytHttpClient is null)
        {
            throw new ArgumentNullException(nameof(ytHttpClient));
        }

        _options = options;
        _ytHttpClient = ytHttpClient;
        _logger = logger ?? NullLogger<YTLiveChat>.Instance; // Use NullLogger if none provided

        // Configure debug logging based on options
        _isDebugLoggingEnabled = _options.DebugLogReceivedJsonItems;
        _debugLogFilePath =
            _options.DebugLogFilePath
            ?? Path.Combine(AppContext.BaseDirectory, "ytlivechat_debug_items.jsonl");

        // Optionally force debug logging in DEBUG builds, regardless of options
#if DEBUG
        // Uncomment the next line to force debug logging ON in DEBUG builds
        // _isDebugLoggingEnabled = true;
        _logger.LogDebug(
            "DEBUG build detected. JSON item logging state determined by YTLiveChatOptions: {IsEnabled}",
            _isDebugLoggingEnabled
        );
#endif

        if (_isDebugLoggingEnabled)
        {
            _logger.LogInformation(
                "JSON Item logging enabled. Output Path: {FilePath}",
                _debugLogFilePath
            );
            EnsureDebugLogDirectoryExists();
        }
    }

    private void EnsureDebugLogDirectoryExists()
    {
        try
        {
            string? directory = Path.GetDirectoryName(_debugLogFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation(
                    "Created directory for debug log: {DirectoryPath}",
                    directory
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error ensuring debug log directory exists ('{FilePath}'). Debug logging might fail.",
                _debugLogFilePath
            );
        }
    }

    /// <inheritdoc />
    public void Start(
        string? handle = null,
        string? channelId = null,
        string? liveId = null,
        bool overwrite = false
    )
    {
        // Check if already running
        if (_pollingTask != null && !_pollingTask.IsCompleted)
        {
            _logger.LogWarning("Start called but listener is already running.");
            if (!overwrite)
            {
                return; // Don't start a new one if not overwriting
            }
            _logger.LogInformation("Overwrite requested, stopping previous instance...");
            Stop(); // Stop the current task before starting a new one
        }

        _logger.LogInformation(
            "Attempting to start listener (Handle: {Handle}, ChannelId: {ChannelId}, LiveId: {LiveId})...",
            handle ?? "N/A",
            channelId ?? "N/A",
            liveId ?? "N/A"
        );

        _cancellationTokenSource?.Dispose(); // Dispose previous CTS if any
        _cancellationTokenSource = new CancellationTokenSource();
        CancellationToken token = _cancellationTokenSource.Token;

        // Start the polling operation on a background thread
        _pollingTask = Task.Run(
            async () => await StartInternalAsync(handle, channelId, liveId, token),
            token
        );

        // Optionally, handle unobserved exceptions from the task if not awaited elsewhere
        _pollingTask.ContinueWith(
            task =>
            {
                if (task.IsFaulted && task.Exception != null)
                {
                    _logger.LogError(
                        task.Exception.Flatten(),
                        "Unhandled exception in background polling task."
                    );
                    // It's crucial to handle the exception to avoid process termination
                    // Fire error event, but avoid throwing from here.
                    OnErrorOccurred(
                        new ErrorOccurredEventArgs(
                            task.Exception.Flatten().InnerException ?? task.Exception
                        )
                    );
                    // Optionally stop if the background task faults unexpectedly
                    OnChatStopped(new() { Reason = "Background task faulted unexpectedly." });
                }
                else if (task.IsCanceled)
                {
                    _logger.LogInformation("Polling task was cancelled.");
                    OnChatStopped(new() { Reason = "Operation Cancelled" });
                }
                else
                {
                    _logger.LogInformation(
                        "Polling task completed normally (likely stopped via Stop() or stream ended)."
                    );
                    // ChatStopped event should have been fired already by the loop logic or Stop()
                }
            },
            TaskScheduler.Default
        ); // Use default scheduler to avoid blocking UI thread
    }

    /// <summary>
    /// Internal asynchronous method that performs the initialization and polling loop.
    /// </summary>
    private async Task StartInternalAsync(
        string? handle,
        string? channelId,
        string? liveId,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Listener task starting initialization...");
        try
        {
            // --- Initialization ---
            _fetchOptionsInternal = await GetFetchOptionsAsync(
                handle,
                channelId,
                liveId,
                true,
                cancellationToken
            ); // Always overwrite internal options on new start
            if (_fetchOptionsInternal?.LiveId is null)
            {
                // Error logged within GetFetchOptionsAsync
                // Fire events and exit task
                OnErrorOccurred(
                    new ErrorOccurredEventArgs(
                        new InvalidOperationException(
                            "Failed to retrieve valid initial FetchOptions (LiveId is missing)."
                        )
                    )
                );
                OnChatStopped(new() { Reason = "Failed to get initial options" });
                return;
            }

            OnInitialPageLoaded(new() { LiveId = _fetchOptionsInternal.LiveId });
            _logger.LogInformation(
                "Initial page loaded for Live ID: {LiveId}. Starting polling loop...",
                _fetchOptionsInternal.LiveId
            );

            // --- Polling Loop (Conditional Timer/Delay) ---
#if NETSTANDARD2_1 || NETSTANDARD2_0
            await PollingLoopWithTaskDelayAsync(cancellationToken);
#else
            await PollingLoopWithPeriodicTimerAsync(cancellationToken);
#endif
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected cancellation when Stop() is called or token is cancelled externally
            _logger.LogInformation(
                "Listener task cancellation requested during initialization or polling loop setup."
            );
            // Stop() or the ContinueWith handler will fire ChatStopped
        }
        catch (Exception ex)
        {
            // Catch unexpected critical errors during initialization or loop setup
            _logger.LogError(
                ex,
                "Critical error during listener task execution (outside polling loop)."
            );
            OnErrorOccurred(new ErrorOccurredEventArgs(ex));
            OnChatStopped(new() { Reason = $"Critical error: {ex.Message}" });
        }
        finally
        {
            // Ensure state is clean if task ends unexpectedly or normally
            _logger.LogInformation("Listener task finished execution.");
            // ChatStopped should be fired by loop logic, cancellation, or Stop()
        }
    }

#if !NETSTANDARD2_1 && !NETSTANDARD2_0
    /// <summary>
    /// Polling loop implementation using PeriodicTimer (for .NET 6+).
    /// </summary>
    private async Task PollingLoopWithPeriodicTimerAsync(CancellationToken cancellationToken)
    {
        int currentRetryAttempt = 0;
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(_options.RequestFrequency));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (_fetchOptionsInternal?.Continuation is null)
                {
                    _logger.LogError(
                        "Error: Fetch options or continuation token missing. Stopping poll."
                    );
                    OnErrorOccurred(
                        new ErrorOccurredEventArgs(
                            new InvalidOperationException("Continuation token lost.")
                        )
                    );
                    OnChatStopped(new() { Reason = "Continuation token missing" });
                    break;
                }

                try
                {
                    // --- Fetch and Process ---
                    (LiveChatResponse? response, string? rawJson) =
                        await _ytHttpClient.GetLiveChatAsync(
                            _fetchOptionsInternal,
                            cancellationToken
                        );

                    // Log raw JSON if enabled
                    if (
                        _isDebugLoggingEnabled
                        && response?.ContinuationContents?.LiveChatContinuation?.Actions != null
                    )
                    {
                        await LogRawJsonActionsAsync(
                            response.ContinuationContents.LiveChatContinuation.Actions,
                            cancellationToken
                        );
                    }

                    // Process response
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
                            _fetchOptionsInternal = _fetchOptionsInternal with
                            {
                                Continuation = continuation,
                            };
                            currentRetryAttempt = 0; // Reset retries on success
                        }
                        else
                        {
                            _logger.LogInformation(
                                "No continuation token received. Assuming stream ended or chat disabled."
                            );
                            OnChatStopped(new() { Reason = "Stream ended or continuation lost" });
                            break; // Exit loop normally
                        }
                    }
                    else
                    {
                        // GetLiveChatAsync returning null indicates a handled HTTP or JSON error occurred.
                        // Treat this as potentially retryable.
                        throw new Exception(
                            "GetLiveChatAsync returned null response object, indicating a fetch/parse error."
                        );
                    }
                }
                catch (HttpRequestException httpEx)
                    when (httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    // --- Handle Forbidden (403) ---
                    _logger.LogError(
                        httpEx,
                        "Received Forbidden (403). Stream might be region locked, require login, or API access changed. Stopping."
                    );
                    OnErrorOccurred(new ErrorOccurredEventArgs(httpEx));
                    OnChatStopped(new() { Reason = "Received Forbidden (403)" });
                    break; // Stop polling loop
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // --- Handle Other Errors (Retry Logic) ---
                    OnErrorOccurred(new ErrorOccurredEventArgs(ex));
                    currentRetryAttempt++;
                    if (currentRetryAttempt > MaxRetryAttempts)
                    {
                        _logger.LogError(
                            ex,
                            "Maximum retry attempts ({MaxAttempts}) exceeded. Stopping poll.",
                            MaxRetryAttempts
                        );
                        OnChatStopped(
                            new()
                            {
                                Reason = $"Failed after {MaxRetryAttempts} retries: {ex.Message}",
                            }
                        );
                        break; // Stop polling loop
                    }

                    // Calculate delay with jitter
                    double delaySeconds =
                        BaseRetryDelaySeconds * Math.Pow(2, currentRetryAttempt - 1);
                    delaySeconds = Math.Min(delaySeconds, MaxRetryDelaySeconds);
                    delaySeconds *= 1.0 + ((s_random.NextDouble() * 0.4) - 0.2); // +-20% jitter
                    TimeSpan delay = TimeSpan.FromSeconds(delaySeconds);

                    _logger.LogWarning(
                        ex,
                        "Error during poll cycle (Attempt {Attempt}/{MaxAttempts}). Retrying after delay {Delay}...",
                        currentRetryAttempt,
                        MaxRetryAttempts,
                        delay
                    );

                    // Delay before the *next* tick is attempted by PeriodicTimer
                    await Task.Delay(delay, cancellationToken);
                    // PeriodicTimer will handle the wait for the next regular interval *after* this retry delay completes.
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Polling loop cancelled via CancellationToken.");
            // Stop() or ContinueWith will fire ChatStopped
        }
    }
#else

    /// <summary>
    /// Polling loop implementation using Task.Delay (for .NET Standard 2.1).
    /// </summary>
    private async Task PollingLoopWithTaskDelayAsync(CancellationToken cancellationToken)
    {
        int currentRetryAttempt = 0;
        TimeSpan pollInterval = TimeSpan.FromMilliseconds(_options.RequestFrequency);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Check cancellation before fetching
                cancellationToken.ThrowIfCancellationRequested();

                if (_fetchOptionsInternal?.Continuation is null)
                {
                    _logger.LogError(
                        "Error: Fetch options or continuation token missing. Stopping poll."
                    );
                    OnErrorOccurred(
                        new ErrorOccurredEventArgs(
                            new InvalidOperationException("Continuation token lost.")
                        )
                    );
                    OnChatStopped(new() { Reason = "Continuation token missing" });
                    break;
                }

                try
                {
                    // --- Fetch and Process ---
                    (LiveChatResponse? response, string? rawJson) =
                        await _ytHttpClient.GetLiveChatAsync(
                            _fetchOptionsInternal,
                            cancellationToken
                        );

                    // Log raw JSON if enabled
                    if (
                        _isDebugLoggingEnabled
                        && response?.ContinuationContents?.LiveChatContinuation?.Actions != null
                    )
                    {
                        await LogRawJsonActionsAsync(
                            response.ContinuationContents.LiveChatContinuation.Actions,
                            cancellationToken
                        );
                    }

                    // Process response
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
                            _fetchOptionsInternal = _fetchOptionsInternal with
                            {
                                Continuation = continuation!,
                            };
                            currentRetryAttempt = 0; // Reset retries on success
                        }
                        else
                        {
                            _logger.LogInformation(
                                "No continuation token received. Assuming stream ended or chat disabled."
                            );
                            OnChatStopped(new() { Reason = "Stream ended or continuation lost" });
                            break; // Exit loop normally
                        }
                    }
                    else
                    {
                        // GetLiveChatAsync returning null indicates a handled HTTP or JSON error occurred.
                        // Treat this as potentially retryable.
                        throw new Exception(
                            "GetLiveChatAsync returned null response object, indicating a fetch/parse error."
                        );
                    }

                    // --- Normal Delay ---
                    // Delay *after* successful processing before next iteration
                    await Task.Delay(pollInterval, cancellationToken);
                }
                catch (HttpRequestException httpEx)
                    when (httpEx.Message.Contains("403") || httpEx.Message.Contains("Forbidden"))
                {
                    // --- Handle Forbidden (403) ---
                    _logger.LogError(
                        httpEx,
                        "Received Forbidden (403). Stream might be region locked, require login, or API access changed. Stopping."
                    );
                    OnErrorOccurred(new ErrorOccurredEventArgs(httpEx));
                    OnChatStopped(new() { Reason = "Received Forbidden (403)" });
                    break; // Stop polling loop
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // --- Handle Other Errors (Retry Logic) ---
                    OnErrorOccurred(new ErrorOccurredEventArgs(ex));
                    currentRetryAttempt++;
                    if (currentRetryAttempt > MaxRetryAttempts)
                    {
                        _logger.LogError(
                            ex,
                            "Maximum retry attempts ({MaxAttempts}) exceeded. Stopping poll.",
                            MaxRetryAttempts
                        );
                        OnChatStopped(
                            new()
                            {
                                Reason = $"Failed after {MaxRetryAttempts} retries: {ex.Message}",
                            }
                        );
                        break; // Stop polling loop
                    }

                    // Calculate delay with jitter
                    double delaySeconds =
                        BaseRetryDelaySeconds * Math.Pow(2, currentRetryAttempt - 1);
                    delaySeconds = Math.Min(delaySeconds, MaxRetryDelaySeconds);
                    delaySeconds *= 1.0 + ((s_random.NextDouble() * 0.4) - 0.2); // +-20% jitter
                    TimeSpan delay = TimeSpan.FromSeconds(delaySeconds);

                    _logger.LogWarning(
                        ex,
                        "Error during poll cycle (Attempt {Attempt}/{MaxAttempts}). Retrying after delay {Delay}...",
                        currentRetryAttempt,
                        MaxRetryAttempts,
                        delay
                    );

                    // Delay before the *next* attempt
                    await Task.Delay(delay, cancellationToken);
                    // Continue to the next iteration immediately after the retry delay
                    continue;
                }
            } // End while
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Polling loop cancelled via CancellationToken.");
            // Stop() or ContinueWith will fire ChatStopped
        }
    }
#endif

    /// <summary>
    /// Logs raw JSON action items if debug logging is enabled.
    /// </summary>
    private async Task LogRawJsonActionsAsync(
        List<Models.Response.Action> actions,
        CancellationToken cancellationToken
    )
    {
        if (!_isDebugLoggingEnabled || actions == null || actions.Count == 0)
            return;

        // Extract only the relevant item data for logging
        List<AddChatItemActionItem> itemsToLog =
        [
            .. actions.Select(a => a.AddChatItemAction?.Item).WhereNotNull(),
        ];

        if (itemsToLog.Count == 0)
            return;

        // Use SemaphoreSlim for thread-safe file access
        await s_debugLogLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
#if NETSTANDARD2_0 || NETSTANDARD2_1
            // CS8417 fix: Use synchronous 'using' for NS2.0/NS2.1
            using FileStream fs = new(
                _debugLogFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                4096,
                useAsync: true
            ); // useAsync is a hint
            using StreamWriter writer = new(fs, System.Text.Encoding.UTF8);
            foreach (AddChatItemActionItem item in itemsToLog)
            {
                if (cancellationToken.IsCancellationRequested)
                    break; // Check cancellation, but can't pass token to WriteLineAsync
                string jsonLine = JsonSerializer.Serialize(item, s_debugJsonOptions);
                // CS7036 fix: Use WriteLineAsync(string) overload for NS2.0/NS2.1
                await writer.WriteLineAsync(jsonLine).ConfigureAwait(false);
            }
#else
            // Use await using for newer frameworks
            await using FileStream fs = new(
                _debugLogFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                4096,
                useAsync: true
            );
            await using StreamWriter writer = new(fs, System.Text.Encoding.UTF8);
            foreach (AddChatItemActionItem item in itemsToLog)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string jsonLine = JsonSerializer.Serialize(item, s_debugJsonOptions);
                await writer
                    .WriteLineAsync(jsonLine.AsMemory(), cancellationToken)
                    .ConfigureAwait(false);
            }
#endif
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Debug logging cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error writing raw JSON actions to log file '{FilePath}'",
                _debugLogFilePath
            );
            // Consider disabling debug logging temporarily if errors persist
        }
        finally
        {
            s_debugLogLock.Release();
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            _logger.LogInformation("Stop requested. Cancelling polling task...");
            try
            {
                _cancellationTokenSource.Cancel();
                // ChatStopped event is typically fired by the polling task's finally/catch block or ContinueWith
            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning(
                    "Attempted to cancel a disposed CancellationTokenSource during stop."
                );
            }
            // Do not dispose CTS here, let the task finish and potentially clean up in Dispose
        }
        else
        {
            _logger.LogDebug("Stop called but listener was not running or already stopping.");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger.LogDebug("Disposing YTLiveChat service.");
            Stop(); // Ensure cancellation is requested
            // Wait briefly for the task to potentially complete cancellation if needed?
            // Or rely on callers managing the lifetime appropriately.
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _pollingTask = null; // Clear task reference

            // Dispose SemaphoreSlim
            s_debugLogLock.Dispose();

            // Do not dispose _ytHttpClient as its lifetime is managed externally
        }
    }

    // --- Event Invokers ---
    // These remain protected virtual for potential inheritance, though the class is currently sealed implicitly (can be made explicitly sealed).

    /// <summary>Invokes the InitialPageLoaded event.</summary>
    protected virtual void OnInitialPageLoaded(InitialPageLoadedEventArgs e)
    {
        try
        {
            InitialPageLoaded?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking InitialPageLoaded event handler.");
        }
    }

    /// <summary>Invokes the ChatStopped event.</summary>
    protected virtual void OnChatStopped(ChatStoppedEventArgs e)
    {
        try
        {
            ChatStopped?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking ChatStopped event handler.");
        }
    }

    /// <summary>Invokes the ChatReceived event.</summary>
    protected virtual void OnChatReceived(ChatReceivedEventArgs e)
    {
        try
        {
            ChatReceived?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error invoking ChatReceived event handler for item ID {ItemId}.",
                e.ChatItem?.Id
            );
        }
    }

    /// <summary>Invokes the ErrorOccurred event.</summary>
    protected virtual void OnErrorOccurred(ErrorOccurredEventArgs e)
    {
        try
        {
            ErrorOccurred?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error invoking ErrorOccurred event handler for exception: {ErrorMessage}",
                e.GetException()?.Message
            );
        }
    }

    /// <summary>
    /// Fetches the initial HTML page and parses it to get necessary API options.
    /// </summary>
    private async Task<FetchOptions?> GetFetchOptionsAsync(
        string? handle,
        string? channelId,
        string? liveId,
        bool overwrite, // 'overwrite' here refers to overwriting the *cached* options
        CancellationToken cancellationToken = default
    )
    {
        // Check if we need to fetch new options or can use cached ones
        if (_fetchOptionsInternal == null || overwrite)
        {
            _logger.LogInformation(
                "Fetching initial options (Handle: {Handle}, ChannelId: {ChannelId}, LiveId: {LiveId})...",
                handle ?? "N/A",
                channelId ?? "N/A",
                liveId ?? "N/A"
            );
            try
            {
                // Use the injected YTHttpClient instance
                string pageHtml = await _ytHttpClient.GetOptionsAsync(
                    handle,
                    channelId,
                    liveId,
                    cancellationToken
                );
                FetchOptions newOptions = Parser.GetOptionsFromLivePage(pageHtml); // Can throw
                _logger.LogInformation(
                    "Successfully parsed initial options. Live ID: {LiveId}",
                    newOptions.LiveId
                );
                return newOptions; // Return the newly fetched options
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Initial options fetch cancelled.");
                throw; // Re-throw cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get or parse initial options.");
                // Wrap the exception to provide context before re-throwing
                throw new InvalidOperationException(
                    $"Failed to initialize from YouTube page: {ex.Message}",
                    ex
                );
            }
        }
        else
        {
            // Using cached options
            _logger.LogDebug(
                "Using cached initial options. Live ID: {LiveId}",
                _fetchOptionsInternal.LiveId
            );
            return _fetchOptionsInternal;
        }
    }
}
