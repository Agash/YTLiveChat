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
    /// If true and started with a channel handle/channelId (not direct liveId), keeps monitoring for future livestreams.
    /// When a stream ends, the service continues polling the channel's /live endpoint until the next stream starts.
    /// </summary>
    public bool EnableContinuousLivestreamMonitor { get; set; } = false;

    /// <summary>
    /// Frequency in milliseconds for checking whether a channel handle/channelId is currently live
    /// while waiting for a stream to start in continuous monitor mode.
    /// </summary>
    public int LiveCheckFrequency { get; set; } = 10000;

    /// <summary>
    /// [DEBUG] If true, logs the raw JSON of received 'AddChatItemAction.Item' objects to the specified file.
    /// This is useful for inspecting the structure of different event types.
    /// Default is false.
    /// </summary>
    public bool DebugLogReceivedJsonItems { get; set; } = false;

    /// <summary>
    /// [DEBUG] The file path where raw JSON items will be logged if DebugLogReceivedJsonItems is true.
    /// Defaults to "ytlivechat_debug_items.json" in the application's base directory.
    /// Uses a single JSON array for easier downstream analysis.
    /// </summary>
    public string DebugLogFilePath { get; set; } =
        Path.Combine(AppContext.BaseDirectory, "ytlivechat_debug_items.json");
}
