using System.Text.Json;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Models;
using YTLiveChat.Contracts.Services;
using YTLiveChat.Services;

namespace YTLiveChat.Example;

internal class ChatMonitorService : IHostedService, IDisposable
{
    private readonly ILogger<ChatMonitorService> _logger;
    private readonly IReadOnlyList<ExampleRunOptions> _runOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly CancellationTokenSource _stoppingCts = new();

    private readonly List<MonitorSession> _sessions = [];
    private readonly object _sessionLock = new();

    private static readonly object s_consoleLock = new();

    public ChatMonitorService(
        ILogger<ChatMonitorService> logger,
        IReadOnlyList<ExampleRunOptions> runOptions,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        IHostApplicationLifetime appLifetime
    )
    {
        _logger = logger;
        _runOptions = runOptions;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _appLifetime = appLifetime;
        _ = appLifetime.ApplicationStopping.Register(OnApplicationStopping);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_runOptions.Count == 0)
        {
            _logger.LogError("No targets configured for chat monitor.");
            _appLifetime.StopApplication();
            return Task.CompletedTask;
        }

        foreach (ExampleRunOptions runOptions in _runOptions)
        {
            MonitorSession session = BuildSession(runOptions);
            _sessions.Add(session);
            AttachHandlers(session);

            try
            {
                StartSession(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start target {SourceTag}", session.SourceTag);
                MarkSessionStopped(session, $"Start failed: {ex.Message}");
            }
        }

        if (_sessions.All(s => s.IsStopped))
        {
            _appLifetime.StopApplication();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping all chat monitor sessions.");
        foreach (MonitorSession session in _sessions)
        {
            DetachHandlers(session);
            session.Chat.Stop();
        }

        _stoppingCts.Cancel();
        return Task.CompletedTask;
    }

    private MonitorSession BuildSession(ExampleRunOptions options)
    {
        YTLiveChatOptions ytOptions = new()
        {
            YoutubeBaseUrl = "https://www.youtube.com",
        };

#pragma warning disable CS0618
        if (options.EnableContinuousMonitor)
        {
            ytOptions.EnableContinuousLivestreamMonitor = true;
            ytOptions.LiveCheckFrequency = options.LiveCheckFrequency;
            ytOptions.RequireActiveBroadcastForAutoDetectedStream =
                options.RequireActiveBroadcastForAutoDetectedStream;
            ytOptions.IgnoredAutoDetectedLiveIds = options.IgnoredAutoDetectedLiveIds;
        }
#pragma warning restore CS0618

        if (options.EnableJsonLogging)
        {
            ytOptions.DebugLogReceivedJsonItems = true;
            if (!string.IsNullOrWhiteSpace(options.DebugLogPath))
            {
                ytOptions.DebugLogFilePath = options.DebugLogPath!;
            }
        }

        HttpClient httpClient = _httpClientFactory.CreateClient("YTLiveChatExample");
        httpClient.BaseAddress ??= new Uri("https://www.youtube.com");
        YTHttpClient ytHttpClient = new(httpClient, _loggerFactory.CreateLogger<YTHttpClient>());
        IYTLiveChat chat = new YTLiveChat.Services.YTLiveChat(
            ytOptions,
            ytHttpClient,
            _loggerFactory.CreateLogger<YTLiveChat.Services.YTLiveChat>()
        );

        return new(options.SourceTag, options, chat);
    }

    private void StartSession(MonitorSession session)
    {
        if (!string.IsNullOrWhiteSpace(session.Options.Handle))
        {
            session.Chat.Start(handle: session.Options.Handle);
            return;
        }

        if (!string.IsNullOrWhiteSpace(session.Options.ChannelId))
        {
            session.Chat.Start(channelId: session.Options.ChannelId);
            return;
        }

        if (!string.IsNullOrWhiteSpace(session.Options.LiveId))
        {
            session.Chat.Start(liveId: session.Options.LiveId);
            return;
        }

        throw new InvalidOperationException("No valid target identifier configured.");
    }

    private void AttachHandlers(MonitorSession session)
    {
        session.InitialPageLoadedHandler = (_, e) => OnInitialPageLoaded(session, e);
#pragma warning disable CS0618
        session.LivestreamStartedHandler = (_, e) => OnLivestreamStarted(session, e);
        session.LivestreamEndedHandler = (_, e) => OnLivestreamEnded(session, e);
        session.LivestreamInaccessibleHandler = (_, e) => OnLivestreamInaccessible(session, e);
#pragma warning restore CS0618
        session.ChatReceivedHandler = (_, e) => OnChatReceived(session, e);
        session.RawActionReceivedHandler = (_, e) => OnRawActionReceived(session, e);
        session.ChatStoppedHandler = (_, e) => OnChatStopped(session, e);
        session.ErrorOccurredHandler = (_, e) => OnErrorOccurred(session, e);

        session.Chat.InitialPageLoaded += session.InitialPageLoadedHandler;
#pragma warning disable CS0618
        session.Chat.LivestreamStarted += session.LivestreamStartedHandler;
        session.Chat.LivestreamEnded += session.LivestreamEndedHandler;
        session.Chat.LivestreamInaccessible += session.LivestreamInaccessibleHandler;
#pragma warning restore CS0618
        session.Chat.ChatReceived += session.ChatReceivedHandler;
        session.Chat.RawActionReceived += session.RawActionReceivedHandler;
        session.Chat.ChatStopped += session.ChatStoppedHandler;
        session.Chat.ErrorOccurred += session.ErrorOccurredHandler;
    }

    private static void DetachHandlers(MonitorSession session)
    {
        if (session.InitialPageLoadedHandler != null)
        {
            session.Chat.InitialPageLoaded -= session.InitialPageLoadedHandler;
        }

#pragma warning disable CS0618
        if (session.LivestreamStartedHandler != null)
        {
            session.Chat.LivestreamStarted -= session.LivestreamStartedHandler;
        }

        if (session.LivestreamEndedHandler != null)
        {
            session.Chat.LivestreamEnded -= session.LivestreamEndedHandler;
        }

        if (session.LivestreamInaccessibleHandler != null)
        {
            session.Chat.LivestreamInaccessible -= session.LivestreamInaccessibleHandler;
        }
#pragma warning restore CS0618

        if (session.ChatReceivedHandler != null)
        {
            session.Chat.ChatReceived -= session.ChatReceivedHandler;
        }

        if (session.RawActionReceivedHandler != null)
        {
            session.Chat.RawActionReceived -= session.RawActionReceivedHandler;
        }

        if (session.ChatStoppedHandler != null)
        {
            session.Chat.ChatStopped -= session.ChatStoppedHandler;
        }

        if (session.ErrorOccurredHandler != null)
        {
            session.Chat.ErrorOccurred -= session.ErrorOccurredHandler;
        }
    }

    private void OnInitialPageLoaded(MonitorSession session, InitialPageLoadedEventArgs e)
    {
        lock (s_consoleLock)
        {
            WriteTimestamp(DateTimeOffset.UtcNow);
            WriteSourceTag(session.SourceTag);
            WriteTag("LIVE", ConsoleColor.Green);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" -> ");
            Console.ResetColor();
            Console.WriteLine(e.LiveId);
        }
    }

    private void OnLivestreamStarted(MonitorSession session, LivestreamStartedEventArgs e)
    {
        lock (s_consoleLock)
        {
            WriteTimestamp(DateTimeOffset.UtcNow);
            WriteSourceTag(session.SourceTag);
            WriteTag("STREAM START", ConsoleColor.Green);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" -> ");
            Console.ResetColor();
            Console.WriteLine(e.LiveId);
        }
    }

    private void OnLivestreamEnded(MonitorSession session, LivestreamEndedEventArgs e)
    {
        lock (s_consoleLock)
        {
            WriteTimestamp(DateTimeOffset.UtcNow);
            WriteSourceTag(session.SourceTag);
            WriteTag("STREAM END", ConsoleColor.DarkYellow);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" -> ");
            Console.ResetColor();
            Console.WriteLine($"{e.LiveId} ({e.Reason ?? "Unknown"})");
        }
    }

    private void OnLivestreamInaccessible(
        MonitorSession session,
        LivestreamInaccessibleEventArgs e
    )
    {
        lock (s_consoleLock)
        {
            WriteTimestamp(DateTimeOffset.UtcNow);
            WriteSourceTag(session.SourceTag);
            WriteTag("STREAM BLOCKED", ConsoleColor.DarkYellow);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" -> ");
            Console.ResetColor();
            Console.WriteLine($"{e.LiveId} ({e.Reason ?? "Unknown"})");
        }
    }

    private static void OnChatReceived(MonitorSession session, ChatReceivedEventArgs e) =>
        RenderChatItem(session.SourceTag, e.ChatItem);

    private static void OnRawActionReceived(MonitorSession session, RawActionReceivedEventArgs e)
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
            WriteSourceTag(session.SourceTag);
            WriteTag("RAW", ConsoleColor.DarkGray);
            Console.Write(' ');
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(actionKind);
            Console.ResetColor();
            Console.WriteLine();
        }
    }

    private void OnChatStopped(MonitorSession session, ChatStoppedEventArgs e)
    {
        lock (s_consoleLock)
        {
            WriteTimestamp(DateTimeOffset.UtcNow);
            WriteSourceTag(session.SourceTag);
            WriteTag("STOP", ConsoleColor.Red);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" -> ");
            Console.ResetColor();
            Console.WriteLine(e.Reason ?? "Unknown");
        }

        MarkSessionStopped(session, e.Reason);
    }

    private void OnErrorOccurred(MonitorSession session, ErrorOccurredEventArgs e)
    {
        Exception ex = e.GetException();
        _logger.LogError(ex, "Error from target {SourceTag}", session.SourceTag);

        lock (s_consoleLock)
        {
            WriteTimestamp(DateTimeOffset.UtcNow);
            WriteSourceTag(session.SourceTag);
            WriteTag("ERROR", ConsoleColor.Red);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" -> ");
            Console.ResetColor();
            Console.WriteLine(ex.Message);
        }

        if (ex is HttpRequestException httpEx)
        {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            if (httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
#else
            if (httpEx.Message.Contains("403") || httpEx.Message.Contains("Forbidden"))
#endif
            {
                _logger.LogCritical("Stopping application due to forbidden HTTP error.");
                _appLifetime.StopApplication();
            }
        }
    }

    private void MarkSessionStopped(MonitorSession session, string? reason)
    {
        bool shouldStopApp = false;
        lock (_sessionLock)
        {
            if (session.IsStopped)
            {
                return;
            }

            session.IsStopped = true;
            DetachHandlers(session);
            session.Chat.Stop();
            shouldStopApp = _sessions.All(s => s.IsStopped);
        }

        if (shouldStopApp)
        {
            _logger.LogInformation("All sessions stopped. Shutting down host.");
            _appLifetime.StopApplication();
        }
    }

    private void OnApplicationStopping()
    {
        _logger.LogInformation("Application stopping. Stopping monitor sessions.");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        foreach (MonitorSession session in _sessions)
        {
            DetachHandlers(session);
            session.Chat.Stop();
            if (session.Chat is IDisposable disposableChat)
            {
                disposableChat.Dispose();
            }
        }

        _stoppingCts.Dispose();
    }

    private static void RenderChatItem(string sourceTag, ChatItem item)
    {
        lock (s_consoleLock)
        {
            WriteTimestamp(item.Timestamp);
            WriteSourceTag(sourceTag);

            if (item.ViewerLeaderboardRank is int rank)
            {
                WriteTag($"#{rank}", ConsoleColor.DarkYellow);
            }

            if (item.IsTicker)
            {
                WriteTag("TICKER", ConsoleColor.DarkMagenta);
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

    private static void WriteSourceTag(string sourceTag)
    {
        Console.Write(' ');
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write('[');
        Console.Write(sourceTag);
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
            MembershipEventType.Milestone => (membership.MilestoneMonths is int months
                ? $"MILESTONE {months}m"
                : "MILESTONE") + $" (levelName: {membership.LevelName})",
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
            return item.Superchat?.Sticker?.Alt != null
                ? $"Sticker: {item.Superchat.Sticker.Alt}"
                : null;
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

    private sealed class MonitorSession
    {
        public MonitorSession(string sourceTag, ExampleRunOptions options, IYTLiveChat chat)
        {
            SourceTag = sourceTag;
            Options = options;
            Chat = chat;
        }

        public string SourceTag { get; }
        public ExampleRunOptions Options { get; }
        public IYTLiveChat Chat { get; }
        public bool IsStopped { get; set; }

        public EventHandler<InitialPageLoadedEventArgs>? InitialPageLoadedHandler { get; set; }
        public EventHandler<LivestreamStartedEventArgs>? LivestreamStartedHandler { get; set; }
        public EventHandler<LivestreamEndedEventArgs>? LivestreamEndedHandler { get; set; }
        public EventHandler<LivestreamInaccessibleEventArgs>? LivestreamInaccessibleHandler { get; set; }
        public EventHandler<ChatReceivedEventArgs>? ChatReceivedHandler { get; set; }
        public EventHandler<RawActionReceivedEventArgs>? RawActionReceivedHandler { get; set; }
        public EventHandler<ChatStoppedEventArgs>? ChatStoppedHandler { get; set; }
        public EventHandler<ErrorOccurredEventArgs>? ErrorOccurredHandler { get; set; }
    }
}

internal class ExampleRunOptions
{
    public string SourceTag { get; set; } = string.Empty;
    public string? LiveId { get; set; }
    public string? Handle { get; set; }
    public string? ChannelId { get; set; }
    public bool EnableContinuousMonitor { get; set; }
    public int LiveCheckFrequency { get; set; } = 10000;
    public bool RequireActiveBroadcastForAutoDetectedStream { get; set; }
    public List<string> IgnoredAutoDetectedLiveIds { get; set; } = [];
    public bool EnableJsonLogging { get; set; }
    public string? DebugLogPath { get; set; }
}
