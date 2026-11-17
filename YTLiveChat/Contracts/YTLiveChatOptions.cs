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
    /// Milliseconds between chat polls while the chat is active: the fastest the chat is polled.
    /// </summary>
    public int RequestFrequency { get; set; } = 1000;

    /// <summary>
    /// The longest wait, in milliseconds, between chat polls while the chat is quiet. Each poll that brings nothing
    /// stretches the wait by half, from <see cref="RequestFrequency"/> up to this (and never past the timeout
    /// YouTube names); the first poll that brings anything goes back to <see cref="RequestFrequency"/>.
    /// </summary>
    public int IdleRequestFrequency { get; set; } = 5000;

    /// <summary>
    /// If true and started with a channel handle/channelId (not direct liveId), keeps monitoring for future livestreams.
    /// When a stream ends, the service continues polling the channel's /live endpoint until the next stream starts.
    /// </summary>
    [Obsolete(
        "BETA/UNSUPPORTED: Continuous livestream monitor mode may change or break at any time and is not covered by semver stability guarantees."
    )]
    public bool EnableContinuousLivestreamMonitor { get; set; } = false;

    /// <summary>
    /// Frequency in milliseconds for checking whether a stream is live: a scheduled stream waiting to start, and
    /// in continuous monitor mode the channel between streams.
    /// </summary>
    public int LiveCheckFrequency { get; set; } = 10000;

    /// <summary>
    /// When enabled, handle/channel auto-detection only accepts streams that are currently broadcasting.
    /// Prevents monitor mode from attaching to scheduled/free-chat placeholders.
    /// Does not affect explicit <c>liveId</c> starts.
    /// </summary>
    [Obsolete(
        "BETA/UNSUPPORTED: Continuous livestream monitor mode may change or break at any time and is not covered by semver stability guarantees."
    )]
    public bool RequireActiveBroadcastForAutoDetectedStream { get; set; } = false;

    /// <summary>
    /// Optional list of stream IDs to ignore during handle/channel auto-detection.
    /// Useful for long-lived free-chat/scheduled streams that should never be selected.
    /// Does not affect explicit <c>liveId</c> starts.
    /// </summary>
    [Obsolete(
        "BETA/UNSUPPORTED: Continuous livestream monitor mode may change or break at any time and is not covered by semver stability guarantees."
    )]
    public List<string> IgnoredAutoDetectedLiveIds { get; set; } = [];

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
