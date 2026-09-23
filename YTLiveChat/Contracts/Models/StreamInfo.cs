namespace YTLiveChat.Contracts.Models;

/// <summary>Live status of a stream entry.</summary>
public enum StreamStatus
{
    /// <summary>Currently broadcasting.</summary>
    Live,

    /// <summary>Scheduled for a future date.</summary>
    Upcoming,

    /// <summary>Finished broadcast (replay available).</summary>
    Past,
}

/// <summary>
/// Details for a single stream entry from a channel's streams page.
/// Returned by <see cref="Services.IYTLiveChat.GetStreamsAsync"/>.
/// </summary>
public class StreamInfo
{
    /// <summary>YouTube video ID.</summary>
    public required string LiveId { get; set; }

    /// <summary>Stream title.</summary>
    public required string Title { get; set; }

    /// <summary>Best available thumbnail URL, or null if unavailable.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Live status: Live, Upcoming, or Past.</summary>
    public StreamStatus Status { get; set; }

    /// <summary>
    /// Current viewer or waiting count.
    /// Non-null for Live (viewers) and Upcoming (waiting) streams.
    /// Null for Past streams.
    /// </summary>
    public int? ViewerCount { get; set; }

    /// <summary>
    /// Total view count for Past streams. Null for Live and Upcoming.
    /// </summary>
    public long? ViewCount { get; set; }

    /// <summary>
    /// Scheduled broadcast time for Upcoming streams (UTC). Null for Live and Past.
    /// </summary>
    public DateTimeOffset? ScheduledAt { get; set; }

    /// <summary>
    /// Relative published time text as returned by YouTube (e.g. "3 days ago").
    /// Populated for Past streams. Null for Live and Upcoming.
    /// </summary>
    public string? PublishedTimeText { get; set; }

    /// <summary>
    /// Stream duration for Past streams. Null for Live and Upcoming.
    /// </summary>
    public TimeSpan? Duration { get; set; }
}
