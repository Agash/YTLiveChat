using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YTLiveChat.Contracts.Models;
using YTLiveChat.Contracts.Services;

namespace YTLiveChat.Example;

/// <summary>
/// A background service that monitors a YouTube live chat using IYTLiveChat.
/// </summary>
internal class ChatMonitorService : IHostedService, IDisposable
{
    private readonly ILogger<ChatMonitorService> _logger;
    private readonly IYTLiveChat _ytLiveChat;
    private readonly ExampleRunOptions _options;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly CancellationTokenSource _stoppingCts = new();

    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    public ChatMonitorService(
        ILogger<ChatMonitorService> logger,
        IYTLiveChat ytLiveChat,
        ExampleRunOptions options, // Inject options containing Live ID
        IHostApplicationLifetime appLifetime
    )
    {
        _logger = logger;
        _ytLiveChat = ytLiveChat;
        _options = options;
        _appLifetime = appLifetime;

        // Subscribe to application stopping event to gracefully stop the chat listener
        appLifetime.ApplicationStopping.Register(OnApplicationStopping);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Chat Monitor Service starting for Live ID: {LiveId}",
            _options.LiveId
        );

        // Subscribe to chat events
        _ytLiveChat.InitialPageLoaded += OnInitialPageLoaded;
        _ytLiveChat.ChatReceived += OnChatReceived;
        _ytLiveChat.ChatStopped += OnChatStopped;
        _ytLiveChat.ErrorOccurred += OnErrorOccurred;

        try
        {
            // Start listening - provide only the Live ID
            _ytLiveChat.Start(liveId: _options.LiveId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to start YTLiveChat service for Live ID {LiveId}",
                _options.LiveId
            );
            // Signal the application to stop if startup fails critically
            _appLifetime.StopApplication();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Chat Monitor Service stopping.");

        // Unsubscribe from events
        _ytLiveChat.InitialPageLoaded -= OnInitialPageLoaded;
        _ytLiveChat.ChatReceived -= OnChatReceived;
        _ytLiveChat.ChatStopped -= OnChatStopped;
        _ytLiveChat.ErrorOccurred -= OnErrorOccurred;

        // Stop the chat listener
        _ytLiveChat.Stop();

        // Signal that the service is stopped
        _stoppingCts.Cancel();

        return Task.CompletedTask;
    }

    private void OnInitialPageLoaded(object? sender, InitialPageLoadedEventArgs e)
    {
        _logger.LogInformation(
            "🎉 Successfully loaded initial chat data for Live ID: {LiveId}",
            e.LiveId
        );
        Console.WriteLine($"--- Chat monitoring started for Live ID: {e.LiveId} ---");
    }

    private void OnChatReceived(object? sender, ChatReceivedEventArgs e)
    {
        ChatItem item = e.ChatItem;

        // Simple output for regular text messages
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
                Console.ForegroundColor = ChatMonitorService.GetEventColor(item); // Color-code special events
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
#if NETSTANDARD2_1
            if (httpEx.Message.Contains("403") || httpEx.Message.Contains("Forbidden"))
            {
                _logger.LogCritical("Stopping application due to Forbidden HTTP error.");
                _appLifetime.StopApplication();
            }
#else
            if (httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogCritical("Stopping application due to Forbidden HTTP error.");
                _appLifetime.StopApplication();
            }
#endif
        }
        // You might add more complex logic here based on exception types or retry counts
    }

    private void OnApplicationStopping()
    {
        _logger.LogInformation(
            "Application stopping event received. Initiating graceful shutdown of chat monitor..."
        );
        // No need to call StopAsync here, the host does that.
        // Just ensure any long-running operations respect the _stoppingCts if needed.
    }

    public void Dispose()
    {
        _logger.LogDebug("Disposing ChatMonitorService.");
        // Dispose the CancellationTokenSource used for stopping signal
        _stoppingCts.Dispose();
        // The IYTLiveChat instance is managed by DI, so we don't dispose it here.
        // The host will dispose DI-managed services.
    }
}

/// <summary>
/// Simple class to hold runtime options, like the Live ID.
/// </summary>
internal class ExampleRunOptions
{
    public string LiveId { get; set; } = string.Empty;
}
