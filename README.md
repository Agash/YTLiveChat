# YTLiveChat: Your Unofficial Gateway to YouTube Live Chat! 🎉🚀💬

[![NuGet Version](https://img.shields.io/nuget/v/Agash.YTLiveChat.svg?style=flat-square)](https://www.nuget.org/packages/Agash.YTLiveChat/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)
<!-- Add Build Status Badge Here -->
<!-- [![Build Status](https://dev.azure.com/your-org/your-project/_apis/build/status/your-build-definition?branchName=main)](https://dev.azure.com/your-org/your-project/_build/latest?definitionId=your-build-definition&branchName=main) -->

Hey Stream Devs, VTuber Tech Wizards, and Chat Interaction Creators! 👋

Ever wanted to tap into the electrifying buzz of a YouTube live chat without wrestling with the official API quotas or complex authentication flows? Look no further! ✨

**YTLiveChat** is your friendly, lightweight .NET library designed to grab live chat messages directly from YouTube streams, just like your browser does. It uses the internal "InnerTube" API, meaning **no API keys**, **no OAuth dance**, and **no quota headaches**! 🥳

Perfect for:

*   Building custom chat overlays 🎨
*   Creating chat-driven games and interactions (like ChatPlaysPokemon!) 🎮
*   Developing moderation tools 🛡️
*   Making cool alerts for Super Chats, new members, or gifted subs! 💸🎁
*   Anything else your creative brain cooks up for live streams! 🧠💡

## Features That Sparkle ✨

*   ✅ **Access Live Chat Messages:** Get regular messages, Super Chats, Super Stickers, membership events, and more!
*   🚫 **No Official API Key Needed:** Skips the YouTube Data API v3 setup and quota limitations.
*   ⚡ **Real-time(ish) Events:** Uses efficient polling to get updates quickly.
*   🗣️ **Parses Message Content:** Breaks down messages into text and emoji parts (including custom channel emojis!).
*   💸 **Super Chat & Sticker Details:** Get amounts, currencies, colors, and sticker images.
*   👑 **Membership Tracking:** Detects new members, milestones, gift purchases (who gifted!), and gift redemptions (who received!).
*   🛡️ **Author Information:** Identifies channel owners, moderators, verified users, and members (with badge info!).
*   🛠️ **Easy .NET Integration:** Simple setup using standard Dependency Injection.
*   💖 **Built for Streamers & Devs:** Designed with the needs of interactive streaming applications in mind.

## Get Started in a Flash ⚡

It's super easy to integrate YTLiveChat into your .NET application.

**1. Install the Package:**

```bash
dotnet add package Agash.YTLiveChat
```

**2. Configure Services (using Dependency Injection):**

In your `Program.cs` or wherever you configure your services:

```csharp
using YTLiveChat.Contracts; // <-- Add this using!
using Microsoft.Extensions.Hosting;

// --- Example using Generic Host Builder ---
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Add YTLiveChat and configure options if needed (optional)
builder.AddYTLiveChat();
// builder.Services.Configure<YTLiveChatOptions>(builder.Configuration.GetSection("MyYtChatSettings")); // Optional: Configure from appsettings.json

// --- Example using IServiceCollection directly (e.g., in ASP.NET Core) ---
// var builder = WebApplication.CreateBuilder(args);
// builder.Services.AddYTLiveChat(builder.Configuration); // Pass configuration

// ... your other service registrations

var host = builder.Build();

// Resolve and use IYTLiveChat somewhere in your app!
// var chatService = host.Services.GetRequiredService<IYTLiveChat>();
// chatService.Start(liveId: "YOUR_YOUTUBE_LIVE_VIDEO_ID");

// host.Run(); // Or start your application
```

**3. Listen for Chat Magic!**

Inject `IYTLiveChat` into your service or class and start listening:

```csharp
using YTLiveChat.Contracts.Models;
using YTLiveChat.Contracts.Services;
using Microsoft.Extensions.Logging; // Assuming you have logging setup

public class MyChatListenerService : IDisposable // Example Service
{
    private readonly IYTLiveChat _ytLiveChat;
    private readonly ILogger<MyChatListenerService> _logger;

    public MyChatListenerService(IYTLiveChat ytLiveChat, ILogger<MyChatListenerService> logger)
    {
        _ytLiveChat = ytLiveChat;
        _logger = logger;

        // Subscribe to the events! ✨
        _ytLiveChat.InitialPageLoaded += OnInitialPageLoaded;
        _ytLiveChat.ChatReceived += OnChatReceived;
        _ytLiveChat.ChatStopped += OnChatStopped;
        _ytLiveChat.ErrorOccurred += OnErrorOccurred;
    }

    public void StartListening(string videoId)
    {
        _logger.LogInformation("Starting YouTube Live Chat listener for Video ID: {VideoId}", videoId);
        // You can start by handle ("@ChannelHandle"), channelId ("UC...") or liveId ("VIDEO_ID")
        _ytLiveChat.Start(liveId: videoId);
    }

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
                    _logger.LogInformation($"🎉 MILESTONE: {item.Author.Name} has been a {details.LevelName} for {details.MilestoneMonths} months!");
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
        _logger.LogInformation("Disposing Chat Listener Service and stopping chat.");
        _ytLiveChat.Stop(); // Ensure listener is stopped
        // Unsubscribe (important to prevent memory leaks!)
        _ytLiveChat.InitialPageLoaded -= OnInitialPageLoaded;
        _ytLiveChat.ChatReceived -= OnChatReceived;
        _ytLiveChat.ChatStopped -= OnChatStopped;
        _ytLiveChat.ErrorOccurred -= OnErrorOccurred;
        _ytLiveChat.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

## Key Components 🧩

*   **`IYTLiveChat`**: The main service interface. Inject this!
    *   `Start(handle?, channelId?, liveId?, overwrite?)`: Starts listening. Provide *one* identifier.
    *   `Stop()`: Stops the listener.
    *   Events: `InitialPageLoaded`, `ChatReceived`, `ChatStopped`, `ErrorOccurred`.
*   **`ChatItem`**: Represents a single received item (message, super chat, etc.). Contains all the juicy details!
*   **`Author`**: Information about the user who sent the item (Name, Channel ID, Thumbnail, Badge).
*   **`MessagePart`**: Base class for parts of a message.
    *   **`TextPart`**: Plain text segment.
    *   **`EmojiPart`**: An emoji (standard or custom), includes image URL.
*   **`Superchat`**: Details about a Super Chat or Super Sticker (amount, currency, colors, sticker info).
*   **`MembershipDetails`**: Details about a membership event (type, level name, milestone months, gifter/recipient info).
*   **`YTLiveChatOptions`**: Configuration class (optional, use via `IOptions<YTLiveChatOptions>`). Set polling frequency (`RequestFrequency`) etc.

## ⚠️ Important Considerations ⚠️

*   **Unofficial API:** This library uses YouTube's internal web API, which is not officially documented or supported for third-party use. YouTube could change it at any time, potentially breaking this library without warning. Use it at your own risk!
*   **Be Respectful:** Don't abuse the service. The default request frequency is reasonable; making it too fast might get your IP temporarily blocked by YouTube.
*   **No Sending Messages:** This library is for *reading* chat only.

## Contributing 🤝

Found a bug? Have a feature idea? Feel free to open an issue or submit a pull request! We love contributions!

## License 📄

This project is licensed under the **MIT License**. See the [LICENSE.txt](LICENSE.txt) file for details.

---

Happy Coding, and may your streams be ever interactive! 🎉✨