using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Models;
using YTLiveChat.Contracts.Services;
using YTLiveChat.Services;

/// <summary>
/// Live-watch mode: connects to one or more channels/handles and captures only events that are
/// unrecognized by the parser (unknown actions, unknown membership types). Produces a JSONL file
/// containing raw action objects that can be fed back into the Tools log-analysis commands.
/// </summary>
internal static class WatchMode
{
    public static async Task<int> RunAsync(string[] args)
    {
        WatchOptions options = ParseWatchOptions(args);

        if (options.Targets.Count == 0)
        {
            PrintWatchUsage();
            return 1;
        }

        string outputPath = options.OutputPath ?? BuildDefaultOutputPath();
        string? outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        Console.WriteLine($"Watch mode — output: {outputPath}");
        Console.WriteLine($"Targets ({options.Targets.Count}): {string.Join(", ", options.Targets.Select(t => t.Tag))}");
        if (options.SkippedLiveIds.Count > 0)
        {
            Console.WriteLine($"Skipping live IDs: {string.Join(", ", options.SkippedLiveIds)}");
        }

        Console.WriteLine($"Capturing: {(options.AllMembership ? "all membership events + unknowns" : "unknown actions + unknown membership types")}");
        Console.WriteLine("Press Ctrl+C to stop.");
        Console.WriteLine();

        using CancellationTokenSource cts = new();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        using StreamWriter writer = new(outputPath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)) { AutoFlush = true };
        int[] capturedCount = [0];
        object fileLock = new();
        object consoleLock = new();

        List<(WatchTarget Target, IYTLiveChat Chat, HttpClient HttpClient)> sessions = [];

        foreach (WatchTarget target in options.Targets)
        {
#pragma warning disable CS0618
            YTLiveChatOptions ytOptions = new()
            {
                YoutubeBaseUrl = "https://www.youtube.com",
                EnableContinuousLivestreamMonitor = target.IsHandleOrChannel,
                LiveCheckFrequency = options.CheckIntervalMs,
                RequireActiveBroadcastForAutoDetectedStream = !options.IncludeScheduled,
                IgnoredAutoDetectedLiveIds = [.. options.SkippedLiveIds],
            };
#pragma warning restore CS0618

            HttpClient httpClient = new() { BaseAddress = new Uri("https://www.youtube.com") };
            YTHttpClient ytHttpClient = new(httpClient, NullLogger<YTHttpClient>.Instance);
            IYTLiveChat chat = new YTLiveChat.Services.YTLiveChat(
                ytOptions,
                ytHttpClient,
                NullLogger<YTLiveChat.Services.YTLiveChat>.Instance
            );

            sessions.Add((target, chat, httpClient));

            WireHandlers(target, chat, options, writer, fileLock, consoleLock, capturedCount);

            StartChat(target, chat);
        }

        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ctrl+C — expected
        }
        finally
        {
            foreach ((_, IYTLiveChat chat, HttpClient httpClient) in sessions)
            {
                chat.Stop();
                if (chat is IDisposable d)
                {
                    d.Dispose();
                }

                httpClient.Dispose();
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Stopped. {capturedCount[0]} event(s) captured → {outputPath}");

        if (capturedCount[0] > 0)
        {
            Console.WriteLine();
            Console.WriteLine("To inspect the captured events:");
            Console.WriteLine(
                $"  dotnet run --project YTLiveChat.Tools -- --variants {outputPath}"
            );
            Console.WriteLine(
                $"  dotnet run --project YTLiveChat.Tools -- --dump-renderer=liveChatMembershipItemRenderer {outputPath}"
            );
        }

        return 0;
    }

    private static void WireHandlers(
        WatchTarget target,
        IYTLiveChat chat,
        WatchOptions options,
        StreamWriter writer,
        object fileLock,
        object consoleLock,
        int[] capturedCount
    )
    {
        chat.RawActionReceived += (_, e) =>
        {
            bool isCompletelyUnknown = e.ParsedChatItem == null;
            bool isUnknownMembership =
                e.ParsedChatItem?.MembershipDetails?.EventType == MembershipEventType.Unknown;
            bool isKnownMembership =
                options.AllMembership && e.ParsedChatItem?.MembershipDetails != null;

            if (!isCompletelyUnknown && !isUnknownMembership && !isKnownMembership)
            {
                return;
            }

            string reason = isCompletelyUnknown ? "unknown-action"
                : isUnknownMembership ? "unknown-membership"
                : "membership";

            string rendererKey = GetRendererKey(e.RawAction);
            string compactJson = JsonSerializer.Serialize(e.RawAction);

            lock (fileLock)
            {
                writer.WriteLine(compactJson);
                capturedCount[0]++;
            }

            lock (consoleLock)
            {
                Console.Write('[');
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(DateTimeOffset.Now.ToString("HH:mm:ss"));
                Console.ResetColor();
                Console.Write("] ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"[{target.Tag}]");
                Console.ResetColor();
                Console.Write(' ');
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[CAPTURED]");
                Console.ResetColor();
                Console.WriteLine($" {reason} | {rendererKey}");
            }
        };

#pragma warning disable CS0618
        chat.InitialPageLoaded += (_, e) => WriteStatus(consoleLock, target.Tag, "LIVE", ConsoleColor.Green, e.LiveId);
        chat.LivestreamStarted += (_, e) => WriteStatus(consoleLock, target.Tag, "STREAM START", ConsoleColor.Green, e.LiveId);
        chat.LivestreamEnded += (_, e) => WriteStatus(consoleLock, target.Tag, "STREAM END", ConsoleColor.DarkYellow, e.LiveId);
        chat.LivestreamInaccessible += (_, e) => WriteStatus(consoleLock, target.Tag, "BLOCKED", ConsoleColor.DarkYellow, e.LiveId);
#pragma warning restore CS0618
        chat.ChatStopped += (_, e) => WriteStatus(consoleLock, target.Tag, "STOPPED", ConsoleColor.Red, e.Reason);
        chat.ErrorOccurred += (_, e) => WriteStatus(consoleLock, target.Tag, "ERROR", ConsoleColor.Red, e.GetException().Message);
    }

    private static void StartChat(WatchTarget target, IYTLiveChat chat)
    {
        if (target.Handle != null)
        {
            chat.Start(handle: target.Handle);
        }
        else if (target.ChannelId != null)
        {
            chat.Start(channelId: target.ChannelId);
        }
        else
        {
            chat.Start(liveId: target.LiveId!);
        }
    }

    private static void WriteStatus(object consoleLock, string tag, string label, ConsoleColor color, string? detail)
    {
        lock (consoleLock)
        {
            Console.Write('[');
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(DateTimeOffset.Now.ToString("HH:mm:ss"));
            Console.ResetColor();
            Console.Write("] ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"[{tag}]");
            Console.ResetColor();
            Console.Write(' ');
            Console.ForegroundColor = color;
            Console.Write($"[{label}]");
            Console.ResetColor();
            if (!string.IsNullOrWhiteSpace(detail))
            {
                Console.Write($" {detail}");
            }

            Console.WriteLine();
        }
    }

    private static string GetRendererKey(JsonElement action)
    {
        if (action.ValueKind != JsonValueKind.Object)
        {
            return "?";
        }

        using JsonElement.ObjectEnumerator outer = action.EnumerateObject();
        if (!outer.MoveNext())
        {
            return "?";
        }

        // Try to dig one level deeper to get the renderer key inside addChatItemAction.item
        JsonElement actionValue = outer.Current.Value;
        if (
            actionValue.ValueKind == JsonValueKind.Object
            && actionValue.TryGetProperty("item", out JsonElement item)
            && item.ValueKind == JsonValueKind.Object
        )
        {
            using JsonElement.ObjectEnumerator itemEnum = item.EnumerateObject();
            if (itemEnum.MoveNext())
            {
                return itemEnum.Current.Name;
            }
        }

        return outer.Current.Name;
    }

    private static string BuildDefaultOutputPath()
    {
        string fileName = $"watch_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.jsonl";
        return Path.GetFullPath(Path.Combine("logs", fileName));
    }

    private static WatchOptions ParseWatchOptions(string[] args)
    {
        List<WatchTarget> targets = [];
        List<string> skippedLiveIds = [];
        int checkIntervalMs = 10000;
        bool includeScheduled = false;
        bool allMembership = false;
        string? outputPath = null;

        foreach (string arg in args)
        {
            if (arg.Equals("--include-scheduled", StringComparison.OrdinalIgnoreCase))
            {
                includeScheduled = true;
                continue;
            }

            if (arg.Equals("--all-membership", StringComparison.OrdinalIgnoreCase))
            {
                allMembership = true;
                continue;
            }

            const string skipPrefix = "--skip=";
            if (arg.StartsWith(skipPrefix, StringComparison.OrdinalIgnoreCase))
            {
                skippedLiveIds.AddRange(
                    arg[skipPrefix.Length..]
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                );
                continue;
            }

            const string checkIntervalPrefix = "--check-interval=";
            if (arg.StartsWith(checkIntervalPrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(arg[checkIntervalPrefix.Length..], out int parsed) && parsed > 0)
                {
                    checkIntervalMs = parsed;
                }

                continue;
            }

            const string outputPrefix = "--output=";
            if (arg.StartsWith(outputPrefix, StringComparison.OrdinalIgnoreCase))
            {
                outputPath = Path.GetFullPath(arg[outputPrefix.Length..]);
                continue;
            }

            targets.Add(ParseTarget(arg));
        }

        return new WatchOptions(targets, skippedLiveIds, checkIntervalMs, includeScheduled, allMembership, outputPath);
    }

    private static WatchTarget ParseTarget(string identifier)
    {
        if (identifier.StartsWith("@", StringComparison.Ordinal))
        {
            return new WatchTarget(identifier, Handle: identifier, ChannelId: null, LiveId: null, IsHandleOrChannel: true);
        }

        if (identifier.StartsWith("UC", StringComparison.OrdinalIgnoreCase))
        {
            return new WatchTarget(identifier, Handle: null, ChannelId: identifier, LiveId: null, IsHandleOrChannel: true);
        }

        return new WatchTarget(identifier, Handle: null, ChannelId: null, LiveId: identifier, IsHandleOrChannel: false);
    }

    private static void PrintWatchUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine(
            "  dotnet run --project YTLiveChat.Tools -- watch [options] <@handle|UCxxx|liveId> [...]"
        );
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine(
            "  --skip=<id[,id...]>       Skip these video IDs when auto-detecting streams (comma-separated)."
        );
        Console.WriteLine(
            "  --check-interval=<ms>     How often to poll for a live stream. Default: 10000."
        );
        Console.WriteLine(
            "  --include-scheduled       Include scheduled streams and free-chat. Default: active broadcasts only."
        );
        Console.WriteLine(
            "  --all-membership          Also capture all successfully-parsed membership events, not just Unknown ones."
        );
        Console.WriteLine(
            "  --output=<path>           Output JSONL file path. Default: logs/watch_<timestamp>.jsonl."
        );
        Console.WriteLine();
        Console.WriteLine("What gets captured:");
        Console.WriteLine(
            "  - Any action the parser did not produce a ChatItem for (completely unknown renderer/action)."
        );
        Console.WriteLine(
            "  - Membership events parsed with EventType=Unknown (e.g. tier-upgrade events)."
        );
        Console.WriteLine(
            "  - With --all-membership: all membership events (New/Milestone/Gift/Redemption too)."
        );
        Console.WriteLine();
        Console.WriteLine("Output is a JSONL file (one raw action JSON per line) compatible with");
        Console.WriteLine("the log-analysis commands (--dump-renderer, --variants, etc.).");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine(
            "  dotnet run --project YTLiveChat.Tools -- watch @SomeStreamer @OtherStreamer"
        );
        Console.WriteLine(
            "  dotnet run --project YTLiveChat.Tools -- watch --skip=abc123,def456 --output=upgrades.jsonl @SomeStreamer"
        );
    }

    private sealed record WatchOptions(
        IReadOnlyList<WatchTarget> Targets,
        IReadOnlyList<string> SkippedLiveIds,
        int CheckIntervalMs,
        bool IncludeScheduled,
        bool AllMembership,
        string? OutputPath
    );

    private sealed record WatchTarget(
        string Tag,
        string? Handle,
        string? ChannelId,
        string? LiveId,
        bool IsHandleOrChannel
    );
}
