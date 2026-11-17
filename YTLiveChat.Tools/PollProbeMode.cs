using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using YTLiveChat.Helpers;
using YTLiveChat.Models;
using YTLiveChat.Models.Response;
using YTLiveChat.Services;

/// <summary>
/// Poll-probe mode: polls the chat of one or more live streams at a fixed interval and records, per poll, the
/// continuation kind YouTube returned, its <c>timeoutMs</c>, how many actions arrived and how old the newest
/// chat item was. Shows how often a chat must be polled to keep up, and what YouTube asks for.
/// </summary>
internal static class PollProbeMode
{
    public static async Task<int> RunAsync(string[] args)
    {
        int intervalMs = 1000;
        int durationSeconds = 180;
        string? outputPath = null;
        List<string> videoIds = [];
        foreach (string arg in args)
        {
            if (arg.StartsWith("--interval=", StringComparison.Ordinal))
            {
                intervalMs = int.Parse(arg["--interval=".Length..], CultureInfo.InvariantCulture);
            }
            else if (arg.StartsWith("--duration=", StringComparison.Ordinal))
            {
                durationSeconds = int.Parse(arg["--duration=".Length..], CultureInfo.InvariantCulture);
            }
            else if (arg.StartsWith("--output=", StringComparison.Ordinal))
            {
                outputPath = arg["--output=".Length..];
            }
            else
            {
                videoIds.Add(arg);
            }
        }

        if (videoIds.Count == 0)
        {
            Console.WriteLine("Usage: pollprobe [--interval=ms] [--duration=s] [--output=file.tsv] <videoId>...");
            return 1;
        }

        using HttpClient http = new() { BaseAddress = new Uri("https://www.youtube.com") };
        YTHttpClient client = new(http, NullLogger<YTHttpClient>.Instance);
        await using StreamWriter output = new(outputPath ?? Path.Combine(Path.GetTempPath(), "pollprobe.tsv"));
        await output.WriteLineAsync("videoId\telapsedMs\tkind\ttimeoutMs\tactions\tnewestAgeMs").ConfigureAwait(false);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(durationSeconds));
        Task[] probes =
        [
            .. videoIds.Select(id => ProbeAsync(client, id, intervalMs, output, cts.Token)),
        ];
        await Task.WhenAll(probes).ConfigureAwait(false);
        return 0;
    }

    private static async Task ProbeAsync(
        YTHttpClient client,
        string videoId,
        int intervalMs,
        StreamWriter output,
        CancellationToken ct
    )
    {
        FetchOptions options;
        try
        {
            options = Parser.GetOptionsFromLivePage(
                await client.GetOptionsAsync(null, null, videoId, ct).ConfigureAwait(false)
            );
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
        {
            Console.WriteLine($"{videoId}: {ex.Message}");
            return;
        }

        Stopwatch clock = Stopwatch.StartNew();
        int polls = 0;
        int empty = 0;
        Dictionary<string, int> kinds = [];
        List<int> timeouts = [];
        while (!ct.IsCancellationRequested)
        {
            LiveChatResponse? response;
            try
            {
                (response, _) = await client.GetLiveChatAsync(options, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            Continuation? next = response?.ContinuationContents?.LiveChatContinuation?.Continuations?.FirstOrDefault();
            string kind = next?.InvalidationContinuationData is not null ? "invalidation"
                : next?.TimedContinuationData is not null ? "timed"
                : "none";
            int timeout = next?.InvalidationContinuationData?.TimeoutMs ?? next?.TimedContinuationData?.TimeoutMs ?? 0;
            int actions = response?.ContinuationContents?.LiveChatContinuation?.Actions?.Count ?? 0;
            (List<YTLiveChat.Contracts.Models.ChatItem> items, string? continuation) = Parser.ParseLiveChatResponse(response);
            long newestAge = items.Count == 0
                ? -1
                : (long)(DateTimeOffset.UtcNow - items.Max(static i => i.Timestamp)).TotalMilliseconds;

            polls++;
            empty += actions == 0 ? 1 : 0;
            kinds[kind] = kinds.GetValueOrDefault(kind) + 1;
            timeouts.Add(timeout);
            lock (output)
            {
                output.WriteLine($"{videoId}\t{clock.ElapsedMilliseconds}\t{kind}\t{timeout}\t{actions}\t{newestAge}");
            }

            if (string.IsNullOrEmpty(continuation))
            {
                break;
            }

            options = options with { Continuation = continuation };
            try
            {
                await Task.Delay(intervalMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        timeouts.Sort();
        Console.WriteLine(
            $"{videoId}: {polls} polls, {empty} empty, kinds {string.Join(' ', kinds.Select(k => $"{k.Key}={k.Value}"))},"
                + $" timeoutMs median {(timeouts.Count == 0 ? 0 : timeouts[timeouts.Count / 2])}"
        );
    }
}
