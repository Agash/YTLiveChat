#pragma warning disable CS1591 // Internal request model, not part of public API
using System.Text.Json.Serialization;

namespace YTLiveChat.Models;

public sealed record LiveChatRequest
{
    [JsonPropertyName("context")]
    public required RequestContext Context { get; init; }

    [JsonPropertyName("continuation")]
    public string? Continuation { get; init; }
}

/// <summary>The innertube <c>updated_metadata</c> request: the video first, then the continuation.</summary>
public sealed record MetadataRequest
{
    [JsonPropertyName("context")]
    public required RequestContext Context { get; init; }

    [JsonPropertyName("videoId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VideoId { get; init; }

    [JsonPropertyName("continuation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Continuation { get; init; }
}

/// <summary>The innertube <c>get_live_chat_replay</c> request: the continuation and the player position.</summary>
public sealed record ReplayRequest
{
    [JsonPropertyName("context")]
    public required RequestContext Context { get; init; }

    [JsonPropertyName("continuation")]
    public required string Continuation { get; init; }

    [JsonPropertyName("currentPlayerState")]
    public required PlayerState CurrentPlayerState { get; init; }
}

public sealed record PlayerState
{
    [JsonPropertyName("playerOffsetMs")]
    public required string PlayerOffsetMs { get; init; }
}

/// <summary>The innertube <c>player</c> request: the video whose playback state is asked for.</summary>
public sealed record PlayerRequest
{
    [JsonPropertyName("context")]
    public required RequestContext Context { get; init; }

    [JsonPropertyName("videoId")]
    public required string VideoId { get; init; }
}

public sealed record RequestContext
{
    [JsonPropertyName("client")]
    public required ClientInfo Client { get; init; }
}

public sealed record ClientInfo
{
    [JsonPropertyName("clientVersion")]
    public required string ClientVersion { get; init; }

    [JsonPropertyName("clientName")]
    public required string ClientName { get; init; }

    [JsonPropertyName("hl")]
    public string? Hl { get; init; }

    [JsonPropertyName("gl")]
    public string? Gl { get; init; }
}

#pragma warning restore CS1591
