using System.Text.Json.Nodes;

namespace YTLiveChat.Models.Response;

internal class GetLiveChatResponse
{
    public required JsonObject ResponseContext { get; set; }
    public string? TrackingParams { get; set; }
    public required ContinuationContents ContinuationContents { get; set; }
}
