namespace YTLiveChat.Contracts.Models
{
    public abstract class MessagePart { }

    public class ImagePart : MessagePart
    {
        public required string Url { get; set; }
        public string? Alt { get; set; }
    }
    public class EmojiPart : ImagePart
    {
        public required string EmojiText { get; set; }
        public bool IsCustomEmoji { get; set; }
    };
    public class TextPart : MessagePart
    {
        public required string Text { get; set; }
    }
}
