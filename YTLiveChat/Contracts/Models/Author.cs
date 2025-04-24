namespace YTLiveChat.Contracts.Models;

/// <summary>
/// Represents the Author of the message
/// </summary>
public class Author
{
    /// <summary>
    /// Public name of the Author
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// ImagePart containing the Authors Thumbnail
    /// </summary>
    public ImagePart? Thumbnail { get; set; }

    /// <summary>
    /// ChannelId if available
    /// </summary>
    public required string ChannelId { get; set; }

    /// <summary>
    /// Current Badge of the Author within the Live Channel
    /// </summary>
    public Badge? Badge { get; set; }
}

/// <summary>
/// Badges available on YouTube for Users
/// </summary>
public class Badge
{
    /// <summary>
    /// Text representation of the Badge
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// ImagePart containing the Badge Thumbnail
    /// </summary>
    public ImagePart? Thumbnail { get; set; }
}
