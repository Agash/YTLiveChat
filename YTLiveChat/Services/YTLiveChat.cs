using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Models;
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
    public event EventHandler<LivestreamStartedEventArgs>? LivestreamStarted;

    /// <inheritdoc />
    public event EventHandler<LivestreamEndedEventArgs>? LivestreamEnded;

    /// <inheritdoc />
    public event EventHandler<RawActionReceivedEventArgs>? RawActionReceived;

    /// <inheritdoc />
    public event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;

    private static readonly Random s_random = new();
    private readonly YTLiveChatOptions _options;
    private readonly YTHttpClient _ytHttpClient;
    private readonly ILogger<YTLiveChat> _logger;

    private FetchOptions? _fetchOptionsInternal;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _pollingTask; // Keep track of the polling task
    private bool _continuousMonitorEnabledForSession;
    private string? _activeLiveId;
    private bool _terminateCurrentSession;

#pragma warning disable CS0618
    private bool ContinuousMonitorSetting => _options.EnableContinuousLivestreamMonitor;
    private int LiveCheckFrequencySetting => _options.LiveCheckFrequency;
#pragma warning restore CS0618

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
    private static readonly byte[] s_debugLogArrayStart = Encoding.UTF8.GetBytes("[\r\n");
    private static readonly byte[] s_debugLogEntrySeparator = Encoding.UTF8.GetBytes(",\r\n");
    private static readonly byte[] s_debugLogArrayEnd = Encoding.UTF8.GetBytes("\r\n]\r\n");
    private static readonly byte[] s_debugLogNewline = Encoding.UTF8.GetBytes(Environment.NewLine);

    private bool _debugLogArrayStarted;
    private bool _debugLogHasEntries;
    private bool _debugLogArrayClosed;

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
        ILogger<YTLiveChat>? logger = null // Logger is optional
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
            ?? Path.Combine(AppContext.BaseDirectory, "ytlivechat_debug_items.json");

#if DEBUG
        // Optionally force debug logging in DEBUG builds, regardless of options
        _isDebugLoggingEnabled = true;
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
            EnsureDebugLogFileIsArray();
        }
    }

    private void EnsureDebugLogDirectoryExists()
    {
        try
        {
            string? directory = Path.GetDirectoryName(_debugLogFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                _ = Directory.CreateDirectory(directory);
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
        _ = _pollingTask.ContinueWith(
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
                    EndActiveLivestream("Background task faulted unexpectedly.");
                    OnChatStopped(new() { Reason = "Background task faulted unexpectedly." });
                }
                else if (task.IsCanceled)
                {
                    _logger.LogInformation("Polling task was cancelled.");
                    EndActiveLivestream("Operation Cancelled");
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
            bool isHandleOrChannelTarget =
                !string.IsNullOrWhiteSpace(handle) || !string.IsNullOrWhiteSpace(channelId);
            bool isDirectLiveId = !string.IsNullOrWhiteSpace(liveId);
            _continuousMonitorEnabledForSession =
                ContinuousMonitorSetting && isHandleOrChannelTarget && !isDirectLiveId;
            _terminateCurrentSession = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                _fetchOptionsInternal = await TryGetFetchOptionsForSessionAsync(
                    handle,
                    channelId,
                    liveId,
                    cancellationToken
                );
                if (_fetchOptionsInternal == null)
                {
                    // Continuous mode: no active live yet, keep checking.
                    await Task
                        .Delay(TimeSpan.FromMilliseconds(LiveCheckFrequencySetting), cancellationToken)
                        .ConfigureAwait(false);
                    continue;
                }

                BeginActiveLivestream(_fetchOptionsInternal.LiveId);
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

                if (!_continuousMonitorEnabledForSession)
                {
                    break;
                }

                if (_terminateCurrentSession)
                {
                    break;
                }
            }
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
            EndActiveLivestream($"Critical error: {ex.Message}");
            OnChatStopped(new() { Reason = $"Critical error: {ex.Message}" });
        }
        finally
        {
            // Ensure state is clean if task ends unexpectedly or normally
            _logger.LogInformation("Listener task finished execution.");
            // ChatStopped should be fired by loop logic, cancellation, or Stop()
        }
    }

    private async Task<FetchOptions?> TryGetFetchOptionsForSessionAsync(
        string? handle,
        string? channelId,
        string? liveId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            FetchOptions? options = await GetFetchOptionsAsync(
                handle,
                channelId,
                liveId,
                true,
                cancellationToken
            );
            return options?.LiveId is null
                ? _continuousMonitorEnabledForSession
                    ? null
                    : throw new InvalidOperationException(
                    "Failed to retrieve valid initial FetchOptions (LiveId is missing)."
                )
                : options;
        }
        catch (InvalidOperationException ex) when (
            _continuousMonitorEnabledForSession && IsLikelyNoActiveLiveError(ex)
        )
        {
            _logger.LogDebug(
                "No active livestream found for monitored target yet. Rechecking in {DelayMs} ms.",
                LiveCheckFrequencySetting
            );
            return null;
        }
    }

    private static bool IsLikelyNoActiveLiveError(InvalidOperationException ex)
    {
        string message = ex.ToString();
        return message.Contains("Live Stream canonical link not found", StringComparison.Ordinal)
            || message.Contains("Initial Continuation token not found", StringComparison.Ordinal)
            || message.Contains("is finished live", StringComparison.Ordinal);
    }

    private void BeginActiveLivestream(string liveId)
    {
        _activeLiveId = liveId;
        OnLivestreamStarted(new() { LiveId = liveId });
    }

    private void EndActiveLivestream(string? reason)
    {
        if (string.IsNullOrWhiteSpace(_activeLiveId))
        {
            return;
        }

        string endedLiveId = _activeLiveId!;
        _activeLiveId = null;
        _fetchOptionsInternal = null;
        OnLivestreamEnded(
            new()
            {
                LiveId = endedLiveId,
                Reason = reason,
            }
        );
    }

    private void EndActiveLivestreamAndStopChat(string reason)
    {
        _terminateCurrentSession = true;
        EndActiveLivestream(reason);
        OnChatStopped(new() { Reason = reason });
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
                    EndActiveLivestreamAndStopChat("Continuation token missing");
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
                        await LogRawJsonActionsAsync(rawJson, cancellationToken);
                    }

                    // Process response
                    if (response != null)
                    {
                        string? continuation = ProcessResponseAndEmitEvents(response, rawJson);

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
                            EndActiveLivestream("Stream ended or continuation lost");
                            if (!_continuousMonitorEnabledForSession)
                            {
                                OnChatStopped(new() { Reason = "Stream ended or continuation lost" });
                            }

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
                    EndActiveLivestreamAndStopChat("Received Forbidden (403)");
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
                        EndActiveLivestreamAndStopChat(
                            $"Failed after {MaxRetryAttempts} retries: {ex.Message}"
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
                    EndActiveLivestreamAndStopChat("Continuation token missing");
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
                        await LogRawJsonActionsAsync(rawJson, cancellationToken);
                    }

                    // Process response
                    if (response != null)
                    {
                        string? continuation = ProcessResponseAndEmitEvents(response, rawJson);

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
                            EndActiveLivestream("Stream ended or continuation lost");
                            if (!_continuousMonitorEnabledForSession)
                            {
                                OnChatStopped(new() { Reason = "Stream ended or continuation lost" });
                            }

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
                    EndActiveLivestreamAndStopChat("Received Forbidden (403)");
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
                        EndActiveLivestreamAndStopChat(
                            $"Failed after {MaxRetryAttempts} retries: {ex.Message}"
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
    /// Processes a live chat response and emits parsed and raw action events.
    /// </summary>
    /// <param name="response">Parsed live chat response model.</param>
    /// <param name="rawJson">Raw response JSON payload.</param>
    /// <returns>The next continuation token, if available.</returns>
    private string? ProcessResponseAndEmitEvents(LiveChatResponse response, string? rawJson)
    {
        bool shouldEmitRawActions = RawActionReceived != null;

        if (!shouldEmitRawActions)
        {
            (
                List<ChatItem> items,
                string? continuationToken
            ) = Parser.ParseLiveChatResponse(response);
            foreach (ChatItem chatItem in items)
            {
                OnChatReceived(new() { ChatItem = chatItem });
            }

            return continuationToken;
        }

        (
            List<(ChatItem Item, int ActionIndex)> indexedItems,
            string? continuation
        ) = Parser.ParseLiveChatResponseWithActionIndex(response);

        Dictionary<int, ChatItem> parsedByActionIndex = new(indexedItems.Count);
        foreach ((ChatItem item, int actionIndex) in indexedItems)
        {
            OnChatReceived(new() { ChatItem = item });
            parsedByActionIndex[actionIndex] = item;
        }

        List<JsonElement>? rawActions = ExtractRawActions(rawJson);
        if (rawActions is null)
        {
            _logger.LogDebug(
                "RawActionReceived is subscribed but raw actions could not be extracted from payload."
            );
            return continuation;
        }

        for (int i = 0; i < rawActions.Count; i++)
        {
            _ = parsedByActionIndex.TryGetValue(i, out ChatItem? parsedChatItem);
            OnRawActionReceived(
                new()
                {
                    RawAction = rawActions[i],
                    ParsedChatItem = parsedChatItem,
                }
            );
        }

        return continuation;
    }

    /// <summary>
    /// Extracts raw action objects from the live chat response JSON payload.
    /// </summary>
    /// <param name="rawJson">Raw response JSON payload.</param>
    /// <returns>List of raw action elements, or null when unavailable.</returns>
    private List<JsonElement>? ExtractRawActions(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return null;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(rawJson!);
            JsonElement root = document.RootElement;
            if (
                !root.TryGetProperty("continuationContents", out JsonElement continuationContents)
                || !continuationContents.TryGetProperty(
                    "liveChatContinuation",
                    out JsonElement liveChatContinuation
                )
                || !liveChatContinuation.TryGetProperty("actions", out JsonElement actions)
                || actions.ValueKind != JsonValueKind.Array
            )
            {
                return null;
            }

            List<JsonElement> extractedActions = new(actions.GetArrayLength());
            foreach (JsonElement action in actions.EnumerateArray())
            {
                extractedActions.Add(action.Clone());
            }

            return extractedActions;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse raw live chat response JSON for raw actions.");
            return null;
        }
    }

    /// <summary>
    /// Logs raw JSON action items if debug logging is enabled.
    /// </summary>
    private async Task LogRawJsonActionsAsync(string? rawJson, CancellationToken cancellationToken)
    {
        if (!_isDebugLoggingEnabled || rawJson == null)
            return;

        string entry = rawJson.Trim();
        if (string.IsNullOrEmpty(entry))
            return;

        await s_debugLogLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await AppendJsonArrayEntryAsync(entry, cancellationToken).ConfigureAwait(false);
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
        }
        finally
        {
            _ = s_debugLogLock.Release();
        }
    }

    private void EnsureDebugLogFileIsArray()
    {
        try
        {
            if (!File.Exists(_debugLogFilePath))
            {
                File.WriteAllBytes(_debugLogFilePath, s_debugLogArrayStart);
                return;
            }

            using FileStream fs = new(
                _debugLogFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );

            if (fs.Length == 0)
            {
                fs.Dispose();
                File.WriteAllBytes(_debugLogFilePath, s_debugLogArrayStart);
                return;
            }

            char? firstNonWhitespace = GetFirstNonWhitespaceChar(fs);
            if (firstNonWhitespace == '[')
            {
                return;
            }

            fs.Dispose();
            string legacyPath = $"{_debugLogFilePath}.{DateTime.UtcNow:yyyyMMddHHmmss}.legacy";
            File.Move(_debugLogFilePath, legacyPath);
            File.WriteAllBytes(_debugLogFilePath, s_debugLogArrayStart);

            _logger.LogWarning(
                "Debug log at '{FilePath}' was not a JSON array. Moved old content to '{LegacyPath}' and started a new array.",
                _debugLogFilePath,
                legacyPath
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error preparing debug log file '{FilePath}'.",
                _debugLogFilePath
            );
        }
    }

    private async Task AppendJsonArrayEntryAsync(string entryJson, CancellationToken cancellationToken)
    {
        using FileStream fs = new(
            _debugLogFilePath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.Read,
            4096,
            useAsync: true
        );

        await PrepareJsonArrayStreamAsync(fs, cancellationToken).ConfigureAwait(false);

        if (_debugLogHasEntries)
        {
            await WriteBytesAsync(fs, s_debugLogEntrySeparator, cancellationToken).ConfigureAwait(false);
        }

        byte[] entryBytes = Encoding.UTF8.GetBytes(entryJson);
        await WriteBytesAsync(fs, entryBytes, cancellationToken).ConfigureAwait(false);
        await WriteBytesAsync(fs, s_debugLogNewline, cancellationToken).ConfigureAwait(false);

        _debugLogHasEntries = true;
    }

    private async Task PrepareJsonArrayStreamAsync(FileStream fs, CancellationToken cancellationToken)
    {
        if (_debugLogArrayClosed)
        {
            // Logging after closing is unexpected; reopen by truncating
            fs.SetLength(0);
            _debugLogArrayClosed = false;
            _debugLogArrayStarted = false;
            _debugLogHasEntries = false;
        }

        if (_debugLogArrayStarted)
        {
            _ = fs.Seek(0, SeekOrigin.End);
            return;
        }

        if (fs.Length == 0)
        {
            await WriteBytesAsync(fs, s_debugLogArrayStart, cancellationToken).ConfigureAwait(false);
            _debugLogArrayStarted = true;
            _debugLogHasEntries = false;
            return;
        }

        await TrimTrailingClosingBracketAsync(fs, cancellationToken).ConfigureAwait(false);
        _debugLogArrayStarted = true;
        _debugLogHasEntries = fs.Length > s_debugLogArrayStart.Length;
    }

    private static async Task TrimTrailingClosingBracketAsync(FileStream fs, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[1];
        long position = fs.Length;
        while (position > 0)
        {
            position--;
            _ = fs.Seek(position, SeekOrigin.Begin);
            int bytesRead = await ReadByteAsync(fs, buffer, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
                break;

            char current = (char)buffer[0];
            if (char.IsWhiteSpace(current))
                continue;

            if (current == ']')
            {
                fs.SetLength(position);
            }

            break;
        }

        _ = fs.Seek(0, SeekOrigin.End);
    }

    private static async Task WriteBytesAsync(
        FileStream fs,
        byte[] buffer,
        CancellationToken cancellationToken
    ) =>
#if NETSTANDARD2_0
        await fs.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
#else
        await fs.WriteAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
#endif


    private static async Task<int> ReadByteAsync(
        FileStream fs,
        byte[] buffer,
        CancellationToken cancellationToken
    ) =>
#if NETSTANDARD2_0
        await fs.ReadAsync(buffer, 0, 1, cancellationToken).ConfigureAwait(false);
#else
        await fs.ReadAsync(buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
#endif


    private static char? GetFirstNonWhitespaceChar(FileStream fs)
    {
        byte[] buffer = new byte[1];
        _ = fs.Seek(0, SeekOrigin.Begin);
        while (fs.Position < fs.Length)
        {
            int read = fs.Read(buffer, 0, 1);
            if (read == 0)
            {
                break;
            }

            char current = (char)buffer[0];
            if (!char.IsWhiteSpace(current))
            {
                return current;
            }
        }

        return null;
    }

    private void FinalizeDebugJsonLog()
    {
        if (!_isDebugLoggingEnabled || !_debugLogArrayStarted || _debugLogArrayClosed)
        {
            return;
        }

        s_debugLogLock.Wait();
        try
        {
            using FileStream fs = new(
                _debugLogFilePath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.Read
            );
            _ = fs.Seek(0, SeekOrigin.End);
            fs.Write(s_debugLogArrayEnd, 0, s_debugLogArrayEnd.Length);
            _debugLogArrayClosed = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error finalizing debug JSON log array at '{FilePath}'",
                _debugLogFilePath
            );
        }
        finally
        {
            _ = s_debugLogLock.Release();
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
    public async IAsyncEnumerable<ChatItem> StreamChatItemsAsync(
        string? handle = null,
        string? channelId = null,
        string? liveId = null,
        bool overwrite = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        Channel<ChatItem> channel = Channel.CreateUnbounded<ChatItem>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            }
        );

        void HandleChatReceived(object? _, ChatReceivedEventArgs e) => _ = channel
            .Writer.TryWrite(e.ChatItem);
        void HandleChatStopped(object? _, ChatStoppedEventArgs e) => channel.Writer.TryComplete();
        void HandleErrorOccurred(object? _, ErrorOccurredEventArgs e) => channel.Writer.TryComplete(
            e.GetException()
        );

        ChatReceived += HandleChatReceived;
        ChatStopped += HandleChatStopped;
        ErrorOccurred += HandleErrorOccurred;

        using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(() =>
            channel.Writer.TryComplete()
        );

        try
        {
            Start(handle, channelId, liveId, overwrite);

            await foreach (
                ChatItem item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)
            )
            {
                yield return item;
            }
        }
        finally
        {
            ChatReceived -= HandleChatReceived;
            ChatStopped -= HandleChatStopped;
            ErrorOccurred -= HandleErrorOccurred;
            Stop();
            _ = channel.Writer.TryComplete();
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<RawActionReceivedEventArgs> StreamRawActionsAsync(
        string? handle = null,
        string? channelId = null,
        string? liveId = null,
        bool overwrite = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        Channel<RawActionReceivedEventArgs> channel = Channel.CreateUnbounded<
            RawActionReceivedEventArgs
        >(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            }
        );

        void HandleRawActionReceived(object? _, RawActionReceivedEventArgs e) => _ = channel
            .Writer.TryWrite(e);
        void HandleChatStopped(object? _, ChatStoppedEventArgs e) => channel.Writer.TryComplete();
        void HandleErrorOccurred(object? _, ErrorOccurredEventArgs e) => channel.Writer.TryComplete(
            e.GetException()
        );

        RawActionReceived += HandleRawActionReceived;
        ChatStopped += HandleChatStopped;
        ErrorOccurred += HandleErrorOccurred;

        using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(() =>
            channel.Writer.TryComplete()
        );

        try
        {
            Start(handle, channelId, liveId, overwrite);

            await foreach (
                RawActionReceivedEventArgs rawAction in channel
                    .Reader.ReadAllAsync(cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                yield return rawAction;
            }
        }
        finally
        {
            RawActionReceived -= HandleRawActionReceived;
            ChatStopped -= HandleChatStopped;
            ErrorOccurred -= HandleErrorOccurred;
            Stop();
            _ = channel.Writer.TryComplete();
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
            _logger.LogDebug("Disposing YTLiveChat service. Calling Stop().");
            Stop(); // Ensures cancellation is signaled

            // Dispose the CancellationTokenSource if it hasn't been already
            // (e.g., if Start was never called or called multiple times with overwrite)
            try
            {
                _cancellationTokenSource?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                _logger.LogDebug(
                    "CancellationTokenSource was already disposed during YTLiveChat.Dispose."
                );
            }

            _cancellationTokenSource = null;

            // Clear the task reference. The task itself will complete due to cancellation.
            // We don't `await` it here as Dispose is synchronous.
            _pollingTask = null;

            // Finalize the optional debug JSON array log (adds closing bracket).
            FinalizeDebugJsonLog();

            _logger.LogDebug("YTLiveChat service Dispose complete.");
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

    /// <summary>Invokes the LivestreamStarted event.</summary>
    protected virtual void OnLivestreamStarted(LivestreamStartedEventArgs e)
    {
        try
        {
            LivestreamStarted?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking LivestreamStarted event handler.");
        }
    }

    /// <summary>Invokes the LivestreamEnded event.</summary>
    protected virtual void OnLivestreamEnded(LivestreamEndedEventArgs e)
    {
        try
        {
            LivestreamEnded?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking LivestreamEnded event handler.");
        }
    }

    /// <summary>Invokes the RawActionReceived event.</summary>
    protected virtual void OnRawActionReceived(RawActionReceivedEventArgs e)
    {
        try
        {
            RawActionReceived?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking RawActionReceived event handler.");
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

