namespace YTLiveChat.Contracts.Models;

/// <summary>Who may write in the chat after a mode change.</summary>
public enum ChatMode
{
    /// <summary>A mode this version does not recognize; see <see cref="ChatModeChangeItem.IconType"/>.</summary>
    Unknown,

    /// <summary>Anyone can write again (a restricting mode was turned off).</summary>
    Open,

    /// <summary>Only subscribers (of at least some time, named in the subtext) can write.</summary>
    SubscribersOnly,

    /// <summary>Live commentary: only viewers the channel owner selected can write.</summary>
    LiveCommentary,
}

/// <summary>
/// The chat's mode changed (<c>liveChatModeChangeMessageRenderer</c>), shown in chat as a notice. The mode is read
/// from the notice's icon, which does not depend on the language; the texts are YouTube's, in the requested language.
/// </summary>
public class ChatModeChangeItem
{
    /// <summary>The notice's id.</summary>
    public required string Id { get; set; }

    /// <summary>When the mode changed.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>The mode, from the icon. Observed 2026-11-17: <c>TAB_SUBSCRIPTIONS</c> for subscribers-only,
    /// <c>LIVE</c> for live commentary, <c>QUESTION_ANSWER</c> for open again.</summary>
    public ChatMode Mode { get; set; }

    /// <summary>The icon YouTube shows with the notice, as sent.</summary>
    public string? IconType { get; set; }

    /// <summary>The notice, for example "@channel turned on subscribers-only mode".</summary>
    public required MessagePart[] Text { get; set; }

    /// <summary>The explanation under it, for example "Only channel subscribers of 5 minutes or longer can send messages".</summary>
    public required MessagePart[] Subtext { get; set; }
}
