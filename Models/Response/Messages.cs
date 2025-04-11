﻿using System.Text.Json.Serialization;
using YTLiveChat.Helpers;

namespace YTLiveChat.Models.Response;

[JsonConverter(typeof(MessageRunConverter))]
internal abstract class MessageRun
{
}

internal class MessageText : MessageRun
{
    public required string Text { get; set; }
}

internal class MessageEmoji : MessageRun
{
    public required EmojiObj Emoji { get; set; }
    public class EmojiObj
    {
        public required string EmojiId { get; set; }
        public string[] Shortcuts { get; set; } = [];
        public string[] SearchTerms { get; set; } = [];
        public bool SupportsSkinTones { get; set; } = false;
        public ImageWithAccessibility? Image { get; set; }
        public string[] VariantIds { get; set; } = [];
        public bool IsCustomEmoji { get; set; } = false;
    }
}

internal class MessageRendererBase
{
    public required string Id { get; set; }
    public AuthorName? AuthorName { get; set; }
    public required Image AuthorPhoto { get; set; }
    public AuthorBadge[]? AuthorBadges { get; set; }
    public required string AuthorExternalChannelId { get; set; }
    public required ContextMenuEndpoint ContextMenuEndpoint { get; set; }
    public required Accessibility ContextMenuAccessibility { get; set; }
    public required string TimestampUsec { get; set; }
}

internal class MessageRuns
{
    public MessageRun[] Runs { get; set; } = [];
}

internal class LiveChatTextMessageRenderer : MessageRendererBase
{
    // Not required because LiveChatPaidMessages are ChatTextMessages but don't always have a MessageRun
    public MessageRuns? Message { get; set; }
}

internal class PurchaseAmountText
{
    public required string SimpleText { get; set; }
}

internal class LiveChatPaidMessageRenderer : LiveChatTextMessageRenderer
{
    public required PurchaseAmountText PurchaseAmountText { get; set; }
    public required long HeaderBackgroundColor { get; set; }
    public required long HeaderTextColor { get; set; }
    public required long BodyBackgroundColor { get; set; }
    public required long BodyTextColor { get; set; }
    public required long AuthorNameTextColor { get; set; }
}

internal class LiveChatPaidStickerRenderer : MessageRendererBase
{
    public required PurchaseAmountText PurchaseAmountText { get; set; }
    public required ImageWithAccessibility Sticker { get; set; }
    public required long MoneyChipBackgroundColor { get; set; }
    public required long MoneyChipTextColor { get; set; }
    public required long StickerDisplayWidth { get; set; }
    public required long StickerDisplayHeight { get; set; }
    public required long BackgroundColor { get; set; }
    public required long AuthorNameTextColor { get; set; }
}

internal class LiveChatMembershipItemRenderer : MessageRendererBase
{
    public required MessageRuns HeaderSubtext { get; set; }
}
