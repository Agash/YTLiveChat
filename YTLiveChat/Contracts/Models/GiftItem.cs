namespace YTLiveChat.Contracts.Models;

/// <summary>
/// Represents a virtual gift sent by a viewer using YouTube Jewels.
/// Produced by <c>giftMessageViewModel</c> in an <c>addChatItemAction</c>.
/// </summary>
/// <remarks>
/// YouTube Jewels is a virtual gifting system distinct from gift memberships
/// (<c>liveChatSponsorshipsGiftPurchaseAnnouncementRenderer</c>). Viewers spend
/// Jewels (purchased with real money) to send named gift items to the streamer.
/// </remarks>
public class GiftItem
{
    /// <summary>
    /// Unique identifier for this gift action.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The sender's @handle (e.g. <c>"@yaniescobar2170"</c>).
    /// </summary>
    public required string AuthorHandle { get; set; }

    /// <summary>
    /// The sender's profile picture, when provided by YouTube (~45% of gifts).
    /// </summary>
    public ImagePart? AuthorAvatar { get; set; }

    /// <summary>
    /// The description YouTube writes, e.g. <c>"sent Gold coin"</c>, or <c>"comboed x2 Gold coin for 20 Jewels"</c>
    /// for the same gift sent several times in a row.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// The gift's name as YouTube shows it (e.g. <c>"Gold coin"</c>, <c>"Heart"</c>, <c>"Ramen"</c>), from the gift
    /// image's description or <see cref="Text"/>. Null when neither names it.
    /// </summary>
    public string? GiftItemName { get; set; }

    /// <summary>
    /// The Jewels spent, when YouTube states them: a combo names the total (<c>20</c> from
    /// <c>"comboed x2 Gold coin for 20 Jewels"</c>). A single gift's text does not name its price, so null.
    /// </summary>
    public int? JewelAmount { get; set; }

    /// <summary>
    /// How many of the gift were sent in a row, for a combo (<c>2</c> from <c>"comboed x2 Gold coin for 20 Jewels"</c>);
    /// null for a single gift.
    /// </summary>
    public int? ComboCount { get; set; }

    /// <summary>
    /// The gift's key from its image file (<c>"gold_coin"</c>, <c>"heart"</c>, <c>"sparkles"</c>, <c>"ramen_jp"</c>),
    /// the same in every interface language, where <see cref="GiftItemName"/> is translated. Null when the gift has
    /// no image.
    /// </summary>
    public string? GiftKey { get; set; }

    /// <summary>
    /// Client-side image name for the gift icon (e.g. <c>"GIFT"</c>).
    /// This is an internal YouTube client symbol identifier, not a URL.
    /// All gift types currently share the same generic icon; the specific
    /// item name is only available via <see cref="GiftItemName"/>.
    /// </summary>
    public string? GiftImageName { get; set; }

    /// <summary>
    /// ARGB color tint of the gift icon as a 6-digit uppercase hex string
    /// (e.g. <c>"FF0000"</c>), or <see langword="null"/> when absent.
    /// </summary>
    public string? GiftImageColor { get; set; }

    /// <summary>
    /// CDN image of the gift item from <c>giftMessageViewModel.giftImage</c>.
    /// Present on ~45% of gifts. Distinct from <see cref="GiftImageName"/> which is a
    /// client-side resource symbol identifier.
    /// </summary>
    public ImagePart? GiftImage { get; set; }
}
