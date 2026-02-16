using System.Text.Json;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using YTLiveChat.Contracts.Models;
using YTLiveChat.Contracts.Services;

namespace YTLiveChat.Example;

internal class ChatMonitorService : IHostedService, IDisposable
{
    private readonly ILogger<ChatMonitorService> _logger;
    private readonly IYTLiveChat _ytLiveChat;
    private readonly ExampleRunOptions _options; // This already uses the updated class
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly CancellationTokenSource _stoppingCts = new();
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    public ChatMonitorService(
        ILogger<ChatMonitorService> logger,
        IYTLiveChat ytLiveChat,
        ExampleRunOptions options, // Injection uses the updated class
        IHostApplicationLifetime appLifetime
    )
    {
        _logger = logger;
        _ytLiveChat = ytLiveChat;
        _options = options;
        _appLifetime = appLifetime;
        _ = appLifetime.ApplicationStopping.Register(OnApplicationStopping);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Log the identifier being used
        string identifierInfo = !string.IsNullOrEmpty(_options.Handle)
            ? $"Handle: {_options.Handle}"
            : $"Live ID: {_options.LiveId}";
        _logger.LogInformation(
            "Chat Monitor Service starting for {IdentifierInfo}",
            identifierInfo
        );

        // Subscribe to events
        _ytLiveChat.InitialPageLoaded += OnInitialPageLoaded;
        _ytLiveChat.ChatReceived += OnChatReceived;
        _ytLiveChat.ChatStopped += OnChatStopped;
        _ytLiveChat.ErrorOccurred += OnErrorOccurred;

        try
        {
            // Call Start with the correct parameter based on options
            if (!string.IsNullOrEmpty(_options.Handle))
            {
                _logger.LogDebug("Starting YTLiveChat with Handle: {Handle}", _options.Handle);
                _ytLiveChat.Start(handle: _options.Handle);
            }
            else if (!string.IsNullOrEmpty(_options.LiveId))
            {
                _logger.LogDebug("Starting YTLiveChat with Live ID: {LiveId}", _options.LiveId);
                _ytLiveChat.Start(liveId: _options.LiveId);
            }
            else
            {
                // This case should technically not be reachable due to Program.cs validation
                _logger.LogError(
                    "Cannot start chat monitor: No valid Live ID or Handle was provided in options."
                );
                _appLifetime.StopApplication(); // Stop if configuration is invalid
                return Task.CompletedTask;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to start YTLiveChat service for {IdentifierInfo}",
                identifierInfo
            );
            _appLifetime.StopApplication(); // Stop the application if start fails
        }

        return Task.CompletedTask;
    }

    // StopAsync, OnInitialPageLoaded, OnChatReceived, GetEventColor,
    // OnChatStopped, OnErrorOccurred, OnApplicationStopping, Dispose methods remain the same...
    // ... (keep the rest of the ChatMonitorService class as it was) ...

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Chat Monitor Service stopping.");
        _ytLiveChat.InitialPageLoaded -= OnInitialPageLoaded;
        _ytLiveChat.ChatReceived -= OnChatReceived;
        _ytLiveChat.ChatStopped -= OnChatStopped;
        _ytLiveChat.ErrorOccurred -= OnErrorOccurred;
        _ytLiveChat.Stop();
        _stoppingCts.Cancel();
        return Task.CompletedTask;
    }

    private void OnInitialPageLoaded(object? sender, InitialPageLoadedEventArgs e)
    {
        // This event correctly returns the resolved LiveId even if started by Handle
        _logger.LogInformation(
            "🎉 Successfully loaded initial chat data for Live ID: {LiveId} (resolved from input)",
            e.LiveId
        );
        Console.WriteLine($"--- Chat monitoring started for Live ID: {e.LiveId} ---");
    }

    private void OnChatReceived(object? sender, ChatReceivedEventArgs e)
    {
        ChatItem item = e.ChatItem;
        if (
            item.Superchat == null
            && item.MembershipDetails == null
            && item.Message.Length > 0
            && item.Message.All(p => p is TextPart)
        )
        {
            string messageText = string.Join("", item.Message.Select(p => p.ToString()));
            string authorType =
                item.IsOwner ? "[OWNER]"
                : item.IsModerator ? "[MOD]"
                : item.IsVerified ? "[VERIFIED]"
                : item.IsMembership ? "[MEMBER]"
                : "";
            Console.WriteLine(
                $"[{item.Timestamp:HH:mm:ss}] {authorType}{item.Author.Name}: {messageText}"
            );
        }
        // Output full JSON for special events (Superchats, Memberships, complex messages)
        else
        {
            try
            {
                string json = JsonSerializer.Serialize(item, s_jsonOptions);
                Console.ForegroundColor = GetEventColor(item); // Color-code special events
                Console.WriteLine($"[{item.Timestamp:HH:mm:ss}] --- SPECIAL EVENT ---");
                Console.WriteLine(json);
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serialize ChatItem to JSON. ID: {ItemId}", item.Id);
                Console.WriteLine(
                    $"[{item.Timestamp:HH:mm:ss}] !!! Failed to display event: {item.Id} !!!"
                );
            }
        }
    }

    private static ConsoleColor GetEventColor(ChatItem item)
    {
        if (item.Superchat != null)
            return ConsoleColor.Yellow;
        if (item.MembershipDetails != null)
        {
            return item.MembershipDetails.EventType switch
            {
                MembershipEventType.New => ConsoleColor.Green,
                MembershipEventType.Milestone => ConsoleColor.Cyan,
                MembershipEventType.GiftPurchase => ConsoleColor.Magenta,
                MembershipEventType.GiftRedemption => ConsoleColor.Blue,
                _ => ConsoleColor.Gray,
            };
        }

        return ConsoleColor.DarkGray; // Default for other complex messages
    }

    private void OnChatStopped(object? sender, ChatStoppedEventArgs e)
    {
        _logger.LogWarning(
            "⏹️ Chat listener reported stopped. Reason: {Reason}",
            e.Reason ?? "Unknown"
        );
        Console.WriteLine($"--- Chat monitoring stopped. Reason: {e.Reason ?? "Unknown"} ---");
        // Signal the application host to exit gracefully
        _appLifetime.StopApplication();
    }

    private void OnErrorOccurred(object? sender, ErrorOccurredEventArgs e)
    {
        Exception ex = e.GetException();
        _logger.LogError(ex, "😱 An error occurred in the chat listener!");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"--- ERROR OCCURRED ---");
        Console.WriteLine(ex.ToString()); // Print full exception details
        Console.ResetColor();
        // Optionally, decide if the error is fatal and stop the application
        // For example, stop on persistent Forbidden errors
        if (ex is HttpRequestException httpEx)
        {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            if (httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogCritical("Stopping application due to Forbidden HTTP error.");
                _appLifetime.StopApplication();
            }
#else // Fallback for netstandard2.0 or if StatusCode is null
            if (httpEx.Message.Contains("403") || httpEx.Message.Contains("Forbidden"))
            {
                _logger.LogCritical(
                    "Stopping application due to Forbidden HTTP error (detected via message)."
                );
                _appLifetime.StopApplication();
            }
#endif
        }
        // Added: Stop if initialization fails specifically
        else if (ex is InvalidOperationException && ex.Message.StartsWith("Failed to initialize"))
        {
            _logger.LogCritical("Stopping application due to initialization failure.");
            _appLifetime.StopApplication();
        }
    }

    private void OnApplicationStopping()
    {
        _logger.LogInformation(
            "Application stopping event received. Initiating graceful shutdown of chat monitor..."
        );
        // Consider calling Stop() here as well, although StopAsync should handle it
        // _ytLiveChat.Stop();
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
            _logger.LogDebug("Disposing ChatMonitorService.");
            // Unsubscribe events here is good practice, although StopAsync does it too.
            // Helps prevent issues if StopAsync isn't called or completes partially.
            _ytLiveChat.InitialPageLoaded -= OnInitialPageLoaded;
            _ytLiveChat.ChatReceived -= OnChatReceived;
            _ytLiveChat.ChatStopped -= OnChatStopped;
            _ytLiveChat.ErrorOccurred -= OnErrorOccurred;

            // Call Stop explicitly if not already stopped (idempotent)
            _ytLiveChat.Stop();

            // Dispose the CancellationTokenSource
            _stoppingCts.Dispose();
        }
    }
}

/// <summary>
/// Simple class to hold runtime options, like the Live ID.
/// </summary>
internal class ExampleRunOptions
{
    public string? LiveId { get; set; } // Keep nullable for clarity
    public string? Handle { get; set; } // Add nullable Handle property
}
