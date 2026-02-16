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
    private readonly ExampleRunOptions _options;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly CancellationTokenSource _stoppingCts = new();

    private static readonly object s_consoleLock = new();

    public ChatMonitorService(
        ILogger<ChatMonitorService> logger,
        IYTLiveChat ytLiveChat,
        ExampleRunOptions options,
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
        string identifierInfo = !string.IsNullOrEmpty(_options.Handle)
            ? $"Handle: {_options.Handle}"
            : $"Live ID: {_options.LiveId}";
        _logger.LogInformation("Chat Monitor Service starting for {IdentifierInfo}", identifierInfo);

        _ytLiveChat.InitialPageLoaded += OnInitialPageLoaded;
        _ytLiveChat.ChatReceived += OnChatReceived;
        _ytLiveChat.RawActionReceived += OnRawActionReceived;
        _ytLiveChat.ChatStopped += OnChatStopped;
        _ytLiveChat.ErrorOccurred += OnErrorOccurred;

        try
        {
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
                _logger.LogError("Cannot start chat monitor: No valid Live ID or Handle was provided in options.");
                _appLifetime.StopApplication();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start YTLiveChat service for {IdentifierInfo}", identifierInfo);
            _appLifetime.StopApplication();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Chat Monitor Service stopping.");
        _ytLiveChat.InitialPageLoaded -= OnInitialPageLoaded;
        _ytLiveChat.ChatReceived -= OnChatReceived;
        _ytLiveChat.RawActionReceived -= OnRawActionReceived;
        _ytLiveChat.ChatStopped -= OnChatStopped;
        _ytLiveChat.ErrorOccurred -= OnErrorOccurred;
        _ytLiveChat.Stop();
        _stoppingCts.Cancel();
        return Task.CompletedTask;
    }

    private void OnInitialPageLoaded(object? sender, InitialPageLoadedEventArgs e)
    {
        _logger.LogInformation("Successfully loaded initial chat data for Live ID: {LiveId}", e.LiveId);

        lock (s_consoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("LIVE ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("-> ");
            Console.ResetColor();
            Console.WriteLine(e.LiveId);
        }
    }

    private void OnChatReceived(object? sender, ChatReceivedEventArgs e)
    {
        RenderChatItem(e.ChatItem);
    }

    private void OnRawActionReceived(object? sender, RawActionReceivedEventArgs e)
    {
        if (e.ParsedChatItem != null)
        {
            return;
        }

        string actionKind = "unknownAction";
        if (e.RawAction.ValueKind == JsonValueKind.Object)
        {
            using JsonElement.ObjectEnumerator enumerator = e.RawAction.EnumerateObject();
            if (enumerator.MoveNext())
            {
                actionKind = enumerator.Current.Name;
            }
        }

        lock (s_consoleLock)
        {
            WriteTimestamp(DateTimeOffset.UtcNow);
            WriteTag("RAW", ConsoleColor.DarkGray);
            Console.Write(' ');
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(actionKind);
            Console.ResetColor();
            Console.WriteLine();
        }
    }

    private void OnChatStopped(object? sender, ChatStoppedEventArgs e)
    {
        _logger.LogWarning("Chat listener reported stopped. Reason: {Reason}", e.Reason ?? "Unknown");

        lock (s_consoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("STOP ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("-> ");
            Console.ResetColor();
            Console.WriteLine(e.Reason ?? "Unknown");
        }

        _appLifetime.StopApplication();
    }

    private void OnErrorOccurred(object? sender, ErrorOccurredEventArgs e)
    {
        Exception ex = e.GetException();
        _logger.LogError(ex, "An error occurred in the chat listener.");

        lock (s_consoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("ERROR ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("-> ");
            Console.ResetColor();
            Console.WriteLine(ex.Message);
        }

        if (ex is HttpRequestException httpEx)
        {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            if (httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogCritical("Stopping application due to Forbidden HTTP error.");
                _appLifetime.StopApplication();
            }
#else
            if (httpEx.Message.Contains("403") || httpEx.Message.Contains("Forbidden"))
            {
                _logger.LogCritical(
                    "Stopping application due to Forbidden HTTP error (detected via message)."
                );
                _appLifetime.StopApplication();
            }
#endif
        }
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
            _ytLiveChat.InitialPageLoaded -= OnInitialPageLoaded;
            _ytLiveChat.ChatReceived -= OnChatReceived;
            _ytLiveChat.RawActionReceived -= OnRawActionReceived;
            _ytLiveChat.ChatStopped -= OnChatStopped;
            _ytLiveChat.ErrorOccurred -= OnErrorOccurred;

            _ytLiveChat.Stop();
            _stoppingCts.Dispose();
        }
    }

    private static void RenderChatItem(ChatItem item)
    {
        lock (s_consoleLock)
        {
            WriteTimestamp(item.Timestamp);

            if (item.ViewerLeaderboardRank is int rank)
            {
                WriteTag($"#{rank}", ConsoleColor.DarkYellow);
            }

            if (item.IsOwner)
            {
                WriteTag("OWNER", ConsoleColor.Red);
            }
            else
            {
                if (item.IsModerator)
                {
                    WriteTag("MOD", ConsoleColor.Blue);
                }

                if (item.IsVerified)
                {
                    WriteTag("VERIFIED", ConsoleColor.Cyan);
                }

                if (item.IsMembership)
                {
                    WriteTag("MEMBER", ConsoleColor.Green);
                }
            }

            if (!string.IsNullOrWhiteSpace(item.Author.Badge?.Label))
            {
                WriteTag(item.Author.Badge!.Label!, ConsoleColor.DarkCyan);
            }

            if (item.Superchat != null)
            {
                WriteSuperchatTag(item.Superchat);
            }

            if (item.MembershipDetails != null)
            {
                WriteMembershipTag(item.MembershipDetails);
            }

            Console.Write(' ');
            Console.ForegroundColor = item.IsOwner ? ConsoleColor.Red : ConsoleColor.White;
            Console.Write(item.Author.Name);
            Console.ResetColor();
            Console.Write(": ");

            if (item.Message.Length > 0)
            {
                WriteMessageParts(item.Message);
            }
            else
            {
                string? fallback = BuildFallbackMessage(item);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(fallback ?? "(no message)");
                Console.ResetColor();
            }

            Console.WriteLine();
        }
    }

    private static void WriteTimestamp(DateTimeOffset timestamp)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write('[');
        Console.Write(timestamp.ToLocalTime().ToString("HH:mm:ss"));
        Console.Write(']');
        Console.ResetColor();
    }

    private static void WriteTag(string text, ConsoleColor color)
    {
        Console.Write(' ');
        Console.ForegroundColor = color;
        Console.Write('[');
        Console.Write(text);
        Console.Write(']');
        Console.ResetColor();
    }

    private static void WriteSuperchatTag(Superchat superchat)
    {
        Console.Write(' ');
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("[SC ");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write(superchat.Currency);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(' ');
        Console.Write(superchat.AmountValue.ToString("0.##"));
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(']');
        Console.ResetColor();
    }

    private static void WriteMembershipTag(MembershipDetails membership)
    {
        string text = membership.EventType switch
        {
            MembershipEventType.New => $"JOIN {membership.LevelName ?? "Member"}",
            MembershipEventType.Milestone => membership.MilestoneMonths is int months
                ? $"MILESTONE {months}m"
                : "MILESTONE",
            MembershipEventType.GiftPurchase => membership.GiftCount is int giftCount
                ? $"GIFT x{giftCount}"
                : "GIFT",
            MembershipEventType.GiftRedemption => "GIFTED",
            _ => "MEM",
        };

        ConsoleColor color = membership.EventType switch
        {
            MembershipEventType.New => ConsoleColor.Green,
            MembershipEventType.Milestone => ConsoleColor.Cyan,
            MembershipEventType.GiftPurchase => ConsoleColor.Magenta,
            MembershipEventType.GiftRedemption => ConsoleColor.Blue,
            _ => ConsoleColor.DarkGray,
        };

        WriteTag(text, color);
    }

    private static void WriteMessageParts(MessagePart[] parts)
    {
        foreach (MessagePart part in parts)
        {
            switch (part)
            {
                case TextPart textPart:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write(textPart.Text);
                    break;
                case EmojiPart emojiPart:
                    string emojiText = string.IsNullOrWhiteSpace(emojiPart.EmojiText)
                        ? (emojiPart.Alt ?? string.Empty)
                        : emojiPart.EmojiText;
                    if (emojiPart.IsCustomEmoji)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write(
                            string.IsNullOrWhiteSpace(emojiText)
                                ? $"[{emojiPart.Alt ?? "custom-emoji"}]"
                                : emojiText
                        );
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(emojiText);
                    }
                    break;
                case ImagePart imagePart:
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.Write($"[img:{imagePart.Alt ?? "image"}]");
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("[part]");
                    break;
            }
        }

        Console.ResetColor();
    }

    private static string? BuildFallbackMessage(ChatItem item)
    {
        if (item.MembershipDetails == null)
        {
            if (item.Superchat?.Sticker?.Alt != null)
            {
                return $"Sticker: {item.Superchat.Sticker.Alt}";
            }

            return null;
        }

        MembershipDetails membership = item.MembershipDetails;
        return membership.EventType switch
        {
            MembershipEventType.New => membership.HeaderSubtext ?? membership.HeaderPrimaryText,
            MembershipEventType.Milestone => membership.HeaderPrimaryText ?? membership.HeaderSubtext,
            MembershipEventType.GiftPurchase => membership.HeaderPrimaryText,
            MembershipEventType.GiftRedemption => membership.HeaderPrimaryText,
            _ => membership.HeaderPrimaryText ?? membership.HeaderSubtext,
        };
    }
}

/// <summary>
/// Simple class to hold runtime options, like the Live ID.
/// </summary>
internal class ExampleRunOptions
{
    public string? LiveId { get; set; }
    public string? Handle { get; set; }
    public bool EnableJsonLogging { get; set; }
    public string? DebugLogPath { get; set; }
}
