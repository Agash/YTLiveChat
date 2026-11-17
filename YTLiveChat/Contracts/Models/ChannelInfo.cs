namespace YTLiveChat.Contracts.Models;

/// <summary>A channel's profile as its page shows it to someone not signed in.</summary>
public class ChannelInfo
{
    /// <summary>The channel id (<c>UC...</c>).</summary>
    public required string ChannelId { get; set; }

    /// <summary>The handle, with its <c>@</c>.</summary>
    public string? Handle { get; set; }

    /// <summary>The channel's name.</summary>
    public required string Title { get; set; }

    /// <summary>The full "About" description.</summary>
    public string? Description { get; set; }

    /// <summary>The largest avatar image.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>The largest banner image.</summary>
    public string? BannerUrl { get; set; }

    /// <summary>
    /// Subscribers as YouTube rounds them for display (three significant digits, for example 1,160,000 for
    /// "1.16M"), the same precision the YouTube Data API gives; null when the channel hides the count.
    /// </summary>
    public long? SubscriberCount { get; set; }

    /// <summary>YouTube's subscriber text, for example "1.16M subscribers".</summary>
    public string? SubscriberCountText { get; set; }

    /// <summary>Public videos.</summary>
    public long? VideoCount { get; set; }

    /// <summary>Whether the name carries YouTube's verified check mark.</summary>
    public bool IsVerified { get; set; }

    /// <summary>Whether the name carries the official artist badge.</summary>
    public bool IsOfficialArtist { get; set; }

    /// <summary>The channel's keywords, as one space-separated string.</summary>
    public string? Keywords { get; set; }

    /// <summary>The first link the channel shows under its name.</summary>
    public string? Link { get; set; }
}
