using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Services;
using YTLiveChat.Helpers;
using YTLiveChat.Models;
using YTLiveChat.Models.Response;

using Action = YTLiveChat.Models.Response.Action;

namespace YTLiveChat.Services;

internal class YTLiveChat : IYTLiveChat
{
    // --- Events ---
    public event EventHandler<InitialPageLoadedEventArgs>? InitialPageLoaded;
    public event EventHandler<ChatStoppedEventArgs>? ChatStopped;
    public event EventHandler<ChatReceivedEventArgs>? ChatReceived;
    public event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;

    // --- Dependencies & Options ---
    private readonly YTHttpClientFactory _httpClientFactory;
    private readonly IOptions<YTLiveChatOptions> _options;
    private readonly ILogger<YTLiveChat> _logger;

    // --- State ---
    private FetchOptions? _fetchOptions;
    private CancellationTokenSource? _cancellationTokenSource;

    // --- Retry Logic Configuration ---
    private const int MaxRetryAttempts = 5; // Max number of retries before giving up
    private const double BaseRetryDelaySeconds = 1.0; // Initial delay in seconds
    private const double MaxRetryDelaySeconds = 30.0; // Maximum delay capped

    // --- Debug Logging Fields ---
    private static readonly SemaphoreSlim s_debugLogLock = new(1, 1);
    private readonly bool _isDebugLoggingEnabled;
    private readonly string _debugLogFilePath;
    private static readonly JsonSerializerOptions s_debugJsonOptions = new() { WriteIndented = false };

    public YTLiveChat(
        IOptions<YTLiveChatOptions> options,
        YTHttpClientFactory httpClientFactory,
        ILogger<YTLiveChat> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        // --- Initialize Debug Logging ---
        _isDebugLoggingEnabled = _options.Value.DebugLogReceivedJsonItems;
        _debugLogFilePath = _options.Value.DebugLogFilePath
                            ?? Path.Combine(AppContext.BaseDirectory, "ytlivechat_debug_items.jsonl");

        // Override in DEBUG builds for easier development
#if DEBUG
        _logger.LogDebug("Debug build detected, enabling JSON item logging.");
        _isDebugLoggingEnabled = true;
#endif
        if (_isDebugLoggingEnabled)
        {
            _logger.LogInformation("JSON Item logging enabled. Output Path: {FilePath}", _debugLogFilePath);
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
                _logger.LogInformation("Created directory for debug log: {DirectoryPath}", directory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring debug log directory exists ('{FilePath}'). Debug logging might fail.", _debugLogFilePath);
        }
    }

    public void Start(
        string? handle = null,
        string? channelId = null,
        string? liveId = null,
        bool overwrite = false)
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            _logger.LogWarning("Start called but listener is already running.");
            if (!overwrite)
            {
                return;
            }

            _logger.LogInformation("Overwrite requested, stopping previous instance...");
            Stop(); // Stop existing task before restarting
        }

        _logger.LogInformation("Attempting to start listener...");
        _cancellationTokenSource = new CancellationTokenSource();
        CancellationToken token = _cancellationTokenSource.Token;

        // Run the main loop in a background thread to avoid blocking the caller
        _ = Task.Run(async () => await StartAsync(handle, channelId, liveId, overwrite, token), token)
              .ContinueWith(task => // Log unhandled exceptions from the background task
              {
                  if (task.IsFaulted && task.Exception != null)
                  {
                      _logger.LogError(task.Exception, "Unhandled exception in background task.");

                      OnErrorOccurred(new ErrorOccurredEventArgs(task.Exception.Flatten().InnerException ?? task.Exception));
                      OnChatStopped(new() { Reason = "Background task faulted." });
                  }
              }, TaskScheduler.Default);
    }

    private async Task StartAsync(
        string? handle,
        string? channelId,
        string? liveId,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listener task started.");
        int currentRetryAttempt = 0;

        try
        {
            // --- Initialization ---
            _fetchOptions = await GetOptionsAsync(handle, channelId, liveId, overwrite, cancellationToken);
            if (_fetchOptions?.LiveId is null)
            {
                InvalidOperationException initException = new("Failed to retrieve valid initial FetchOptions (LiveId is missing).");
                OnErrorOccurred(new ErrorOccurredEventArgs(initException));
                OnChatStopped(new() { Reason = "Failed to get initial options" });
                _logger.LogError("Initialization failed: Could not retrieve valid FetchOptions.");
                return; // Stop execution if initialization fails
            }

            OnInitialPageLoaded(new() { LiveId = _fetchOptions.LiveId });
            _logger.LogInformation("Initial page loaded for Live ID: {LiveId}. Starting polling...", _fetchOptions.LiveId);

            // --- Polling Loop ---
            using YTHttpClient httpClient = _httpClientFactory.Create();
            using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(_options.Value.RequestFrequency));

            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait for the next tick. This handles cancellation internally.
                if (!await timer.WaitForNextTickAsync(cancellationToken))
                {
                    break; // Timer stopped or task cancelled
                }

                if (_fetchOptions?.Continuation is null)
                {
                    _logger.LogError("Error: Fetch options or continuation token missing. Stopping poll.");
                    OnErrorOccurred(new ErrorOccurredEventArgs(new InvalidOperationException("Continuation token lost.")));
                    OnChatStopped(new() { Reason = "Continuation token missing" });
                    break; // Exit loop if continuation is lost
                }

                try
                {
                    // Fetch data using the current options
                    (LiveChatResponse? response, string? rawJson) =
                        await httpClient.GetLiveChatAsync(_fetchOptions, cancellationToken);

                    // --- Log Raw JSON Item Actions if Enabled ---
                    if (_isDebugLoggingEnabled && response?.ContinuationContents?.LiveChatContinuation?.Actions != null)
                    {
                        await LogRawJsonActionsAsync(response.ContinuationContents.LiveChatContinuation.Actions, cancellationToken);
                    }

                    if (response != null)
                    {
                        // Parse the response
                        (List<Contracts.Models.ChatItem> items, string? continuation) =
                            Parser.ParseLiveChatResponse(response);

                        // Dispatch received items
                        foreach (Contracts.Models.ChatItem item in items)
                        {
                            OnChatReceived(new() { ChatItem = item });
                        }

                        // Update continuation token or handle stream end
                        if (!string.IsNullOrEmpty(continuation))
                        {
                            _fetchOptions = _fetchOptions with { Continuation = continuation };
                            // Reset retry count on successful poll
                            currentRetryAttempt = 0;
                        }
                        else
                        {
                            _logger.LogInformation("No continuation token received in response. Assuming stream ended or chat disabled.");
                            OnChatStopped(new() { Reason = "Stream ended or continuation lost" });
                            break; // Exit loop
                        }
                    }
                    else
                    {
                        // Handle null response
                        // Trigger generic error and attempt retry with backoff
                        throw new Exception("Failed to fetch or deserialize live chat data (null response received).");
                    }
                }
                catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    // Specific non-retryable HTTP error
                    _logger.LogError(httpEx, "Received Forbidden (403). Stream might be region locked, require login, or API access changed. Stopping.");
                    OnErrorOccurred(new ErrorOccurredEventArgs(httpEx));
                    OnChatStopped(new() { Reason = "Received Forbidden (403)" });
                    break; // Stop polling
                }
                catch (Exception ex) when (ex is not OperationCanceledException) // Catch retryable exceptions
                {
                    OnErrorOccurred(new ErrorOccurredEventArgs(ex));
                    currentRetryAttempt++;

                    if (currentRetryAttempt > MaxRetryAttempts)
                    {
                        _logger.LogError(ex, "Maximum retry attempts ({MaxAttempts}) exceeded. Stopping poll.", MaxRetryAttempts);
                        OnChatStopped(new() { Reason = $"Failed after {MaxRetryAttempts} retries: {ex.Message}" });
                        break; // Give up after max retries
                    }

                    // Calculate exponential backoff delay
                    double delaySeconds = BaseRetryDelaySeconds * Math.Pow(2, currentRetryAttempt - 1);
                    delaySeconds = Math.Min(delaySeconds, MaxRetryDelaySeconds); // Cap delay
                    // Add some jitter (e.g., +/- 20%)
                    delaySeconds *= 1.0 + ((Random.Shared.NextDouble() * 0.4) - 0.2);
                    TimeSpan delay = TimeSpan.FromSeconds(delaySeconds);

                    _logger.LogWarning(ex, "Error during poll cycle (Attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}...",
                        currentRetryAttempt, MaxRetryAttempts, delay);

                    await Task.Delay(delay, cancellationToken); // Wait before retrying
                }
                // OperationCanceledException is caught by the outer try-catch
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Listener task cancellation requested.");
        }
        catch (Exception ex) // Catch errors during initialization or unhandled loop errors
        {
            _logger.LogError(ex, "Critical error during listener task execution.");
            OnErrorOccurred(new ErrorOccurredEventArgs(ex));
            OnChatStopped(new() { Reason = $"Critical error: {ex.Message}" });
        }
        finally
        {
            // Ensure ChatStopped is raised if the task ends and wasn't already raised
            if (cancellationToken.IsCancellationRequested)
            {
                OnChatStopped(new() { Reason = "Operation Cancelled" });
            }

            _logger.LogInformation("Listener task finished.");
        }
    }

    private async Task LogRawJsonActionsAsync(List<Action> actions, CancellationToken cancellationToken)
    {
        if (!_isDebugLoggingEnabled || actions == null || actions.Count == 0)
            return;

        // Filter only AddChatItem actions for logging item details
        List<AddChatItemActionItem> itemsToLog = [.. actions
            .Select(a => a.AddChatItemAction?.Item)
            .WhereNotNull()];

        if (itemsToLog.Count == 0)
            return;

        await s_debugLogLock.WaitAsync(cancellationToken);
        try
        {
            await using StreamWriter writer = new(_debugLogFilePath, append: true, System.Text.Encoding.UTF8);
            foreach (AddChatItemActionItem? item in itemsToLog)
            {
                if (cancellationToken.IsCancellationRequested) break;
                string jsonLine = JsonSerializer.Serialize(item, s_debugJsonOptions);
                await writer.WriteLineAsync(jsonLine.AsMemory(), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Debug logging cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing raw JSON actions to log file '{FilePath}'", _debugLogFilePath);
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
            _logger.LogInformation("Stop requested.");
            try
            {
                _cancellationTokenSource.Cancel();
                // ChatStopped event will be raised by the StartAsync task's finally block upon cancellation.
            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning("Attempted to cancel a disposed CancellationTokenSource during stop.");
            }
        }
        else
        {
            _logger.LogDebug("Stop called but listener was not running or already stopping.");
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
            _logger.LogDebug("Disposing YTLiveChat service.");
            Stop(); // Ensure cancellation is requested and logged if needed
            _cancellationTokenSource?.Dispose();
            s_debugLogLock.Dispose(); // Dispose semaphore
        }
    }

    // --- Event Invokers ---
    protected virtual void OnInitialPageLoaded(InitialPageLoadedEventArgs e)
    {
        try { InitialPageLoaded?.Invoke(this, e); }
        catch (Exception ex) { _logger.LogError(ex, "Error invoking InitialPageLoaded event handler."); }
    }

    protected virtual void OnChatStopped(ChatStoppedEventArgs e)
    {
        try { ChatStopped?.Invoke(this, e); }
        catch (Exception ex) { _logger.LogError(ex, "Error invoking ChatStopped event handler."); }
    }

    protected virtual void OnChatReceived(ChatReceivedEventArgs e)
    {
        try { ChatReceived?.Invoke(this, e); }
        catch (Exception ex) { _logger.LogError(ex, "Error invoking ChatReceived event handler for item ID {ItemId}.", e.ChatItem?.Id); }
    }

    protected virtual void OnErrorOccurred(ErrorOccurredEventArgs e)
    {
        try { ErrorOccurred?.Invoke(this, e); }
        catch (Exception ex) { _logger.LogError(ex, "Error invoking ErrorOccurred event handler."); }
    }

    // --- Helper for Initial Options ---
    private async Task<FetchOptions?> GetOptionsAsync(
        string? handle,
        string? channelId,
        string? liveId,
        bool overwrite,
        CancellationToken cancellationToken = default)
    {
        if (_fetchOptions == null || overwrite)
        {
            _logger.LogInformation("Fetching initial options (Overwrite={Overwrite})...", overwrite);
            try
            {
                using YTHttpClient httpClient = _httpClientFactory.Create();
                string pageHtml = await httpClient.GetOptionsAsync(handle, channelId, liveId, cancellationToken);
                // Parser might throw if critical info is missing
                FetchOptions newOptions = Parser.GetOptionsFromLivePage(pageHtml);
                _logger.LogInformation("Successfully parsed initial options. Live ID: {LiveId}", newOptions.LiveId);
                _fetchOptions = newOptions;
            }
            catch (OperationCanceledException ocEx)
            {
                _logger.LogWarning(ocEx, "Initial options fetch cancelled.");
                _fetchOptions = null; // Ensure null on cancellation during fetch
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get or parse initial options.");
                _fetchOptions = null; // Ensure it's null on failure
                // Error event will be raised by the caller (StartAsync)
                throw new InvalidOperationException($"Failed to initialize from YouTube page: {ex.Message}", ex);
            }
        }
        else
        {
            _logger.LogDebug("Using cached initial options. Live ID: {LiveId}", _fetchOptions.LiveId);
        }

        return _fetchOptions;
    }
}