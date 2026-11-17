namespace YTLiveChat.Contracts.Models;

/// <summary>
/// What YouTube's watch page shows around a live stream, as it updates it (<c>updated_metadata</c>): the viewer
/// count, title, description, date line and likes. Fields YouTube did not send in an update are null; the previous
/// value still holds.
/// </summary>
public class StreamMetadataItem
{
    /// <summary>The stream.</summary>
    public required string LiveId { get; set; }

    /// <summary>Whether YouTube shows the count as concurrent viewers. Also true in a scheduled stream's waiting
    /// room, where the count is the people waiting; <see cref="Services.IYTLiveChat.LivestreamStarted"/> tells the two
    /// apart.</summary>
    public bool? IsLive { get; set; }

    /// <summary>People watching now (or waiting, before the stream starts), while <see cref="IsLive"/>.</summary>
    public long? ViewerCount { get; set; }

    /// <summary>Total views, once the stream is no longer live.</summary>
    public long? ViewCount { get; set; }

    /// <summary>Likes, as the like button counts them for a viewer who has not reacted.</summary>
    public long? LikeCount { get; set; }

    /// <summary>The stream's title, when it changed or was first read.</summary>
    public string? Title { get; set; }

    /// <summary>The stream's description as plain text, when it changed or was first read.</summary>
    public string? Description { get; set; }

    /// <summary>YouTube's date line in the requested language, for example "Started streaming 5 minutes ago".</summary>
    public string? DateText { get; set; }
}
