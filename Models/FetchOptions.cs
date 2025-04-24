namespace YTLiveChat.Models;

internal record FetchOptions
{
    public required string LiveId { get; set; }
    public required string ApiKey { get; set; }
    public required string ClientVersion { get; set; }
    public required string Continuation { get; set; }
}
