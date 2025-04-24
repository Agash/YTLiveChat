namespace YTLiveChat.Contracts.Models;

/// <summary>
/// Represents a single item received from the YouTube Live Chat feed.
/// Can be a regular message, Super Chat, Membership event, etc.
/// </summary>
public class ChatItem
{
    /// <summary>
    /// Unique Identifier for the chat item.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Author of the ChatItem (user sending message, making donation, receiving gift, etc.).
    /// Note: For GiftPurchase events, this is the Gifter. For GiftRedemption, this is the Recipient.
    /// </summary>
    public required Author Author { get; set; }

    /// <summary>
    /// Array of message parts (Text, Emoji, Image).
    /// For Super Chats, this represents the user's optional comment.
    /// For Membership events (New/Milestone/Redemption), this often contains the system message from the header/subtext.
    /// For GiftPurchase events, this is typically empty as the main info is in MembershipDetails.
    /// </summary>
    public required MessagePart[] Message { get; set; }

    /// <summary>
    /// Contains Super Chat or Super Sticker details if applicable. Null otherwise.
    /// </summary>
    public Superchat? Superchat { get; set; }

    /// <summary>
    /// Contains Membership event details if applicable (New, Milestone, GiftPurchase, GiftRedemption). Null otherwise.
    /// </summary>
    public MembershipDetails? MembershipDetails { get; set; } // Added

    /// <summary>
    /// Whether or not Author has *any* membership level on the current Live Channel.
    /// Determined primarily by the presence of a membership badge.
    /// </summary>
    public bool IsMembership { get; set; }

    /// <summary>
    /// Whether or not Author is Verified on YouTube.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Whether or not Author is the Owner of the current Live Channel.
    /// </summary>
    public bool IsOwner { get; set; }

    /// <summary>
    /// Whether or not Author is a Moderator of the current Live Channel.
    /// </summary>
    public bool IsModerator { get; set; }

    /// <summary>
    /// Timestamp when the ChatItem was created/received from the API (approximated).
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow; // Consider parsing TimestampUsec later for more accuracy
}
