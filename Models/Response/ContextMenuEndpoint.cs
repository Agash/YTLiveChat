namespace YTLiveChat.Models.Response;

internal class ContextMenuEndpoint
{
    public string? ClickTrackingParams { get; set; }
    public required CommandMetadata CommandMetadata { get; set; }
    public required LiveChatItemContextMenuEndpoint LiveChatItemContextMenuEndpoint { get; set; }
}

internal class CommandMetadata
{
    public required WebCommandMetadataObj WebCommandMetadata { get; set; }
    public class WebCommandMetadataObj
    {
        public bool IgnoreNavigation { get; set; } = true;
    }
}

internal class LiveChatItemContextMenuEndpoint
{
    public required string Params { get; set; }
}
