namespace YTLiveChat.Contracts.Models;

/// <summary>
/// One entry of a past stream's chat replay: a chat item (message, Super Chat, sticker, membership, gift
/// membership) or a Jewels gift, and where in the video it happened. The items keep the time they were sent, so
/// they can be placed on the real timeline; messages sent in the waiting room sit at offset zero.
/// </summary>
public class ReplayChatItem
{
    /// <summary>Where in the video the entry appears.</summary>
    public TimeSpan VideoOffset { get; set; }

    /// <summary>The chat item, when the entry is one.</summary>
    public ChatItem? ChatItem { get; set; }

    /// <summary>The Jewels gift, when the entry is one.</summary>
    public GiftItem? Gift { get; set; }
}
