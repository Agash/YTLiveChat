namespace YTLiveChat.Contracts.Models;

/// <summary>
/// Represents the type of membership event.
/// </summary>
public enum MembershipEventType
{
    /// <summary>
    /// The specific type could not be determined.
    /// </summary>
    Unknown,
    /// <summary>
    /// A user has become a new member.
    /// </summary>
    New,
    /// <summary>
    /// A user has reached a membership duration milestone.
    /// </summary>
    Milestone,
    /// <summary>
    /// A user announced they gifted memberships (from LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer).
    /// The author of the ChatItem is the gifter.
    /// </summary>
    GiftPurchase,
    /// <summary>
    /// A user received a gifted membership (from LiveChatSponsorshipsGiftRedemptionAnnouncementRenderer).
    /// The author of the ChatItem is the recipient.
    /// </summary>
    GiftRedemption
}

/// <summary>
/// Contains specific details related to a YouTube membership event.
/// </summary>
public class MembershipDetails
{
    /// <summary>
    /// The type of membership event (New, Milestone, GiftPurchase, GiftRedemption).
    /// </summary>
    public MembershipEventType EventType { get; set; } = MembershipEventType.Unknown;

    /// <summary>
    /// The user-visible name of the membership level or tier.
    /// Extracted from badge tooltips or message text. Defaults to "Member".
    /// </summary>
    public string LevelName { get; set; } = "Member";

    /// <summary>
    /// The primary text displayed in the membership item's header (e.g., "Member for 6 months", "Gifted 5 memberships", "Welcome!").
    /// </summary>
    public string? HeaderPrimaryText { get; set; }

    /// <summary>
    /// The secondary text displayed (e.g., "New member" from headerSubtext).
    /// </summary>
    public string? HeaderSubtext { get; set; }

    /// <summary>
    /// The number of months for a Milestone event, parsed from HeaderPrimaryText. Null otherwise.
    /// </summary>
    public int? MilestoneMonths { get; set; }

    /// <summary>
    /// The username of the user who gifted the membership(s).
    /// Only applicable if EventType is GiftPurchase. Null otherwise.
    /// Extracted from the author of the gift purchase announcement.
    /// </summary>
    public string? GifterUsername { get; set; }

    /// <summary>
    /// The number of memberships gifted in a GiftPurchase event.
    /// Only applicable if EventType is GiftPurchase. Null otherwise.
    /// Parsed from HeaderPrimaryText (e.g., "Gifted 5 memberships"). Defaults to 1 if parsing fails but text indicates a gift.
    /// </summary>
    public int? GiftCount { get; set; }

    /// <summary>
    /// The username of the user who received the gifted membership.
    /// Only applicable if EventType is GiftRedemption. Null otherwise.
    /// Extracted from the author of the gift redemption announcement.
    /// </summary>
    public string? RecipientUsername { get; set; }
}