namespace YTLiveChat.Models;

/// <summary>
/// Represents options for fetching live chat data.
/// </summary>
public record FetchOptions
{
    /// <summary>
    /// The ID of the live stream.
    /// </summary>
    public required string LiveId { get; set; }

    /// <summary>
    /// The API key for authentication.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// The client version to be used in the request.
    /// </summary>
    public required string ClientVersion { get; set; }

    /// <summary>
    /// The continuation token for pagination.
    /// </summary>
    public required string Continuation { get; set; }
}
