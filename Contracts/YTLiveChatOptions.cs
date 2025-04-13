namespace YTLiveChat.Contracts;

/// <summary>
/// Options from appsettings.json loaded in DI
/// </summary>
public class YTLiveChatOptions
{
    /// <summary>
    /// Base URL of YouTube
    /// </summary>
    /// <remarks>If not set, during DI initialization it'll default to: "https://www.youtube.com"</remarks>
    public string YoutubeBaseUrl { get; set; } = "https://www.youtube.com";

    /// <summary>
    /// Frequency of when (in milliseconds) the new batch of chat messages will be requested from YT servers, (i.e. every X milliseconds)
    /// </summary>
    public int RequestFrequency { get; set; } = 1000;

    /// <summary>
    /// [DEBUG] If true, logs the raw JSON of received 'AddChatItemAction.Item' objects to the specified file.
    /// This is useful for inspecting the structure of different event types.
    /// Default is false.
    /// </summary>
    public bool DebugLogReceivedJsonItems { get; set; } = false;

    /// <summary>
    /// [DEBUG] The file path where raw JSON items will be logged if DebugLogReceivedJsonItems is true.
    /// Defaults to "ytlivechat_debug_items.jsonl" in the application's base directory.
    /// Uses JSON Lines format (one JSON object per line).
    /// </summary>
    public string DebugLogFilePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "ytlivechat_debug_items.jsonl");
}
