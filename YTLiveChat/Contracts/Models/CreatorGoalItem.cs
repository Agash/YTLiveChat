namespace YTLiveChat.Contracts.Models;

/// <summary>
/// Represents a Super Chat creator goal ticker chip
/// (<c>showCreatorGoalTickerChipCommand</c>).
/// Emitted whenever the live-chat ticker bar shows or refreshes the creator's goal progress chip.
/// Multiple events for the same goal share the same <see cref="EntityKey"/>.
/// </summary>
/// <remarks>
/// Creator goals are a YouTube feature that lets streamers set a Super Chat donation target
/// visible to viewers in the ticker. The <c>showCreatorGoalTickerChipCommand</c> action type
/// fires once per Super Chat received while a goal is active (and occasionally on its own).
/// </remarks>
public class CreatorGoalItem
{
    /// <summary>
    /// Unique identifier for this ticker chip action instance.
    /// Different chip instances for the same goal share <see cref="EntityKey"/> but have distinct <see cref="Id"/> values.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Entity key that identifies the specific goal across all related actions.
    /// Use this to correlate multiple <c>showCreatorGoalTickerChipCommand</c> events for the same goal.
    /// </summary>
    public required string EntityKey { get; set; }

    /// <summary>
    /// Human-readable goal type, e.g. <c>"Super Chat Goal"</c>.
    /// Derived from the accessibility label (<c>a11yLabel</c>: "See Super Chat goal").
    /// </summary>
    public string? GoalType { get; set; }

    /// <summary>
    /// Progress accessibility label from the engagement panel, e.g.
    /// <c>"Super Chat goal progress: $0 out of $1"</c>.
    /// The placeholder tokens (<c>$0</c>, <c>$1</c>) are filled by the YouTube client at display time.
    /// </summary>
    public string? ProgressLabel { get; set; }

    /// <summary>
    /// Screen-reader label from the ticker chip, e.g. <c>"See Super Chat goal"</c>.
    /// </summary>
    public string? AccessibilityLabel { get; set; }
}
