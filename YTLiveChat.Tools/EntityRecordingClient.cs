using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using YTLiveChat.Models;
using YTLiveChat.Models.Response;
using YTLiveChat.Services;

/// <summary>
/// A <see cref="YTHttpClient"/> that also records the entity mutations of every chat response
/// (<c>frameworkUpdates.entityBatchUpdate.mutations</c>): YouTube sends state such as creator goals there rather
/// than as chat actions. Each mutation is written as one JSONL line with its payload type, so the analysis can count
/// and sample them.
/// </summary>
internal sealed class EntityRecordingClient(
    HttpClient httpClient,
    string tag,
    EntityRecorder recorder,
    SchemaRecorder schema
) : YTHttpClient(httpClient, NullLogger<YTHttpClient>.Instance)
{
    public override async Task<(LiveChatResponse? Response, string? RawJson)> GetLiveChatAsync(
        FetchOptions options,
        CancellationToken cancellationToken = default
    )
    {
        (LiveChatResponse? response, string? rawJson) = await base.GetLiveChatAsync(
                options,
                cancellationToken
            )
            .ConfigureAwait(false);
        if (rawJson is not null)
        {
            recorder.Record(tag, options.LiveId, rawJson);
            schema.Record(rawJson);
        }

        return (response, rawJson);
    }
}

/// <summary>Writes entity mutations to a JSONL file and counts them per payload type.</summary>
internal sealed class EntityRecorder(StreamWriter writer)
{
    private readonly ConcurrentDictionary<string, int> _counts = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, int> Counts => _counts;

    public void Record(string tag, string liveId, string rawJson)
    {
        if (!rawJson.Contains("\"mutations\"", StringComparison.Ordinal))
        {
            return;
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(rawJson);
        }
        catch (JsonException)
        {
            return;
        }

        JsonArray? mutations =
            root?["frameworkUpdates"]?["entityBatchUpdate"]?["mutations"] as JsonArray
            ?? root?["continuationContents"]
                ?["frameworkUpdates"]
                ?["entityBatchUpdate"]
                ?["mutations"] as JsonArray;
        if (mutations is null)
        {
            return;
        }

        foreach (JsonNode? mutation in mutations)
        {
            string payloadType =
                mutation?["payload"] is JsonObject payload && payload.Count > 0
                    ? string.Join('+', payload.Select(static p => p.Key))
                    : "(none)";
            string type = mutation?["type"]?.GetValue<string>() ?? "(no type)";
            _ = _counts.AddOrUpdate($"{payloadType} {type}", 1, static (_, n) => n + 1);
            JsonObject line = new()
            {
                ["at"] = DateTimeOffset.UtcNow.ToString("O"),
                ["tag"] = tag,
                ["liveId"] = liveId,
                ["payloadType"] = payloadType,
                ["mutation"] = mutation?.DeepClone(),
            };
            lock (writer)
            {
                writer.WriteLine(line.ToJsonString());
            }
        }
    }
}
