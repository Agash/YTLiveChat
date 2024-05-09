namespace YTLiveChat.Models;

internal class FetchOptions
{
    public required string LiveId { get; set; }
    public required string ApiKey { get; set; }
    public required string ClientVersion { get; set; }
    public required string Continuation { get; set; }
}
