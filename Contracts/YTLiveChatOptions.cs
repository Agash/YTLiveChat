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
}
