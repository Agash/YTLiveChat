namespace YTLiveChat.Contracts.Models;

/// <summary>Where a creator goal stands.</summary>
public enum CreatorGoalState
{
    /// <summary>A state this version does not know; see <see cref="CreatorGoalItem.RawState"/>.</summary>
    Unknown,

    /// <summary>The goal is running.</summary>
    Active,

    /// <summary>The goal was reached (YouTube: <c>CREATOR_GOAL_STATE_COMPLETE</c>).</summary>
    Achieved,

    /// <summary>The goal ended (its timer ran out) before it was reached.</summary>
    NotAchieved,
}

/// <summary>
/// A creator goal, such as "10 Super Chats", as YouTube sends it in the chat's entity updates
/// (<c>frameworkUpdates</c> → <c>creatorGoalEntity</c>). Raised whenever the goal changes: when it starts, on each
/// contribution, and when it is reached or ends.
/// </summary>
public class CreatorGoalItem
{
    /// <summary>The goal's id, the same for every update of one goal.</summary>
    public required string GoalId { get; set; }

    /// <summary>The entity key YouTube updates the goal under; the ticker chip names the same key.</summary>
    public required string EntityKey { get; set; }

    /// <summary>Where the goal stands.</summary>
    public CreatorGoalState State { get; set; }

    /// <summary>YouTube's state name as sent (for example <c>CREATOR_GOAL_STATE_ACTIVE</c>).</summary>
    public string? RawState { get; set; }

    /// <summary>How many contributions count towards the goal so far (Super Chats for a Super Chat goal).</summary>
    public long CurrentCount { get; set; }

    /// <summary>How many contributions the goal needs.</summary>
    public long TargetCount { get; set; }

    /// <summary>When the goal's timer ends, if it has one.</summary>
    public DateTimeOffset? EndsAt { get; set; }

    /// <summary>The creator's description of the goal, as written.</summary>
    public string? Description { get; set; }

    /// <summary>YouTube's label for the target, in the requested language (for example "10 Super Chats").</summary>
    public string? TargetText { get; set; }

    /// <summary>Whether the goal counts XP rather than Super Chats.</summary>
    public bool IsXpGoal { get; set; }
}
