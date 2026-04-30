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
    /// Pre-formatted description as supplied by YouTube,
    /// e.g. <c>"sent Gold coin for 10 Jewels"</c>.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// The gift item name, parsed from <see cref="Text"/>
    /// (e.g. <c>"Gold coin"</c>, <c>"Happy poop"</c>, <c>"Floating heart"</c>).
    /// Null when the text does not match the expected pattern.
    /// </summary>
    public string? GiftItemName { get; set; }

    /// <summary>
    /// Number of Jewels spent on this gift, parsed from <see cref="Text"/>
    /// (e.g. <c>10</c> from <c>"sent Gold coin for 10 Jewels"</c>).
    /// Null when the text does not match the expected pattern.
    /// </summary>
    public int? JewelAmount { get; set; }

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
