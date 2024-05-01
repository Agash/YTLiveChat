namespace YTLiveChat.Contracts.Models
{
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
    }
}
