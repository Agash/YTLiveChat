# YTLiveChat: Your Unofficial Gateway to YouTube Live Chat! 🎉🚀💬

[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/Agash/YTLiveChat/publish.yml?style=flat-square&logo=github&logoColor=white)](https://github.com/Agash/YTLiveChat/actions)
[![NuGet Version](https://img.shields.io/nuget/v/Agash.YTLiveChat.DependencyInjection.svg?style=flat-square&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Agash.YTLiveChat.DependencyInjection/) [![NuGet Version](https://img.shields.io/nuget/v/Agash.YTLiveChat.svg?style=flat-square&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Agash.YTLiveChat/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)

Hey Stream Devs, VTuber Tech Wizards, and Chat Interaction Creators! 👋

Ever wanted to tap into the electrifying buzz of a YouTube live chat without wrestling with the official API quotas or complex authentication flows? Look no further! ✨

**YTLiveChat** is your friendly, lightweight .NET library designed to grab live chat messages directly from YouTube streams, just like your browser does. It uses the internal "InnerTube" API, meaning **no API keys**, **no OAuth dance**, and **no quota headaches**! 🥳

Perfect for:

- Building custom chat overlays 🎨
- Creating chat-driven games and interactions (like ChatPlaysPokemon!) 🎮
- Developing moderation tools 🛡️
- Making cool alerts for Super Chats, new members, or gifted subs! 💸🎁
- Anything else your creative brain cooks up for live streams! 🧠💡

## Features That Sparkle ✨

- ✅ **Access Live Chat Messages:** Get regular messages, Super Chats, Super Stickers, membership events, and more!
- 🚫 **No Official API Key Needed:** Skips the YouTube Data API v3 setup and quota limitations.
- ⚡ **Real-time(ish) Events:** Uses efficient polling to get updates quickly.
- 🗣️ **Parses Message Content:** Breaks down messages into text and emoji parts (including custom channel emojis!).
- 💸 **Super Chat & Sticker Details:** Get amounts, currencies, colors, and sticker images.
- 👑 **Membership Tracking:** Detects new members, milestones, gift purchases (who gifted!), and gift redemptions (who received!).
- 🛡️ **Author Information:** Identifies channel owners, moderators, verified users, and members (with badge info!).
- ⚙️ **Flexible Integration:** Use with standard Dependency Injection or instantiate manually.
- <img src="https://img.shields.io/badge/.NET%20Standard%202.0+-blue?style=flat-square&logo=dotnet" alt=".NET Standard 2.0+"> **Wide Compatibility:** Targets .NET Standard 2.0+, .NET Standard 2.1, and modern .NET (e.g., .NET 9).
- 💖 **Built for Streamers & Devs:** Designed with the needs of interactive streaming applications in mind.

## Installation 🚀

You have two main ways to use this library:

**Option 1: Core Library Only (Recommended for non-DI scenarios like Unity)**

Install the core package. You will need to manage `HttpClient` and `YTLiveChatOptions` yourself.

```bash
dotnet add package Agash.YTLiveChat
```

**Option 2: With Dependency Injection Extensions (Recommended for ASP.NET Core, Generic Host)**

Install the DI extensions package, which automatically includes the core library and helpers for setup.

```bash
dotnet add package Agash.YTLiveChat.DependencyInjection
```

## Usage Examples ⚡

### Using Dependency Injection (Recommended for HostBuilder/ASP.NET Core)

This is the simplest way if you're using `Microsoft.Extensions.DependencyInjection`.

**1. Configure Services:**

In your `Program.cs` or wherever you configure your services (using `IHostBuilder` or `WebApplicationBuilder`):

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using YTLiveChat.Contracts; // Options class
using YTLiveChat.DependencyInjection; // Extension methods

// --- Example using Generic Host Builder ---
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Add YTLiveChat services and configure options from appsettings.json (section "YTLiveChatOptions")
builder.Services.AddYTLiveChat(builder.Configuration);

// Or configure options programmatically
// builder.Services.Configure<YTLiveChatOptions>(options => {
//     options.RequestFrequency = 1500; // Poll every 1.5 seconds
//     options.DebugLogReceivedJsonItems = true; // Log raw items
// });
// builder.Services.AddYTLiveChat(); // Call after configuring options if not passing IConfiguration


// --- Example using IServiceCollection directly (e.g., in Startup.cs) ---
// public void ConfigureServices(IServiceCollection services)
// {
//     // Assuming 'Configuration' is an IConfiguration instance
//     services.AddYTLiveChat(Configuration);
//     // ... other services
// }

// Add your own service that uses IYTLiveChat
builder.Services.AddHostedService<MyChatListenerService>(); // Example listener service

// --- Build and Run ---
var host = builder.Build();
host.Run(); // Or start your application
```

**2. Implement Your Listener:**

Inject `IYTLiveChat` into your service.

```csharp
using YTLiveChat.Contracts.Models;
using YTLiveChat.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq; // For LINQ methods like Select, All
using System.Threading;
using System.Threading.Tasks;

public class MyChatListenerService : IHostedService, IDisposable
{
    private readonly IYTLiveChat _ytLiveChat;
    private readonly ILogger<MyChatListenerService> _logger;
    private bool _disposed = false;

    public MyChatListenerService(IYTLiveChat ytLiveChat, ILogger<MyChatListenerService> logger)
    {
        _ytLiveChat = ytLiveChat;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting listener service.");
        _ytLiveChat.InitialPageLoaded += OnInitialPageLoaded;
        _ytLiveChat.ChatReceived += OnChatReceived;
        _ytLiveChat.ChatStopped += OnChatStopped;
        _ytLiveChat.ErrorOccurred += OnErrorOccurred;

        // Start listening to a specific live stream
        // Replace "YOUR_LIVE_ID" with the actual Video ID
        _ytLiveChat.Start(liveId: "YOUR_LIVE_ID");
        // Alternatively use handle: _ytLiveChat.Start(handle: "@ChannelHandle");
        // Or channelId: _ytLiveChat.Start(channelId: "UCxxxxxxxxxxxxxxx");

        return Task.CompletedTask;
    }

     public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping listener service.");
        // Unsubscribe is important!
        _ytLiveChat.InitialPageLoaded -= OnInitialPageLoaded;
        _ytLiveChat.ChatReceived -= OnChatReceived;
        _ytLiveChat.ChatStopped -= OnChatStopped;
        _ytLiveChat.ErrorOccurred -= OnErrorOccurred;
        _ytLiveChat.Stop(); // Ensure chat stops polling
        return Task.CompletedTask;
    }

    // --- Event Handlers (OnInitialPageLoaded, OnChatReceived, etc.) ---
    private void OnInitialPageLoaded(object? sender, InitialPageLoadedEventArgs e)
    {
        _logger.LogInformation("🎉 Successfully loaded initial chat data for Live ID: {LiveId}", e.LiveId);
    }

    private void OnChatReceived(object? sender, ChatReceivedEventArgs e)
    {
        ChatItem item = e.ChatItem;
        string messagePreview = string.Join("", item.Message.Select(p => p.ToString())); // Simple preview

        _logger.LogInformation("[{Timestamp:HH:mm:ss}] {Author} ({ChannelId}): {Message}",
            item.Timestamp, item.Author.Name, item.Author.ChannelId, messagePreview);

        // --- Check for Special Events ---

        // Super Chat? 💸
        if (item.Superchat != null)
        {
            _logger.LogWarning($"🤑 SUPER CHAT from {item.Author.Name}: {item.Superchat.AmountString}!");
            if (item.Superchat.Sticker != null)
            {
                _logger.LogWarning($"  -> It's a Super Sticker! Alt: {item.Superchat.Sticker.Alt}");
            }
        }

        // Membership Event? 👑🎁
        if (item.MembershipDetails != null)
        {
            var details = item.MembershipDetails;
            switch (details.EventType)
            {
                case MembershipEventType.New:
                    _logger.LogInformation($"✨ NEW MEMBER: Welcome {item.Author.Name} ({details.LevelName})!");
                    break;
                case MembershipEventType.Milestone:
                    _logger.LogInformation($"🎉 MILESTONE: {item.Author.Name} has been a {details.LevelName} member for {details.MilestoneMonths} months!");
                    break;
                case MembershipEventType.GiftPurchase:
                    // NOTE: Author is the GIFTER here!
                    _logger.LogInformation($"🎁 GIFT BOMB! {item.Author.Name} gifted {details.GiftCount} {details.LevelName} memberships!");
                    break;
                case MembershipEventType.GiftRedemption:
                    // NOTE: Author is the RECIPIENT here!
                    _logger.LogInformation($"💝 GIFT RECEIVED: Welcome {item.Author.Name}, you received a {details.LevelName} membership!");
                    break;
            }
        }

        // Is the Author special? 😎
        if (item.IsOwner) _logger.LogInformation($"  -> It's the Channel Owner!");
        if (item.IsModerator) _logger.LogInformation($"  -> It's a Moderator!");
        if (item.IsVerified) _logger.LogInformation($"  -> Verified User!");
        if (item.IsMembership && item.MembershipDetails == null) _logger.LogInformation($"  -> Existing Member ({item.Author.Badge?.Label})!"); // Regular message from existing member
    }

    private void OnChatStopped(object? sender, ChatStoppedEventArgs e)
    {
        _logger.LogWarning("⏹️ Chat listener stopped. Reason: {Reason}", e.Reason ?? "Unknown");
        // Maybe try restarting? Or just clean up.
    }

    private void OnErrorOccurred(object? sender, ErrorOccurredEventArgs e)
    {
        _logger.LogError(e.GetException(), "😱 An error occurred in the chat listener!");
        // Decide if you need to stop or if it might recover.
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _logger.LogDebug("Disposing MyChatListenerService.");
                // Unsubscribe from events to prevent memory leaks if StopAsync wasn't called
                _ytLiveChat.InitialPageLoaded -= OnInitialPageLoaded;
                _ytLiveChat.ChatReceived -= OnChatReceived;
                _ytLiveChat.ChatStopped -= OnChatStopped;
                _ytLiveChat.ErrorOccurred -= OnErrorOccurred;

                // The IYTLiveChat instance itself is managed by DI and disposed by the host
            }
            _disposed = true;
        }
    }
}
```

### Manual Instantiation (Core Library Only - e.g., for Unity)

If you're not using Dependency Injection, you can instantiate the services manually.

```csharp
using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Models;
using YTLiveChat.Contracts.Services;
using YTLiveChat.Services; // Namespace for implementation classes
using Microsoft.Extensions.Logging.Abstractions; // For NullLogger if you don't have logging
using Microsoft.Extensions.Logging; // For ILogger interface
using System.Net.Http; // Required for HttpClient
using System;
using System.Linq; // For LINQ methods
using System.Threading.Tasks; // For Task

public class ManualChatManager : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IYTLiveChat _ytLiveChat;
    // Optional: Use a proper logger if available, otherwise NullLogger
    private readonly ILogger<YTLiveChat.Services.YTLiveChat> _chatLogger = NullLogger<YTLiveChat.Services.YTLiveChat>.Instance;
    private readonly ILogger<YTHttpClient> _httpClientLogger = NullLogger<YTHttpClient>.Instance;
    private bool _disposed = false;


    public ManualChatManager()
    {
        // 1. Configure Options
        var options = new YTLiveChatOptions
        {
            RequestFrequency = 1200, // Custom poll rate
            // YoutubeBaseUrl = "https://www.youtube.com" // Default
        };

        // 2. Create and Configure HttpClient (MANAGE ITS LIFETIME YOURSELF!)
        // IMPORTANT: HttpClient is intended to be long-lived. Create one instance and reuse it.
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri(options.YoutubeBaseUrl)
        };
        // Add any default headers if needed, e.g., User-Agent
        // _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp/1.0");

        // 3. Create YTHttpClient
        var ytHttpClient = new YTHttpClient(_httpClient, _httpClientLogger);

        // 4. Create YTLiveChat Service
        _ytLiveChat = new YTLiveChat.Services.YTLiveChat(options, ytHttpClient, _chatLogger);

        // 5. Subscribe to Events
        _ytLiveChat.InitialPageLoaded += OnInitialPageLoaded;
        _ytLiveChat.ChatReceived += OnChatReceived;
        _ytLiveChat.ChatStopped += OnChatStopped;
        _ytLiveChat.ErrorOccurred += OnErrorOccurred;
    }

    public void StartMonitoring(string liveId)
    {
        Console.WriteLine($"Starting manual monitoring for {liveId}");
        _ytLiveChat.Start(liveId: liveId);
    }

    public void StopMonitoring()
    {
         Console.WriteLine("Stopping manual monitoring");
        _ytLiveChat.Stop();
    }

    // --- Event Handlers ---
    private void OnInitialPageLoaded(object? sender, InitialPageLoadedEventArgs e)
    {
        Console.WriteLine($"[INFO] Initial page loaded for {e.LiveId}");
    }
    private void OnChatReceived(object? sender, ChatReceivedEventArgs e)
    {
        Console.WriteLine($"[CHAT] {e.ChatItem.Author.Name}: {string.Join("", e.ChatItem.Message.Select(p => p.ToString()))}");
         // Add more detailed handling for Superchats, Memberships etc. if needed
    }
    private void OnChatStopped(object? sender, ChatStoppedEventArgs e)
    {
         Console.WriteLine($"[INFO] Chat stopped: {e.Reason ?? "Unknown"}");
    }
    private void OnErrorOccurred(object? sender, ErrorOccurredEventArgs e)
    {
         Console.WriteLine($"[ERROR] {e.GetException()}");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
         if (!_disposed)
        {
            if (disposing)
            {
                Console.WriteLine("Disposing ManualChatManager...");
                // Unsubscribe
                _ytLiveChat.InitialPageLoaded -= OnInitialPageLoaded;
                _ytLiveChat.ChatReceived -= OnChatReceived;
                _ytLiveChat.ChatStopped -= OnChatStopped;
                _ytLiveChat.ErrorOccurred -= OnErrorOccurred;

                // Dispose the core service
                _ytLiveChat.Dispose();

                // Dispose the HttpClient *if* this class owns its lifetime
                _httpClient.Dispose();
            }
            _disposed = true;
        }
    }
}

// --- Example Usage ---
// public static class Program
// {
//     public static async Task Main(string[] args)
//     {
//         var manager = new ManualChatManager();
//         manager.StartMonitoring("YOUR_LIVE_ID"); // Replace with actual ID
//         Console.WriteLine("Monitoring started. Press Enter to stop.");
//         Console.ReadLine(); // Keep running until Enter is pressed
//         manager.StopMonitoring();
//         manager.Dispose();
//         Console.WriteLine("Manager disposed. Exiting.");
//     }
// }
```

## Key Components 🧩

- **`IYTLiveChat`**: The main service interface. Use this for interaction (DI or manual).
  - `Start(handle?, channelId?, liveId?, overwrite?)`: Starts listening. Provide _one_ identifier. `overwrite` controls if a running instance should be stopped first.
  - `Stop()`: Stops the listener gracefully.
  - Events: `InitialPageLoaded`, `ChatReceived`, `ChatStopped`, `ErrorOccurred`.
- **`ChatItem`**: Represents a single received item (message, super chat, membership event, etc.). Contains all the juicy details!
- **`Author`**: Information about the user who sent the item (Name, Channel ID, Thumbnail, Badge).
- **`MessagePart`**: Base class for parts of a message.
  - **`TextPart`**: Plain text segment.
  - **`EmojiPart`**: An emoji (standard or custom), includes image URL and alt text.
- **`Superchat`**: Details about a Super Chat or Super Sticker (amount, currency, colors, sticker info).
- **`MembershipDetails`**: Details about a membership event (type, level name, milestone months, gifter/recipient info).
- **`YTLiveChatOptions`**: Configuration class. Set polling frequency (`RequestFrequency`), base URL, debug logging options. Used via `IOptions<YTLiveChatOptions>` in DI or passed directly in manual setup.

## ⚠️ Important Considerations ⚠️

- **Unofficial API:** This library uses YouTube's internal web API, which is not officially documented or supported for third-party use. YouTube could change it at any time, potentially breaking this library without warning. Use it at your own risk!
- **Be Respectful:** Don't abuse the service. The default request frequency (`RequestFrequency` option, default 1000ms) is reasonable; making it too fast might get your IP temporarily blocked by YouTube.
- **No Sending Messages:** This library is for _reading_ chat only.
- **HttpClient Lifetime:** When using manual instantiation, remember that `HttpClient` is designed for reuse. Create a single instance and pass it to `YTHttpClient`. Do not create a new `HttpClient` for each `YTHttpClient` instance if you create multiple managers.

## Contributing 🤝

Found a bug? Have a feature idea? Feel free to open an issue or submit a pull request! We love contributions!

## License 📄

This project is licensed under the **MIT License**. See the [LICENSE.txt](LICENSE.txt) file for details.

---

Happy Coding, and may your streams be ever interactive! 🎉✨
