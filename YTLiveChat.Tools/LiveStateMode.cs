using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Models;
using YTLiveChat.Services;

/// <summary>
/// Live-state mode: for each channel, lists the live and upcoming entries of its streams page (plus the most
/// recent past stream), then reports every live-state signal YouTube gives for each of them: the markers in the
/// watch page, the innertube <c>player</c> response and the innertube <c>updated_metadata</c> response. Raw
/// responses are saved so the signals can be compared across live, waiting-room and ended streams.
/// </summary>
internal static partial class LiveStateMode
{
    public static async Task<int> RunAsync(string[] args)
    {
        string? outputDir = null;
        List<string> targets = [];
        foreach (string arg in args)
        {
            if (arg.StartsWith("--out=", StringComparison.Ordinal))
            {
                outputDir = arg["--out=".Length..];
            }
            else
            {
                targets.Add(arg);
            }
        }

        if (targets.Count == 0)
        {
            Console.WriteLine(
                "Usage: livestate [--out=<dir>] <@handle | UCchannelId | videoId>...\n"
                    + "  Channels: probes the live and upcoming entries and the latest past stream.\n"
                    + "  Video ids: probes that video."
            );
            return 1;
        }

        outputDir ??= Path.Combine(Path.GetTempPath(), "ytlivechat-livestate");
        Directory.CreateDirectory(outputDir);

        using HttpClient httpClient = new() { BaseAddress = new Uri("https://www.youtube.com") };
        YTHttpClient ytHttpClient = new(httpClient, NullLogger<YTHttpClient>.Instance);
        using YTLiveChat.Services.YTLiveChat liveChat = new(new YTLiveChatOptions(), ytHttpClient);

        foreach (string target in targets)
        {
            List<(string Id, string Source)> videos = [];
            if (target.StartsWith('@') || target.StartsWith("UC", StringComparison.Ordinal))
            {
                IReadOnlyList<StreamInfo> streams = await liveChat
                    .GetStreamsAsync(
                        handle: target.StartsWith('@') ? target : null,
                        channelId: target.StartsWith('@') ? null : target
                    )
                    .ConfigureAwait(false);
                Console.WriteLine($"== {target}: {streams.Count} entries on the streams page");
                foreach (StreamInfo s in streams.Where(s => s.Status is StreamStatus.Live or StreamStatus.Upcoming))
                {
                    videos.Add((s.LiveId, $"streams:{s.Status} scheduled={s.ScheduledAt:O} viewers={s.ViewerCount}"));
                }

                StreamInfo? past = streams.FirstOrDefault(s => s.Status is not (StreamStatus.Live or StreamStatus.Upcoming));
                if (past is not null)
                {
                    videos.Add((past.LiveId, $"streams:{past.Status} published='{past.PublishedTimeText}'"));
                }
            }
            else
            {
                videos.Add((target, "given"));
            }

            foreach ((string id, string source) in videos)
            {
                await ProbeAsync(httpClient, ytHttpClient, id, source, outputDir).ConfigureAwait(false);
            }
        }

        Console.WriteLine($"Raw responses: {outputDir}");
        return 0;
    }

    private static async Task ProbeAsync(
        HttpClient http,
        YTHttpClient ytHttpClient,
        string videoId,
        string source,
        string outputDir
    )
    {
        Console.WriteLine($"-- {videoId} ({source})");
        string html = await ytHttpClient.GetOptionsAsync(null, null, videoId).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"{videoId}.watch.html"), html).ConfigureAwait(false);

        Console.WriteLine("   watch:"
            + $" isLiveNow={Values(html, "isLiveNow")} isUpcoming={Values(html, "isUpcoming")}"
            + $" isLive={Values(html, "isLive")} isLiveContent={Values(html, "isLiveContent")}"
            + $" isReplay={Values(html, "isReplay")} offlineSlate={html.Contains("liveStreamOfflineSlateRenderer", StringComparison.Ordinal)}");
        Match details = LiveBroadcastDetailsRegex().Match(html);
        if (details.Success)
        {
            Console.WriteLine($"   watch liveBroadcastDetails: {details.Value}");
        }

        string? apiKey = ApiKeyRegex().Match(html) is { Success: true } k ? k.Groups[1].Value : null;
        string? clientVersion = ClientVersionRegex().Match(html) is { Success: true } v ? v.Groups[1].Value : null;
        if (apiKey is null || clientVersion is null)
        {
            Console.WriteLine("   no innertube key or client version on the page");
            return;
        }

        JsonNode? player = await PostAsync(http, "player", apiKey, clientVersion, videoId, outputDir).ConfigureAwait(false);
        if (player is not null)
        {
            JsonNode? vd = player["videoDetails"];
            JsonNode? ps = player["playabilityStatus"];
            JsonNode? lbd = player["microformat"]?["playerMicroformatRenderer"]?["liveBroadcastDetails"];
            JsonNode? slate = ps?["liveStreamability"]?["liveStreamabilityRenderer"]?["offlineSlate"];
            Console.WriteLine($"   player: status={ps?["status"]} isLive={vd?["isLive"]} isUpcoming={vd?["isUpcoming"]}"
                + $" isLiveContent={vd?["isLiveContent"]} liveStreamability={ps?["liveStreamability"] is not null}"
                + $" offlineSlate={slate is not null}");
            Console.WriteLine($"   player liveBroadcastDetails: {lbd?.ToJsonString() ?? "(none)"}");
            string? scheduled = slate?["liveStreamOfflineSlateRenderer"]?["scheduledStartTime"]?.ToString();
            if (scheduled is not null)
            {
                Console.WriteLine($"   player offlineSlate.scheduledStartTime: {scheduled}");
            }
        }

        JsonNode? metadata = await PostAsync(http, "updated_metadata", apiKey, clientVersion, videoId, outputDir).ConfigureAwait(false);
        if (metadata?["actions"] is JsonArray actions)
        {
            foreach (JsonNode? action in actions)
            {
                if (action?["updateViewershipAction"]?["viewCount"]?["videoViewCountRenderer"] is { } views)
                {
                    Console.WriteLine($"   updated_metadata viewership: isLive={views["isLive"]} text={Runs(views["viewCount"])}"
                        + $" original={views["originalViewCount"]}");
                }

                if (action?["updateDateTextAction"]?["dateText"] is { } date)
                {
                    Console.WriteLine($"   updated_metadata dateText: {Runs(date)}");
                }
            }

            Console.WriteLine($"   updated_metadata timeoutMs: {metadata["continuation"]?["timedContinuationData"]?["timeoutMs"]}");
        }
    }

    private static async Task<JsonNode?> PostAsync(
        HttpClient http,
        string endpoint,
        string apiKey,
        string clientVersion,
        string videoId,
        string outputDir
    )
    {
        string body = JsonSerializer.Serialize(new
        {
            context = new { client = new { clientName = "WEB", clientVersion, hl = "en", gl = "US" } },
            videoId,
        });
        using HttpRequestMessage request = new(HttpMethod.Post, $"/youtubei/v1/{endpoint}?key={apiKey}&prettyPrint=false")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        using HttpResponseMessage response = await http.SendAsync(request).ConfigureAwait(false);
        string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"{videoId}.{endpoint}.json"), json).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"   {endpoint}: HTTP {(int)response.StatusCode}");
            return null;
        }

        return JsonNode.Parse(json);
    }

    private static string Runs(JsonNode? text) =>
        text?["simpleText"]?.ToString()
        ?? string.Concat((text?["runs"] as JsonArray ?? []).Select(r => r?["text"]?.ToString()));

    private static string Values(string html, string key)
    {
        string[] values =
        [
            .. Regex.Matches(html, $"\"{key}\":\\s*(true|false)").Select(m => m.Groups[1].Value).Distinct(),
        ];
        return values.Length == 0 ? "-" : string.Join('/', values);
    }

    [GeneratedRegex("\"liveBroadcastDetails\":\\{[^}]*\\}")]
    private static partial Regex LiveBroadcastDetailsRegex();

    [GeneratedRegex("\"INNERTUBE_API_KEY\":\\s*\"([^\"]+)\"")]
    private static partial Regex ApiKeyRegex();

    [GeneratedRegex("\"INNERTUBE_CONTEXT_CLIENT_VERSION\":\\s*\"([^\"]+)\"")]
    private static partial Regex ClientVersionRegex();
}
