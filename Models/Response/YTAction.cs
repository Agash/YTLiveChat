using System.Text.Json.Nodes;

namespace YTLiveChat.Models.Response;

internal class YTAction
{
    public AddChatItemAction? AddChatItemAction { get; set; }
    public JsonObject? AddLiveChatTickerItemAction { get; set; }
}

internal class AddChatItemAction
{
    public required ItemObj Item { get; set; }
    public required string ClientId { get; set; }
    public class ItemObj
    {
        public LiveChatTextMessageRenderer? LiveChatTextMessageRenderer { get; set; }
        public LiveChatPaidMessageRenderer? LiveChatPaidMessageRenderer { get; set; }
        public LiveChatMembershipItemRenderer? LiveChatMembershipItemRenderer { get; set; }
        public LiveChatPaidStickerRenderer? LiveChatPaidStickerRenderer { get; set; }
        public JsonObject? LiveChatViewerEngagementMessageRenderer { get; set; }
    }
}
