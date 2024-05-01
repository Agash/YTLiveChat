namespace YTLiveChat.Contracts.Models
{
    public class Author
    {
        public required string Name { get; set; }
        public ImagePart? Thumbnail { get; set; }
        public string? ChannelId { get; set; }
        public Badge? Badge { get; set; }

    }

    public class Badge
    {
        public required string Label { get; set; }
        public ImagePart? Thumbnail { get; set; }
    }
}
