namespace YTLiveChat.Models.Response
{
    internal class AuthorName
    {
        public required string SimpleText { get; set; }
    }

    internal class AuthorBadge
    {
        public required AuthorBadgeRenderer LiveChatAuthorBadgeRenderer { get; set; }
    }

    internal class AuthorBadgeRenderer
    {
        public Image? CustomThumbnail { get; set; }
        public Icon? Icon { get; set; }
        public required string Tooltip { get; set; }
        public required Accessibility Accessibility { get; set; }
    }
}
