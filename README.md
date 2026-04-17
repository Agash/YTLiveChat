# YTLiveChat

Unofficial .NET library for reading YouTube live chat via InnerTube (the same web-facing surface YouTube uses), without Data API quotas or OAuth setup.

[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/Agash/YTLiveChat/publish.yml?style=flat-square&logo=github&logoColor=white)](https://github.com/Agash/YTLiveChat/actions)
[![NuGet Version](https://img.shields.io/nuget/v/Agash.YTLiveChat.DependencyInjection.svg?style=flat-square&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Agash.YTLiveChat.DependencyInjection/)
[![NuGet Version](https://img.shields.io/nuget/v/Agash.YTLiveChat.svg?style=flat-square&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Agash.YTLiveChat/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)

## Targets

- `net10.0`
- `net9.0`
- `netstandard2.1`
- `netstandard2.0`

## Install

Core package:

```bash
dotnet add package Agash.YTLiveChat
```

With DI helpers:

```bash
dotnet add package Agash.YTLiveChat.DependencyInjection
```

## What You Get

**Chat messages**
- Chat messages (`ChatReceived`) — text, emoji, images
- Super Chats / Super Stickers with parsed amount + currency
- Membership events — new join, milestone, gift purchase, gift redemption, tier upgrade (`MembershipDetails.EventType`)
- Ticker support (`addLiveChatTickerItemAction`) — paid messages, membership items, gift purchase announcements; ticker items include author channel ID, author thumbnail, and `Author.ChannelHandle` (the `@handle`) when available
- Viewer leaderboard rank extraction via `ChatItem.ViewerLeaderboardRank` (YouTube points crown tags like `#1`)

**Moderation & lifecycle**
- Message deleted (`ChatItemDeleted`) — single item removed
- Author banned/cleared (`ChatItemsDeletedByAuthor`) — all messages from a channel removed
- Message replaced (`ChatItemReplaced`) — slow-mode or placeholder resolution

**Polls**
- `PollUpdated` — fires when a new poll opens (`Poll.IsNew == true`) and on every vote-count update (`IsNew == false`); carries `Question` (`MessagePart[]?`), structured `Choices` (each with `Text: MessagePart[]` and `VoteRatio`), `CreatorHandle`, and `TotalVotes`
- `PollClosed` — fires when the poll panel is dismissed; use `PollId` to correlate with the preceding `PollUpdated` events
- After `PollClosed`, `EngagementMessageReceived` fires with `MessageType == PollResult` carrying the final summary in `Message` (`MessagePart[]`, not required for poll lifecycle tracking)

**Banners**
- `BannerAdded` — fires with a `BannerItem` subclass; pattern-match to distinguish:
  - `PinnedMessageBannerItem` — pinned chat message; carries `Author`, `Message` (`MessagePart[]`), `PinnedBy`, `Timestamp`, and role flags (`IsOwner`, `IsModerator`, `IsVerified`)
  - `CrossChannelRedirectBannerItem` — cross-channel banner; check `RedirectType` (`Redirect` = owner redirecting viewers to another stream, `Raid` = another channel's viewers joining here); carries `RedirectChannelHandle` (the `@handle`), `RedirectVideoId` (non-null only for `Redirect`), and `BannerMessage` (`MessagePart[]`)
  - `ChatSummaryBannerItem` — AI-generated chat summary (experimental YouTube feature); carries `Summary` (`MessagePart[]`) with bold title, deemphasized disclaimer, and body text runs, plus `SummaryId`
- `BannerRemoved` — banner dismissed; `TargetActionId` matches the preceding `BannerAdded`'s `ActionId`

**System / engagement messages**
- `EngagementMessageReceived` — YouTube-generated notices in the chat feed; `Message` is `MessagePart[]`:
  - `CommunityGuidelines` — welcome/guidelines reminder at stream start
  - `SubscribersOnly` — subscribers-only mode notice
  - `PollResult` — formatted poll result summary (see Polls above)

**Raw access**
- `RawActionReceived` — every InnerTube action including unsupported ones
- Async streaming APIs (`StreamChatItemsAsync`, `StreamRawActionsAsync`)

## Important Caveats

- This is an unofficial parser over YouTube’s internal schema. Payloads can change at any time.
- This library reads chat only (no sending messages).
- Respect request frequency to avoid rate limits or temporary blocks.

## Beta API Notice

Continuous livestream monitor mode is currently **BETA/UNSUPPORTED** and can change or break at any time:

- `YTLiveChatOptions.EnableContinuousLivestreamMonitor`
- `YTLiveChatOptions.LiveCheckFrequency`
- `IYTLiveChat.LivestreamStarted`
- `IYTLiveChat.LivestreamEnded`
- `IYTLiveChat.LivestreamInaccessible`

These members intentionally emit compiler warnings via `[Obsolete]` to signal unstable API status.

Monitor note: channel/watch page resolution is fetched via stateless (no-cookie) requests inside the library to reduce consent-interstitial loops during long-running monitor sessions.

## Quick Start (DI)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Services;
using YTLiveChat.DependencyInjection;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddYTLiveChat(builder.Configuration);

builder.Services.Configure<YTLiveChatOptions>(options =>
{
    options.RequestFrequency = 1000;
    options.DebugLogReceivedJsonItems = true;
    options.DebugLogFilePath = "logs/ytlivechat_debug.json";
});

builder.Services.AddHostedService<ChatWorker>();
await builder.Build().RunAsync();
```

Worker example:

```csharp
using YTLiveChat.Contracts.Models;
using YTLiveChat.Contracts.Services;

public sealed class ChatWorker(IYTLiveChat chat) : IHostedService
{
    public Task StartAsync(CancellationToken ct)
    {
        chat.InitialPageLoaded += (_, e) => Console.WriteLine($"Loaded: {e.LiveId}");
        chat.ChatReceived += (_, e) => HandleChat(e.ChatItem);
        chat.RawActionReceived += (_, e) =>
        {
            if (e.ParsedChatItem is null)
            {
                // Unsupported action still available here
                Console.WriteLine("RAW action received.");
            }
        };
        chat.ChatStopped += (_, e) => Console.WriteLine($"Stopped: {e.Reason}");
        chat.ErrorOccurred += (_, e) => Console.WriteLine($"Error: {e.GetException().Message}");

        chat.Start(handle: "@channelHandle");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        chat.Stop();
        return Task.CompletedTask;
    }

    private static void HandleChat(ChatItem item)
    {
        // inspect item.Superchat, item.MembershipDetails, item.ViewerLeaderboardRank, item.IsTicker
        // for ticker events: item.Author.ChannelId, item.Author.Thumbnail, item.Author.ChannelHandle
    }
}
```

## Async Streaming APIs

```csharp
await foreach (ChatItem item in chat.StreamChatItemsAsync(handle: "@channel", cancellationToken: ct))
{
    Console.WriteLine($"{item.Author.Name}: {string.Join("", item.Message.Select(ToText))}");
}

await foreach (RawActionReceivedEventArgs raw in chat.StreamRawActionsAsync(liveId: "videoId", cancellationToken: ct))
{
    if (raw.ParsedChatItem is null)
    {
        Console.WriteLine(raw.RawAction.ToString());
    }
}

static string ToText(MessagePart part) => part switch
{
    TextPart t => t.Text,
    EmojiPart e => e.EmojiText ?? e.Alt ?? "",
    _ => ""
};
```

`TextPart` carries `Bold`, `Italics`, and `IsDeemphasized` flags so consumers can render formatting without re-parsing. All structured message fields across the library (`ChatItem.Message`, `EngagementItem.Message`, `PollChoice.Text`, `PollItem.Question`, banner `Summary`/`BannerMessage`/`Message`) are `MessagePart[]` — concatenate `TextPart.Text` values for plain text, or pattern-match on the flags for rich rendering.

```csharp
```

## Raw JSON Capture for Schema Analysis

Enable:

```csharp
options.DebugLogReceivedJsonItems = true;
options.DebugLogFilePath = "logs/ytlivechat_debug.json";
```

The file is written as a valid JSON array, so it is directly parseable by tools/scripts.

## Example App

`YTLiveChat.Example` includes:

- UTF-8 console setup for multilingual output
- colorized one-line TUI rendering
- rank/ticker/membership/superchat tagging
- unsupported raw action hints
- optional raw JSON capture prompt
- optional continuous monitor mode prompt (beta)

## Current Schema Coverage Gaps

- Creator goals are not mapped yet (awaiting enough stable raw samples).

## Contributing

Bug reports and raw payload samples are highly valuable.  
If you add parser support for new payloads, include:

- response model updates in `YTLiveChat/Models/Response/LiveChatResponse.cs`
- parser updates in `YTLiveChat/Helpers/Parser.cs`
- tests + fixtures in `YTLiveChat.Tests`

## Acknowledgements

- [**WolfwithSword**](https://github.com/WolfwithSword) — testing and feedback

## License

MIT. See `LICENSE.txt`.
