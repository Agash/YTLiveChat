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
}
#pragma warning restore CS1591
