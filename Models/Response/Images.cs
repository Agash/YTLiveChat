namespace YTLiveChat.Models.Response;

internal class Thumbnail
{
    public required string Url { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}

internal class Icon
{
    public required string IconType { get; set; }
}

internal class Image
{
    public Thumbnail[] Thumbnails { get; set; } = [];
}

internal class ImageWithAccessibility : Image
{
    public required Accessibility Accessibility { get; set; }
}
