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
    /// Memberships were gifted (potentially by the author of the containing ChatItem).
    /// Note: This event type might need refinement based on how gifted membership notifications are rendered.
    /// Currently, it primarily relies on parsing the 'headerSubtext'.
    /// </summary>
    GiftedMemberships
}

/// <summary>
/// Contains specific details related to a YouTube membership event.
/// </summary>
public class MembershipDetails
{
    /// <summary>
    /// The type of membership event (New, Milestone, Gift).
    /// </summary>
    public MembershipEventType EventType { get; set; } = MembershipEventType.Unknown;

    /// <summary>
    /// The user-visible name of the membership level or tier.
    /// Extracted from badge tooltips or message text.
    /// </summary>
    public string? LevelName { get; set; }

    /// <summary>
    /// The full text displayed in the membership item's header (e.g., "Member for 6 months", "Welcome!").
    /// </summary>
    public string? HeaderPrimaryText { get; set; }

    /// <summary>
    /// The number of months for a milestone event, parsed from HeaderPrimaryText. Null otherwise.
    /// </summary>
    public int? MilestoneMonths { get; set; }

    /// <summary>
    /// The username of the user who gifted the membership(s).
    /// Only applicable if EventType is GiftedMemberships.
    /// Note: Currently inferred from the ChatItem author if message indicates a gift.
    /// </summary>
    public string? GifterUsername { get; set; }

    /// <summary>
    /// The number of memberships gifted in this event.
    /// Only applicable if EventType is GiftedMemberships.
    /// Note: Currently inferred by parsing HeaderPrimaryText.
    /// </summary>
    public int? GiftCount { get; set; }
}