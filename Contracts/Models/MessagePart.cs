namespace YTLiveChat.Contracts.Models;

/// <summary>
/// Base class for individual message parts
/// </summary>
public abstract class MessagePart { }

/// <summary>
/// Image variant of a message part
/// </summary>
public class ImagePart : MessagePart
{
    /// <summary>
    /// URL of the image
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Alt string of the image
    /// </summary>
    public string? Alt { get; set; }

    /// <summary>
    /// Create a quasi json representation of an ImagePart
    /// </summary>
    /// <returns>String representation of Image in quasi json</returns>
    public override string ToString() => $"{{Image: {{Alt: {Alt}, Url: {Url}}}}}";
}

/// <summary>
/// Emoji variant of a message part
/// </summary>
public class EmojiPart : ImagePart
{
    /// <summary>
    /// Text representation of the emoji
    /// </summary>
    public required string EmojiText { get; set; }

    /// <summary>
    /// Whether or not Emoji is a custom emoji of the channel
    /// </summary>
    public bool IsCustomEmoji { get; set; }

    /// <summary>
    /// Create a quasi json representation of an EmojiPart
    /// </summary>
    /// <returns>String representation of an Emoji in quasi json</returns>
    public override string ToString() =>
        $"{{Emoji: {{EmojiText: {EmojiText}, Alt: {Alt}, Url: {Url}, IsCustomEmoji: {IsCustomEmoji}}}}}";
};

/// <summary>
/// Text variant of a message part
/// </summary>
public class TextPart : MessagePart
{
    /// <summary>
    /// Contained text of the message
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Return the text
    /// </summary>
    /// <returns>string representation of TextPart</returns>
    public override string ToString() => Text;
}
