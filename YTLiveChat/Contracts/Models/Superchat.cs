namespace YTLiveChat.Contracts.Models;

/// <summary>
/// Represents a Super Chat or Super Sticker event.
/// </summary>
public class Superchat
{
    /// <summary>
    /// The raw amount string displayed (e.g., "$5.00", "¥1,000").
    /// </summary>
    public required string AmountString { get; set; }

    /// <summary>
    /// The numeric value of the amount, parsed from AmountString.
    /// Returns 0 if parsing fails.
    /// </summary>
    public decimal AmountValue { get; set; }

    /// <summary>
    /// The ISO 4217 currency code (e.g., "USD", "JPY") parsed from AmountString. A bare "$" is USD and "¥" is JPY,
    /// as YouTube renders them; null when AmountString names no currency this parser recognizes.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// The primary background color hex code (e.g., "1565C0" for blue) for the Super Chat body or sticker background.
    /// </summary>
    public required string BodyBackgroundColor { get; set; }

    /// <summary>
    /// The background color hex code for the Super Chat header. Null for Super Stickers or if unavailable.
    /// </summary>
    public string? HeaderBackgroundColor { get; set; }

    /// <summary>
    /// The text color hex code for the Super Chat header. Null for Super Stickers or if unavailable.
    /// </summary>
    public string? HeaderTextColor { get; set; }

    /// <summary>
    /// The text color hex code for the Super Chat body (user message). Null for Super Stickers or if unavailable.
    /// </summary>
    public string? BodyTextColor { get; set; }

    /// <summary>
    /// The text color hex code for the author's name within the Super Chat or Super Sticker. Null if unavailable.
    /// </summary>
    public string? AuthorNameTextColor { get; set; }

    /// <summary>
    /// If the event is a Super Sticker, contains an ImagePart with sticker details. Null otherwise.
    /// </summary>
    public ImagePart? Sticker { get; set; }
}
