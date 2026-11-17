using System.Text.Json;

namespace YTLiveChat.Contracts.Models;

/// <summary>
/// One entity update from a chat response (<c>frameworkUpdates.entityBatchUpdate.mutations</c>). YouTube keeps live
/// state there rather than in chat actions: creator goals, reactions, poll state, Super Chat reply counts, the viewer
/// leaderboard and more. The library types the ones it knows (<see cref="CreatorGoalItem"/>,
/// <see cref="ReactionsItem"/>); every update is also available raw.
/// </summary>
public class EntityMutation
{
    /// <summary>The key the entity is stored under; later updates of the same entity carry the same key.</summary>
    public required string EntityKey { get; set; }

    /// <summary>The payload's type, for example <c>creatorGoalEntity</c> or <c>emojiFountainDataEntity</c>.</summary>
    public required string PayloadType { get; set; }

    /// <summary>Whether the entity was removed rather than replaced.</summary>
    public bool IsDelete { get; set; }

    /// <summary>The payload as sent; <see cref="JsonValueKind.Undefined"/> for a removal.</summary>
    public JsonElement Payload { get; set; }
}

/// <summary>
/// Reactions viewers sent (the hearts and emoji floating over the player), as YouTube sums them
/// (<c>emojiFountainDataEntity</c>): one bucket per stretch of time, most recent last.
/// </summary>
public class ReactionsItem
{
    /// <summary>The entity key the reactions are updated under.</summary>
    public required string EntityKey { get; set; }

    /// <summary>When YouTube computed these buckets.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>The buckets, oldest first.</summary>
    public required IReadOnlyList<ReactionBucket> Buckets { get; set; }
}

/// <summary>Reactions within one stretch of time.</summary>
public class ReactionBucket
{
    /// <summary>How many reactions were sent.</summary>
    public long TotalReactions { get; set; }

    /// <summary>The stretch of time the bucket covers.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>YouTube's intensity score for the bucket (how lively the player overlay shows it).</summary>
    public int IntensityScore { get; set; }
}

/// <summary>
/// How many viewers replied to a Super Chat (<c>replyCountEntity</c>); <see cref="EntityKey"/> matches the Super
/// Chat's <see cref="Superchat.ReplyCountEntityKey"/>.
/// </summary>
public class ReplyCountItem
{
    /// <summary>The entity key, the same as on the Super Chat.</summary>
    public required string EntityKey { get; set; }

    /// <summary>The number of replies.</summary>
    public long Count { get; set; }
}
