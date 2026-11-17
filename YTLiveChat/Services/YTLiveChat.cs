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
    public event EventHandler<LivestreamInaccessibleEventArgs>? LivestreamInaccessible;

    /// <inheritdoc />
    public event EventHandler<RawActionReceivedEventArgs>? RawActionReceived;

    /// <inheritdoc />
    public event EventHandler<PollUpdatedEventArgs>? PollUpdated;

    /// <inheritdoc />
    public event EventHandler<PollClosedEventArgs>? PollClosed;

    /// <inheritdoc />
    public event EventHandler<ChatItemDeletedEventArgs>? ChatItemDeleted;

    /// <inheritdoc />
    public event EventHandler<ChatItemsDeletedByAuthorEventArgs>? ChatItemsDeletedByAuthor;

    /// <inheritdoc />
    public event EventHandler<BannerAddedEventArgs>? BannerAdded;

    /// <inheritdoc />
    public event EventHandler<BannerRemovedEventArgs>? BannerRemoved;

    /// <inheritdoc />
    public event EventHandler<ChatItemReplacedEventArgs>? ChatItemReplaced;

    /// <inheritdoc />
    public event EventHandler<EngagementMessageReceivedEventArgs>? EngagementMessageReceived;

    /// <inheritdoc />
    public event EventHandler<GiftReceivedEventArgs>? GiftReceived;

    /// <inheritdoc />
    public event EventHandler<CreatorGoalReceivedEventArgs>? CreatorGoalReceived;

    /// <inheritdoc />
    public event EventHandler<ReactionsReceivedEventArgs>? ReactionsReceived;

    /// <inheritdoc />
    public event EventHandler<ReplyCountReceivedEventArgs>? ReplyCountReceived;

    /// <inheritdoc />
    public event EventHandler<ChatModeChangedEventArgs>? ChatModeChanged;

    /// <inheritdoc />
    public event EventHandler<StreamMetadataUpdatedEventArgs>? StreamMetadataUpdated;

    /// <inheritdoc />
    public event EventHandler<EntityMutationReceivedEventArgs>? EntityMutationReceived;

    /// <inheritdoc />
    public event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;

    private static readonly Random s_random = new();
    private readonly YTLiveChatOptions _options;
    private readonly YTHttpClient _ytHttpClient;
    private readonly ILogger<YTLiveChat> _logger;
    private readonly TimeProvider _timeProvider;

    private FetchOptions? _fetchOptionsInternal;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _pollingTask; // Keep track of the polling task
    private bool _continuousMonitorEnabledForSession;
    private string? _activeLiveId;
    private bool _activeLiveBroadcasting;
    private bool _chatViewPending;
    private readonly Dictionary<string, Contracts.Models.PollItem> _openPolls = new(
        StringComparer.Ordinal
    );
    private DateTimeOffset _nextBroadcastCheck;
    private string? _lastInaccessibleLiveId;
    private bool _terminateCurrentSession;
    private readonly Dictionary<string, DateTimeOffset> _temporaryInaccessibleAutoDetectedLiveIds =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan s_inaccessibleCandidateRetryFloor = TimeSpan.FromMinutes(5);

#pragma warning disable CS0618
    private bool ContinuousMonitorSetting => _options.EnableContinuousLivestreamMonitor;
    private int LiveCheckFrequencySetting => _options.LiveCheckFrequency;
    private bool RequireActiveBroadcastForAutoDetectedStreamSetting =>
        _options.RequireActiveBroadcastForAutoDetectedStream;
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
    /// <param name="timeProvider">Clock and timers for polling and live checks; <see cref="TimeProvider.System"/> when omitted.</param>
    /// <exception cref="ArgumentNullException">Thrown if options or ytHttpClient is null.</exception>
    public YTLiveChat(
        YTLiveChatOptions options,
        YTHttpClient ytHttpClient,
        ILogger<YTLiveChat>? logger = null,
        TimeProvider? timeProvider = null
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
        _timeProvider = timeProvider ?? TimeProvider.System;

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
            _lastInaccessibleLiveId = null;
            _temporaryInaccessibleAutoDetectedLiveIds.Clear();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_continuousMonitorEnabledForSession)
                {
                    _logger.LogInformation(
                        "Monitor probe started for target (Handle: {Handle}, ChannelId: {ChannelId}).",
                        handle ?? "N/A",
                        channelId ?? "N/A"
                    );
                }

                _fetchOptionsInternal = await TryGetFetchOptionsForSessionAsync(
                    handle,
                    channelId,
                    liveId,
                    cancellationToken
                );
                if (_fetchOptionsInternal == null)
                {
                    // Continuous mode: no active live yet, keep checking.
                    if (_continuousMonitorEnabledForSession)
                    {
                        _logger.LogInformation(
                            "Monitor probe found no usable livestream. Next check in {DelayMs} ms.",
                            LiveCheckFrequencySetting
                        );
                    }

                    await DelayAsync(
                            TimeSpan.FromMilliseconds(LiveCheckFrequencySetting),
                            cancellationToken
                        )
                        .ConfigureAwait(false);
                    continue;
                }

                BeginActiveLivestream(_fetchOptionsInternal);
                OnInitialPageLoaded(new() { LiveId = _fetchOptionsInternal.LiveId });
                _logger.LogInformation(
                    "Initial page loaded for Live ID: {LiveId}. Starting polling loop...",
                    _fetchOptionsInternal.LiveId
                );

                // The watch page's metadata (viewers, title, likes) runs beside the chat for as long as it does.
                using (
                    CancellationTokenSource metadataCancellation =
                        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                )
                {
                    Task metadata = MetadataLoopAsync(
                        _fetchOptionsInternal,
                        metadataCancellation.Token
                    );
                    await PollingLoopAsync(cancellationToken);
                    metadataCancellation.Cancel();
                    await metadata.ConfigureAwait(false);
                }

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
        string? resolvedHandle = handle;
        string? resolvedChannelId = channelId;
        string? resolvedLiveId = liveId;

        if (
            _continuousMonitorEnabledForSession
            && string.IsNullOrWhiteSpace(liveId)
            && (!string.IsNullOrWhiteSpace(handle) || !string.IsNullOrWhiteSpace(channelId))
        )
        {
            string? streamsCandidate = await TrySelectCandidateFromStreamsPageAsync(
                handle,
                channelId,
                cancellationToken
            );

            if (!string.IsNullOrWhiteSpace(streamsCandidate))
            {
                _logger.LogInformation(
                    "Monitor probe selected streams candidate {LiveId} before /live fallback.",
                    streamsCandidate
                );
                resolvedLiveId = streamsCandidate;
                resolvedHandle = null;
                resolvedChannelId = null;
            }
            else
            {
                _logger.LogInformation(
                    "Monitor probe found no streams candidate. Falling back to /live resolution."
                );
            }
        }

        try
        {
            FetchOptions? options = await GetFetchOptionsAsync(
                resolvedHandle,
                resolvedChannelId,
                resolvedLiveId,
                true,
                cancellationToken
            );
            return options is null
                ? _continuousMonitorEnabledForSession
                    ? null
                    : throw new InvalidOperationException(
                        "No acceptable livestream candidate found for the provided target."
                    )
                : options.LiveId is null
                    ? throw new InvalidOperationException(
                        "Failed to retrieve valid initial FetchOptions (LiveId is missing)."
                    )
                    : options;
        }
        catch (InvalidOperationException ex)
            when (_continuousMonitorEnabledForSession && IsLikelyInaccessibleLiveError(ex))
        {
            if (!string.IsNullOrWhiteSpace(resolvedLiveId))
            {
                _temporaryInaccessibleAutoDetectedLiveIds[resolvedLiveId!] =
                    _timeProvider.GetUtcNow() + GetInaccessibleCandidateRetryDelay();
            }

            NotifyInaccessibleLivestreamCandidate(resolvedLiveId, ex.Message);
            return null;
        }
        catch (InvalidOperationException ex)
            when (_continuousMonitorEnabledForSession && IsLikelyNoActiveLiveError(ex))
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
            || message.Contains("Live Stream ID not found", StringComparison.Ordinal)
            || message.Contains("Initial Continuation token not found", StringComparison.Ordinal)
            || message.Contains("is finished live", StringComparison.Ordinal)
            || message.Contains(
                "No acceptable livestream candidate found",
                StringComparison.Ordinal
            );
    }

    private static bool IsLikelyInaccessibleLiveError(InvalidOperationException ex)
    {
        string message = ex.ToString();
        return message.Contains("Live stream is inaccessible", StringComparison.Ordinal);
    }

    private void NotifyInaccessibleLivestreamCandidate(string? liveId, string reason)
    {
        if (string.IsNullOrWhiteSpace(liveId))
        {
            _logger.LogInformation(
                "Detected inaccessible livestream candidate but no liveId was available. Reason: {Reason}",
                reason
            );
            return;
        }

        if (string.Equals(_lastInaccessibleLiveId, liveId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(
                "Livestream candidate {LiveId} is still inaccessible. Reason: {Reason}",
                liveId,
                reason
            );
            return;
        }

        _lastInaccessibleLiveId = liveId;
        _logger.LogInformation(
            "Detected inaccessible livestream candidate {LiveId}. Reason: {Reason}",
            liveId,
            reason
        );
        OnLivestreamInaccessible(
            new LivestreamInaccessibleEventArgs { LiveId = liveId!, Reason = reason }
        );
    }

    // A waiting room's chat opens before the stream goes live, so the stream counts as started only once its page
    // says it is broadcasting; until then the watch page is checked again every LiveCheckFrequency.
    private void BeginActiveLivestream(FetchOptions options)
    {
        _activeLiveId = options.LiveId;
        _activeLiveBroadcasting = false;
        _openPolls.Clear();
        _chatViewPending = _options.ChatView == ChatView.AllMessages;
        _lastInaccessibleLiveId = null;
        _temporaryInaccessibleAutoDetectedLiveIds.Clear();
        if (options.IsBroadcasting)
        {
            AnnounceBroadcast(options.LiveId);
        }
        else
        {
            _logger.LogInformation(
                "Live ID {LiveId} is not broadcasting yet; checking every {DelayMs} ms.",
                options.LiveId,
                LiveCheckFrequencySetting
            );
            _nextBroadcastCheck =
                _timeProvider.GetUtcNow() + TimeSpan.FromMilliseconds(LiveCheckFrequencySetting);
        }
    }

    private void AnnounceBroadcast(string liveId)
    {
        _activeLiveBroadcasting = true;
        OnLivestreamStarted(new() { LiveId = liveId });
    }

    private async Task CheckBroadcastStartedAsync(CancellationToken cancellationToken)
    {
        string? liveId = _activeLiveId;
        FetchOptions? options = _fetchOptionsInternal;
        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (
            liveId is null
            || options is null
            || _activeLiveBroadcasting
            || now < _nextBroadcastCheck
        )
        {
            return;
        }

        _nextBroadcastCheck = now + TimeSpan.FromMilliseconds(LiveCheckFrequencySetting);
        string playerJson;
        try
        {
            playerJson = await _ytHttpClient
                .GetPlayerAsync(options, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Whether {LiveId} is broadcasting could not be read.", liveId);
            return;
        }

        if (liveId == _activeLiveId && Parser.IsBroadcastingPlayerResponse(playerJson))
        {
            _logger.LogInformation("Live ID {LiveId} is broadcasting.", liveId);
            AnnounceBroadcast(liveId);
        }
    }

    private void EndActiveLivestream(string? reason)
    {
        if (string.IsNullOrWhiteSpace(_activeLiveId))
        {
            return;
        }

        string endedLiveId = _activeLiveId!;
        bool wasBroadcasting = _activeLiveBroadcasting;
        _activeLiveId = null;
        _activeLiveBroadcasting = false;
        _fetchOptionsInternal = null;
        if (wasBroadcasting)
        {
            OnLivestreamEnded(new() { LiveId = endedLiveId, Reason = reason });
        }
    }

    private void EndActiveLivestreamAndStopChat(string reason)
    {
        _terminateCurrentSession = true;
        EndActiveLivestream(reason);
        OnChatStopped(new() { Reason = reason });
    }

    /// <summary>
    /// Polls the chat <see cref="YTLiveChatOptions.RequestFrequency"/> milliseconds after the previous poll
    /// finished, retrying failed polls with backoff.
    /// </summary>
    private async Task PollingLoopAsync(CancellationToken cancellationToken)
    {
        int currentRetryAttempt = 0;
        TimeSpan pollInterval = TimeSpan.FromMilliseconds(_options.RequestFrequency);
        TimeSpan waitAfterPoll = pollInterval;

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
                        string? continuation =
                            TrySwitchToAllMessages(response)
                            ?? ProcessResponseAndEmitEvents(response, rawJson);

                        if (!string.IsNullOrEmpty(continuation))
                        {
                            _fetchOptionsInternal = _fetchOptionsInternal with
                            {
                                Continuation = continuation!,
                            };
                            currentRetryAttempt = 0; // Reset retries on success
                            waitAfterPoll = NextPollWait(
                                waitAfterPoll,
                                pollInterval,
                                _options.IdleRequestFrequency,
                                response
                            );
                            await CheckBroadcastStartedAsync(cancellationToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "No continuation token received. Assuming stream ended or chat disabled."
                            );
                            EndActiveLivestream("Stream ended or continuation lost");
                            if (!_continuousMonitorEnabledForSession)
                            {
                                OnChatStopped(
                                    new() { Reason = "Stream ended or continuation lost" }
                                );
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

                    await DelayAsync(waitAfterPoll, cancellationToken);
                }
                catch (HttpRequestException httpEx) when (IsForbidden(httpEx))
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
                    await DelayAsync(delay, cancellationToken);
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

    // Measured 2026-11-17 (pollprobe on chats of 25,000, 900 and 50 viewers at a 1 s interval): YouTube answers
    // every poll with an invalidation continuation whose timeoutMs is 10000, the ceiling its own web client long-polls
    // to between push notifications; a message is already 1.6 to 2.7 s old when it can first be read; and quiet
    // chats return nothing on almost every poll (166 and 170 of 171). So a poll that brings nothing stretches the
    // next wait by half, up to IdleRequestFrequency and never past YouTube's timeoutMs, and the first poll that
    // brings anything goes back to RequestFrequency.
    internal static TimeSpan NextPollWait(
        TimeSpan current,
        TimeSpan fastest,
        int idleMs,
        LiveChatResponse response
    )
    {
        LiveChatContinuation? chat = response.ContinuationContents?.LiveChatContinuation;
        if (chat?.Actions is { Count: > 0 })
        {
            return fastest;
        }

        Continuation? next = chat?.Continuations?.FirstOrDefault();
        int timeoutMs =
            next?.InvalidationContinuationData?.TimeoutMs
            ?? next?.TimedContinuationData?.TimeoutMs
            ?? 0;
        double ceilingMs = Math.Max(fastest.TotalMilliseconds, idleMs);
        if (timeoutMs > 0)
        {
            ceilingMs = Math.Min(ceilingMs, Math.Max(fastest.TotalMilliseconds, timeoutMs));
        }

        return TimeSpan.FromMilliseconds(Math.Min(ceilingMs, current.TotalMilliseconds * 1.5));
    }

    private Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
#if NET8_0_OR_GREATER
        Task.Delay(delay, _timeProvider, cancellationToken);
#else
        _timeProvider.Delay(delay, cancellationToken);
#endif

    private static bool IsForbidden(HttpRequestException exception) =>
#if NET8_0_OR_GREATER
        exception.StatusCode == System.Net.HttpStatusCode.Forbidden;
#else
        // No status code on HttpRequestException before .NET 5; EnsureSuccessStatusCode names it in the message.
        exception.Message.Contains("403") || exception.Message.Contains("Forbidden");
#endif

    /// <summary>
    /// Processes a live chat response and emits parsed and raw action events.
    /// </summary>
    /// <param name="response">Parsed live chat response model.</param>
    /// <param name="rawJson">Raw response JSON payload.</param>
    /// <returns>The next continuation token, if available.</returns>
    private string? ProcessResponseAndEmitEvents(LiveChatResponse response, string? rawJson)
    {
        // Fire non-ChatItem events (polls, deletions, banners) for any subscribed handlers.
        EmitActionEvents(response.ContinuationContents?.LiveChatContinuation?.Actions);
        EmitEntityEvents(response);

        bool shouldEmitRawActions = RawActionReceived != null;

        if (!shouldEmitRawActions)
        {
            (List<ChatItem> items, string? continuationToken) = Parser.ParseLiveChatResponse(
                response,
                _timeProvider
            );
            foreach (ChatItem chatItem in items)
            {
                OnChatReceived(new() { ChatItem = chatItem });
            }

            return continuationToken;
        }

        (List<(ChatItem Item, int ActionIndex)> indexedItems, string? continuation) =
            Parser.ParseLiveChatResponseWithActionIndex(response, _timeProvider);

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
                new() { RawAction = rawActions[i], ParsedChatItem = parsedChatItem }
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
            string legacyPath =
                $"{_debugLogFilePath}.{_timeProvider.GetUtcNow():yyyyMMddHHmmss}.legacy";
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
            _logger.LogError(ex, "Error preparing debug log file '{FilePath}'.", _debugLogFilePath);
        }
    }

    private async Task AppendJsonArrayEntryAsync(
        string entryJson,
        CancellationToken cancellationToken
    )
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
            await WriteBytesAsync(fs, s_debugLogEntrySeparator, cancellationToken)
                .ConfigureAwait(false);
        }

        byte[] entryBytes = Encoding.UTF8.GetBytes(entryJson);
        await WriteBytesAsync(fs, entryBytes, cancellationToken).ConfigureAwait(false);
        await WriteBytesAsync(fs, s_debugLogNewline, cancellationToken).ConfigureAwait(false);

        _debugLogHasEntries = true;
    }

    private async Task PrepareJsonArrayStreamAsync(
        FileStream fs,
        CancellationToken cancellationToken
    )
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
            await WriteBytesAsync(fs, s_debugLogArrayStart, cancellationToken)
                .ConfigureAwait(false);
            _debugLogArrayStarted = true;
            _debugLogHasEntries = false;
            return;
        }

        await TrimTrailingClosingBracketAsync(fs, cancellationToken).ConfigureAwait(false);
        _debugLogArrayStarted = true;
        _debugLogHasEntries = fs.Length > s_debugLogArrayStart.Length;
    }

    private static async Task TrimTrailingClosingBracketAsync(
        FileStream fs,
        CancellationToken cancellationToken
    )
    {
        byte[] buffer = new byte[1];
        long position = fs.Length;
        while (position > 0)
        {
            position--;
            _ = fs.Seek(position, SeekOrigin.Begin);
            int bytesRead = await ReadByteAsync(fs, buffer, cancellationToken)
                .ConfigureAwait(false);
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

        void HandleChatReceived(object? _, ChatReceivedEventArgs e) =>
            _ = channel.Writer.TryWrite(e.ChatItem);
        void HandleChatStopped(object? _, ChatStoppedEventArgs e) => channel.Writer.TryComplete();
        void HandleErrorOccurred(object? _, ErrorOccurredEventArgs e) =>
            channel.Writer.TryComplete(e.GetException());

        ChatReceived += HandleChatReceived;
        ChatStopped += HandleChatStopped;
        ErrorOccurred += HandleErrorOccurred;

        using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(
            () =>
                channel.Writer.TryComplete()
        );

        try
        {
            Start(handle, channelId, liveId, overwrite);

            await foreach (
                ChatItem item in channel
                    .Reader.ReadAllAsync(cancellationToken)
                    .ConfigureAwait(false)
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
        Channel<RawActionReceivedEventArgs> channel =
            Channel.CreateUnbounded<RawActionReceivedEventArgs>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false,
                }
            );

        void HandleRawActionReceived(object? _, RawActionReceivedEventArgs e) =>
            _ = channel.Writer.TryWrite(e);
        void HandleChatStopped(object? _, ChatStoppedEventArgs e) => channel.Writer.TryComplete();
        void HandleErrorOccurred(object? _, ErrorOccurredEventArgs e) =>
            channel.Writer.TryComplete(e.GetException());

        RawActionReceived += HandleRawActionReceived;
        ChatStopped += HandleChatStopped;
        ErrorOccurred += HandleErrorOccurred;

        using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(
            () =>
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

    /// <summary>Iterates parsed actions and fires poll/delete/banner events for subscribed handlers.</summary>
    // Live state YouTube keeps outside chat actions: goals, reactions and the rest of frameworkUpdates.
    private void EmitEntityEvents(LiveChatResponse response)
    {
        if (
            CreatorGoalReceived is null
            && ReactionsReceived is null
            && ReplyCountReceived is null
            && EntityMutationReceived is null
        )
        {
            return;
        }

        foreach (EntityMutation mutation in Parser.ExtractEntityMutations(response))
        {
            OnEntityMutationReceived(new() { Mutation = mutation });
            if (Parser.ToCreatorGoal(mutation) is { } goal)
            {
                OnCreatorGoalReceived(new() { CreatorGoal = goal });
            }
            else if (Parser.ToReactions(mutation) is { } reactions)
            {
                OnReactionsReceived(new() { Reactions = reactions });
            }
            else if (Parser.ToReplyCount(mutation) is { } replies)
            {
                OnReplyCountReceived(new() { ReplyCount = replies });
            }
        }
    }

    // The watch page hands out the Top chat continuation (verified 2026-11-17: its chat type field is 4, as on the
    // "Top chat" entry of the view selector; "Live chat" is 1), which hides messages YouTube considers potential
    // spam. The first response's header lists both views, Top chat first and Live chat second (titles are
    // localized); for all messages the session moves to the second before emitting anything, so the Top chat
    // backlog is not emitted twice.
    private string? TrySwitchToAllMessages(LiveChatResponse response)
    {
        if (!_chatViewPending)
        {
            return null;
        }

        _chatViewPending = false;
        List<SubMenuItem>? views = response
            .ContinuationContents
            ?.LiveChatContinuation
            ?.Header
            ?.LiveChatHeaderRenderer
            ?.ViewSelector
            ?.SortFilterSubMenuRenderer
            ?.SubMenuItems;
        if (views is not { Count: >= 2 } || views[1].Selected)
        {
            return null;
        }

        string? allMessages = views[1].Continuation?.ReloadContinuationData?.Continuation;
        if (string.IsNullOrEmpty(allMessages))
        {
            return null;
        }

        _logger.LogInformation("Reading all messages (Live chat) instead of Top chat.");
        return allMessages;
    }

    private static readonly TimeSpan s_metadataFloor = TimeSpan.FromSeconds(5);

    // Polled at the pace YouTube names in each response; idle (no request) while nobody subscribes.
    private async Task MetadataLoopAsync(FetchOptions options, CancellationToken cancellationToken)
    {
        string? continuation = null;
        TimeSpan wait = s_metadataFloor;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (StreamMetadataUpdated is not null)
                {
                    try
                    {
                        string json = await _ytHttpClient
                            .GetUpdatedMetadataAsync(options, continuation, cancellationToken)
                            .ConfigureAwait(false);
                        (StreamMetadataItem metadata, string? next, int timeoutMs) =
                            Parser.ParseUpdatedMetadata(json, options.LiveId);
                        continuation = next ?? continuation;
                        wait = TimeSpan.FromMilliseconds(
                            Math.Max(s_metadataFloor.TotalMilliseconds, timeoutMs)
                        );
                        OnStreamMetadataUpdated(new() { Metadata = metadata });
                    }
                    catch (Exception ex) when (ex is HttpRequestException or JsonException)
                    {
                        _logger.LogWarning(
                            ex,
                            "Stream metadata for {LiveId} could not be read.",
                            options.LiveId
                        );
                    }
                }

                await DelayAsync(wait, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The chat stopped; the metadata stops with it.
        }
    }

    /// <summary>Invokes the StreamMetadataUpdated event.</summary>
    protected virtual void OnStreamMetadataUpdated(StreamMetadataUpdatedEventArgs e)
    {
        try
        {
            StreamMetadataUpdated?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking StreamMetadataUpdated event handler.");
        }
    }

    /// <summary>Invokes the ChatModeChanged event.</summary>
    protected virtual void OnChatModeChanged(ChatModeChangedEventArgs e)
    {
        try
        {
            ChatModeChanged?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking ChatModeChanged event handler.");
        }
    }

    /// <summary>Invokes the ReplyCountReceived event.</summary>
    protected virtual void OnReplyCountReceived(ReplyCountReceivedEventArgs e)
    {
        try
        {
            ReplyCountReceived?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking ReplyCountReceived event handler.");
        }
    }

    /// <summary>Invokes the ReactionsReceived event.</summary>
    protected virtual void OnReactionsReceived(ReactionsReceivedEventArgs e)
    {
        try
        {
            ReactionsReceived?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking ReactionsReceived event handler.");
        }
    }

    /// <summary>Invokes the EntityMutationReceived event.</summary>
    protected virtual void OnEntityMutationReceived(EntityMutationReceivedEventArgs e)
    {
        try
        {
            EntityMutationReceived?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking EntityMutationReceived event handler.");
        }
    }

    private void EmitActionEvents(List<Models.Response.Action>? actions)
    {
        if (actions is null)
            return;

        bool hasPoll = PollUpdated != null;
        bool hasPollClosed = PollClosed != null;
        bool hasDelete = ChatItemDeleted != null;
        bool hasDeleteByAuthor = ChatItemsDeletedByAuthor != null;
        bool hasBannerAdd = BannerAdded != null;
        bool hasBannerRemove = BannerRemoved != null;
        bool hasReplace = ChatItemReplaced != null;
        bool hasEngagement = EngagementMessageReceived != null;
        bool hasGift = GiftReceived != null;
        bool hasModeChange = ChatModeChanged != null;

        if (
            !hasPoll
            && !hasPollClosed
            && !hasDelete
            && !hasDeleteByAuthor
            && !hasBannerAdd
            && !hasBannerRemove
            && !hasReplace
            && !hasEngagement
            && !hasGift
            && !hasModeChange
        )
            return;

        foreach (Models.Response.Action action in actions)
        {
            // A poll's last update is kept until it closes, so the close carries the final results; the close
            // action itself names only the poll (observed 2026-11-17: its targetPanelId is the poll id).
            Contracts.Models.PollItem? poll = action.ToPollItem();
            if (poll is not null)
            {
                _openPolls[poll.PollId] = poll;
                if (hasPoll)
                    OnPollUpdated(new() { Poll = poll });
            }

            string? closedPollId = action.ToClosedPollId();
            if (closedPollId is not null)
            {
                _ = _openPolls.TryGetValue(closedPollId, out Contracts.Models.PollItem? finalPoll);
                _ = _openPolls.Remove(closedPollId);
                if (hasPollClosed)
                    OnPollClosed(new() { PollId = closedPollId, FinalPoll = finalPoll });
            }

            if (hasDelete)
            {
                string? targetId = action.ToDeletedItemId();
                if (targetId is not null)
                    OnChatItemDeleted(new() { TargetId = targetId });
            }

            if (hasDeleteByAuthor)
            {
                string? channelId = action.ToDeletedByAuthorChannelId();
                if (channelId is not null)
                    OnChatItemsDeletedByAuthor(new() { ChannelId = channelId });
            }

            if (hasBannerAdd)
            {
                Contracts.Models.BannerItem? banner = action.ToBannerItem(_timeProvider);
                if (banner is not null)
                    OnBannerAdded(new() { Banner = banner });
            }

            if (hasBannerRemove)
            {
                string? removedId = action.ToRemovedBannerActionId();
                if (removedId is not null)
                    OnBannerRemoved(new() { TargetActionId = removedId });
            }

            if (hasReplace)
            {
                (string? targetItemId, Contracts.Models.ChatItem? replacement) =
                    action.ToReplacedChatItem(_timeProvider);
                if (targetItemId is not null)
                    OnChatItemReplaced(
                        new() { TargetItemId = targetItemId, Replacement = replacement }
                    );
            }

            if (hasEngagement)
            {
                Contracts.Models.EngagementItem? engagement = action.ToEngagementItem(
                    _timeProvider
                );
                if (engagement is not null)
                    OnEngagementMessageReceived(new() { Engagement = engagement });
            }

            if (hasModeChange && action.ToChatModeChange(_timeProvider) is { } modeChange)
            {
                OnChatModeChanged(new() { Change = modeChange });
            }

            if (hasGift)
            {
                Contracts.Models.GiftItem? gift = action.ToGiftItem();
                if (gift is not null)
                    OnGiftReceived(new() { Gift = gift });
            }
        }
    }

    /// <summary>Invokes the PollUpdated event.</summary>
    protected virtual void OnPollUpdated(PollUpdatedEventArgs e)
    {
        try
        {
            PollUpdated?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error invoking PollUpdated event handler for poll {PollId}.",
                e.Poll?.PollId
            );
        }
    }

    /// <summary>Invokes the PollClosed event.</summary>
    protected virtual void OnPollClosed(PollClosedEventArgs e)
    {
        try
        {
            PollClosed?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error invoking PollClosed event handler for poll {PollId}.",
                e.PollId
            );
        }
    }

    /// <summary>Invokes the ChatItemDeleted event.</summary>
    protected virtual void OnChatItemDeleted(ChatItemDeletedEventArgs e)
    {
        try
        {
            ChatItemDeleted?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error invoking ChatItemDeleted event handler for target {TargetId}.",
                e.TargetId
            );
        }
    }

    /// <summary>Invokes the ChatItemsDeletedByAuthor event.</summary>
    protected virtual void OnChatItemsDeletedByAuthor(ChatItemsDeletedByAuthorEventArgs e)
    {
        try
        {
            ChatItemsDeletedByAuthor?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error invoking ChatItemsDeletedByAuthor event handler for channel {ChannelId}.",
                e.ChannelId
            );
        }
    }

    /// <summary>Invokes the BannerAdded event.</summary>
    protected virtual void OnBannerAdded(BannerAddedEventArgs e)
    {
        try
        {
            BannerAdded?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error invoking BannerAdded event handler for action {ActionId}.",
                e.Banner?.ActionId
            );
        }
    }

    /// <summary>Invokes the BannerRemoved event.</summary>
    protected virtual void OnBannerRemoved(BannerRemovedEventArgs e)
    {
        try
        {
            BannerRemoved?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error invoking BannerRemoved event handler for target {TargetActionId}.",
                e.TargetActionId
            );
        }
    }

    /// <summary>Invokes the ChatItemReplaced event.</summary>
    protected virtual void OnChatItemReplaced(ChatItemReplacedEventArgs e)
    {
        try
        {
            ChatItemReplaced?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error invoking ChatItemReplaced event handler for target {TargetItemId}.",
                e.TargetItemId
            );
        }
    }

    /// <summary>Invokes the EngagementMessageReceived event.</summary>
    protected virtual void OnEngagementMessageReceived(EngagementMessageReceivedEventArgs e)
    {
        try
        {
            EngagementMessageReceived?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error invoking EngagementMessageReceived event handler for engagement {Id}.",
                e.Engagement?.Id
            );
        }
    }

    /// <summary>Invokes the GiftReceived event.</summary>
    protected virtual void OnGiftReceived(GiftReceivedEventArgs e)
    {
        try
        {
            GiftReceived?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error invoking GiftReceived event handler for gift {Id}.",
                e.Gift?.Id
            );
        }
    }

    /// <summary>Invokes the CreatorGoalReceived event.</summary>
    protected virtual void OnCreatorGoalReceived(CreatorGoalReceivedEventArgs e)
    {
        try
        {
            CreatorGoalReceived?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error invoking CreatorGoalReceived event handler for goal {GoalId}.",
                e.CreatorGoal?.GoalId
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
            string? pageHtml = null;
            try
            {
                // Use the injected YTHttpClient instance
                pageHtml = await _ytHttpClient.GetOptionsAsync(
                    handle,
                    channelId,
                    liveId,
                    cancellationToken
                );
                FetchOptions newOptions = Parser.GetOptionsFromLivePage(pageHtml); // Can throw
                newOptions.IsBroadcasting = Parser.IsActivelyBroadcastingLivePage(pageHtml);
                if (
                    ShouldSkipAutoDetectedLiveStreamCandidate(
                        handle,
                        channelId,
                        liveId,
                        pageHtml,
                        newOptions.LiveId
                    )
                )
                {
                    return null;
                }

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
                string? inaccessibleKind = pageHtml is null
                    ? null
                    : Parser.DetectInaccessibleLiveReason(pageHtml);
                string inaccessibleReason = inaccessibleKind is null
                    ? string.Empty
                    : $"Live stream is inaccessible ({inaccessibleKind}).";
                if (
                    string.IsNullOrEmpty(inaccessibleReason)
                    && ex is InvalidOperationException ioe
                    && ioe.Message.Contains("Live stream is inaccessible", StringComparison.Ordinal)
                )
                {
                    inaccessibleReason = ioe.Message;
                }

                InvalidOperationException wrapped = new(
                    string.IsNullOrEmpty(inaccessibleReason)
                        ? $"Failed to initialize from YouTube page: {ex.Message}"
                        : inaccessibleReason,
                    ex
                );

                if (_continuousMonitorEnabledForSession && IsLikelyNoActiveLiveError(wrapped))
                {
                    _logger.LogDebug(
                        ex,
                        "No active livestream candidate found on this poll attempt. Monitor mode will keep waiting."
                    );
                    throw wrapped;
                }

                _logger.LogError(ex, "Failed to get or parse initial options.");
                // Wrap the exception to provide context before re-throwing
                throw wrapped;
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

    /// <inheritdoc />
    public async IAsyncEnumerable<ReplayChatItem> StreamChatReplayAsync(
        string liveId,
        TimeSpan? from = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(liveId))
            throw new ArgumentException("A video id must be provided.", nameof(liveId));

        string watchPage = await _ytHttpClient
            .GetOptionsAsync(null, null, liveId, cancellationToken)
            .ConfigureAwait(false);
        FetchOptions page =
            Parser.GetReplayOptions(watchPage)
            ?? throw new InvalidOperationException($"Video {liveId} has no chat replay.");
        string replayToken = page.Continuation;

        LiveChatResponse? first = Parser.ParseReplayPage(
            await _ytHttpClient
                .GetReplayPageAsync(replayToken, cancellationToken)
                .ConfigureAwait(false)
        );
        LiveChatContinuation? chat = first?.ContinuationContents?.LiveChatContinuation;
        if (chat is null)
            throw new InvalidOperationException($"The chat replay of {liveId} could not be read.");

        // As live, the replay page opens in Top chat; the second view of its header is every message.
        List<SubMenuItem>? views = chat.Header
            ?.LiveChatHeaderRenderer
            ?.ViewSelector
            ?.SortFilterSubMenuRenderer
            ?.SubMenuItems;
        string? continuation =
            _options.ChatView == ChatView.AllMessages && views is { Count: >= 2 }
                ? views[1].Continuation?.ReloadContinuationData?.Continuation
                : null;
        if (continuation is null)
        {
            foreach (ReplayChatItem item in ToReplayItems(chat.Actions, from))
                yield return item;
            continuation = NextReplayContinuation(chat);
        }

        long offsetMs = (long)(from?.TotalMilliseconds ?? 0);
        while (!string.IsNullOrEmpty(continuation))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string json = await _ytHttpClient
                .GetLiveChatReplayAsync(
                    page with
                    {
                        Continuation = continuation!,
                    },
                    offsetMs,
                    cancellationToken
                )
                .ConfigureAwait(false);
            chat = JsonSerializer
                .Deserialize(json, YTLiveChatJsonSerializerContext.Default.LiveChatResponse)
                ?.ContinuationContents?.LiveChatContinuation;
            if (chat is null)
                yield break;

            foreach (ReplayChatItem item in ToReplayItems(chat.Actions, from))
            {
                yield return item;
            }

            // The player position follows every replay entry, including the ones that carry nothing to report.
            offsetMs = Math.Max(offsetMs, LastReplayOffsetMs(chat.Actions));

            string? next = NextReplayContinuation(chat);
            if (next == continuation)
                yield break;
            continuation = next;
        }
    }

    private static long LastReplayOffsetMs(List<Models.Response.Action>? actions) =>
        actions
            ?.Select(static a =>
                long.TryParse(a.ReplayChatItemAction?.VideoOffsetTimeMsec, out long ms) ? ms : 0
            )
            .DefaultIfEmpty(0)
            .Max()
        ?? 0;

    private static string? NextReplayContinuation(LiveChatContinuation chat) =>
        chat
            .Continuations?.Select(static c => c.LiveChatReplayContinuationData?.Continuation)
            .FirstOrDefault(static c => !string.IsNullOrEmpty(c));

    private IEnumerable<ReplayChatItem> ToReplayItems(
        List<Models.Response.Action>? actions,
        TimeSpan? from
    )
    {
        foreach (Models.Response.Action action in actions ?? [])
        {
            ReplayChatItemAction? replay = action.ReplayChatItemAction;
            if (replay is null)
                continue;

            TimeSpan offset = TimeSpan.FromMilliseconds(
                long.TryParse(replay.VideoOffsetTimeMsec, out long ms) ? ms : 0
            );
            if (from is { } start && offset < start)
                continue;

            foreach (Models.Response.Action inner in replay.Actions ?? [])
            {
                if (inner.ToChatItem(_timeProvider) is { } chatItem)
                    yield return new ReplayChatItem { VideoOffset = offset, ChatItem = chatItem };
                else if (inner.ToGiftItem() is { } gift)
                    yield return new ReplayChatItem { VideoOffset = offset, Gift = gift };
            }
        }
    }

    /// <inheritdoc />
    public async Task<Contracts.Models.ChannelInfo?> GetChannelInfoAsync(
        string? handle = null,
        string? channelId = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(handle) && string.IsNullOrWhiteSpace(channelId))
            throw new ArgumentException("Either handle or channelId must be provided.");

        string html = await _ytHttpClient
            .GetChannelPageAsync(handle, channelId, cancellationToken)
            .ConfigureAwait(false);
        return Parser.ParseChannelPage(html);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Contracts.Models.StreamInfo>> GetStreamsAsync(
        string? handle = null,
        string? channelId = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(handle) && string.IsNullOrWhiteSpace(channelId))
            throw new ArgumentException("Either handle or channelId must be provided.");

        string html = await _ytHttpClient
            .GetStreamsPageAsync(handle, channelId, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<Contracts.Models.StreamInfo> streams = Parser.ExtractStreamsFromPage(html);
        _logger.LogInformation(
            "GetStreamsAsync [{Target}]: found {Count} stream(s)",
            handle ?? channelId,
            streams.Count
        );
        return streams;
    }

    /// <summary>Invokes the LivestreamInaccessible event.</summary>
    protected virtual void OnLivestreamInaccessible(LivestreamInaccessibleEventArgs e)
    {
        try
        {
            LivestreamInaccessible?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking LivestreamInaccessible event handler.");
        }
    }

    private async Task<string?> TrySelectCandidateFromStreamsPageAsync(
        string? handle,
        string? channelId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            string streamsHtml = await _ytHttpClient
                .GetStreamsPageAsync(handle, channelId, cancellationToken)
                .ConfigureAwait(false);
            IReadOnlyList<StreamPageCandidate> candidates =
                Parser.ExtractStreamCandidatesFromStreamsPage(streamsHtml);
            if (candidates.Count == 0)
            {
                int rawVideoIdMentions = CountOccurrences(
                    streamsHtml,
                    "\"videoId\"",
                    StringComparison.Ordinal
                );
                int escapedVideoIdMentions = CountOccurrences(
                    streamsHtml,
                    "\\\"videoId\\\"",
                    StringComparison.Ordinal
                );
                int initialDataMentions = CountOccurrences(
                    streamsHtml,
                    "ytInitialData",
                    StringComparison.Ordinal
                );
                bool isConsentInterstitial =
                    streamsHtml.IndexOf("consent.youtube.com", StringComparison.OrdinalIgnoreCase)
                    >= 0;
                _logger.LogInformation(
                    "Streams page parsing found 0 candidates for monitor target. Raw page contains {VideoIdMentions} '\"videoId\"' mentions, {EscapedVideoIdMentions} escaped mentions, ytInitialData mentions: {InitialDataMentions}, consent interstitial: {IsConsentInterstitial}.",
                    rawVideoIdMentions,
                    escapedVideoIdMentions,
                    initialDataMentions,
                    isConsentInterstitial
                );
                return null;
            }

            _logger.LogInformation(
                "Streams page parsing found {Count} candidates.",
                candidates.Count
            );

            PruneExpiredInaccessibleCandidateCooldowns();
            IEnumerable<StreamPageCandidate> filteredCandidates = candidates.Where(c =>
                !IsIgnoredAutoDetectedLiveId(c.LiveId)
                && !IsTemporarilyInaccessibleCandidate(c.LiveId)
            );
            List<StreamPageCandidate> filteredCandidateList = filteredCandidates.ToList();
            if (filteredCandidateList.Count == 0)
            {
                _logger.LogInformation(
                    "All streams candidates were filtered out by ignored-live-id settings or temporary inaccessible-live filters."
                );
                return null;
            }

            if (RequireActiveBroadcastForAutoDetectedStreamSetting)
            {
                foreach (StreamPageCandidate candidate in filteredCandidateList)
                {
                    if (!candidate.IsLive)
                    {
                        continue;
                    }

                    _logger.LogDebug(
                        "Selected LIVE candidate from streams page: {LiveId}",
                        candidate.LiveId
                    );
                    return candidate.LiveId;
                }

                _logger.LogInformation(
                    "No actively broadcasting candidate found on streams page (active-only mode)."
                );
                return null;
            }

            foreach (StreamPageCandidate candidate in filteredCandidateList)
            {
                if (!candidate.IsLive)
                {
                    continue;
                }

                _logger.LogDebug(
                    "Selected LIVE candidate from streams page: {LiveId}",
                    candidate.LiveId
                );
                return candidate.LiveId;
            }

            StreamPageCandidate? upcomingCandidate = null;
            foreach (
                StreamPageCandidate candidate in filteredCandidateList.Where(c => c.IsUpcoming)
            )
            {
                if (
                    !upcomingCandidate.HasValue
                    || (candidate.UpcomingStartTime ?? long.MaxValue)
                        < (upcomingCandidate.Value.UpcomingStartTime ?? long.MaxValue)
                )
                {
                    upcomingCandidate = candidate;
                }
            }

            if (!upcomingCandidate.HasValue)
            {
                _logger.LogInformation(
                    "No LIVE or UPCOMING candidate found after filtering streams candidates."
                );
                return null;
            }

            _logger.LogDebug(
                "Selected UPCOMING candidate from streams page: {LiveId} (Start: {Start})",
                upcomingCandidate.Value.LiveId,
                upcomingCandidate.Value.UpcomingStartTime
            );
            return upcomingCandidate.Value.LiveId;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(
                ex,
                "Failed to parse streams page candidates. Falling back to /live page parsing."
            );
            return null;
        }
    }

    private bool ShouldSkipAutoDetectedLiveStreamCandidate(
        string? handle,
        string? channelId,
        string? explicitLiveId,
        string pageHtml,
        string? candidateLiveId
    )
    {
        if (!string.IsNullOrWhiteSpace(explicitLiveId))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(handle) && string.IsNullOrWhiteSpace(channelId))
        {
            return false;
        }

        string? candidateId = candidateLiveId;
        if (candidateId is { Length: > 0 } && IsIgnoredAutoDetectedLiveId(candidateId))
        {
            _logger.LogInformation(
                "Skipping auto-detected livestream {LiveId} because it is configured in IgnoredAutoDetectedLiveIds.",
                candidateId
            );
            return true;
        }

        if (
            RequireActiveBroadcastForAutoDetectedStreamSetting
            && !Parser.IsActivelyBroadcastingLivePage(pageHtml)
        )
        {
            _logger.LogInformation(
                "Skipping auto-detected livestream candidate because page is not actively broadcasting (scheduled/free-chat/offline)."
            );
            return true;
        }

        return false;
    }

    private bool IsIgnoredAutoDetectedLiveId(string liveId)
    {
#pragma warning disable CS0618
        if (_options.IgnoredAutoDetectedLiveIds.Count == 0)
        {
            return false;
        }

        foreach (string ignoredLiveId in _options.IgnoredAutoDetectedLiveIds)
        {
            if (string.Equals(ignoredLiveId?.Trim(), liveId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
#pragma warning restore CS0618

        return false;
    }

    private static int CountOccurrences(string value, string token, StringComparison comparison)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(token))
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while (true)
        {
            int found = value.IndexOf(token, index, comparison);
            if (found < 0)
            {
                break;
            }

            count++;
            index = found + token.Length;
        }

        return count;
    }

    private TimeSpan GetInaccessibleCandidateRetryDelay()
    {
        TimeSpan fromFrequency = TimeSpan.FromMilliseconds(
            Math.Max(LiveCheckFrequencySetting * 5, 0)
        );
        return fromFrequency > s_inaccessibleCandidateRetryFloor
            ? fromFrequency
            : s_inaccessibleCandidateRetryFloor;
    }

    private bool IsTemporarilyInaccessibleCandidate(string liveId)
    {
        if (
            !_temporaryInaccessibleAutoDetectedLiveIds.TryGetValue(
                liveId,
                out DateTimeOffset retryAt
            )
        )
        {
            return false;
        }

        if (_timeProvider.GetUtcNow() < retryAt)
        {
            return true;
        }

        _ = _temporaryInaccessibleAutoDetectedLiveIds.Remove(liveId);
        return false;
    }

    private void PruneExpiredInaccessibleCandidateCooldowns()
    {
        if (_temporaryInaccessibleAutoDetectedLiveIds.Count == 0)
        {
            return;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        List<string> expiredKeys = [];
        foreach (
            KeyValuePair<string, DateTimeOffset> entry in _temporaryInaccessibleAutoDetectedLiveIds
        )
        {
            string key = entry.Key;
            DateTimeOffset retryAt = entry.Value;
            if (retryAt <= now)
            {
                expiredKeys.Add(key);
            }
        }

        foreach (string key in expiredKeys)
        {
            _ = _temporaryInaccessibleAutoDetectedLiveIds.Remove(key);
        }
    }
}
