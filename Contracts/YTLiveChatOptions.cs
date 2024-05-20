using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public string? YoutubeBaseUrl { get; set; }

    /// <summary>
    /// Frequency of when (in milliseconds) the new batch of chat messages will be requested from YT servers, (i.e. every X milliseconds)
    /// </summary>
    public required int RequestFrequency { get; set; } = 1000;
}
