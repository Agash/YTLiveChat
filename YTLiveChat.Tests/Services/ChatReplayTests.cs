using Moq;
using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Models;
using YTLiveChat.Helpers;
using YTLiveChat.Models;
using YTLiveChat.Services;
using YTLiveChat.Tests.TestData;

namespace YTLiveChat.Tests.Services;

// A past stream's chat replay captured on 2026-11-17 (trimmed): the watch page's chat renderer, the replay page and
// one get_live_chat_replay page, plus a paid sticker from the same replay.
[TestClass]
public class ChatReplayTests
{
    private const string VideoId = "9vqIZADqR7Y";

    private static string Load(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Replay", name));

    private static string WatchPage => Load($"watch.{VideoId}.2026-11-17.html");

    private static string ReplayPage => Load($"replay-page.{VideoId}.2026-11-17.html");

    private static string ReplayApi => Load($"replay-api.{VideoId}.2026-11-17.json");

    [TestMethod]
    public void GetReplayOptions_PastStream_ReadsReplayTokenKeyAndVersion()
    {
        FetchOptions? options = Parser.GetReplayOptions(WatchPage);

        Assert.IsNotNull(options);
        Assert.AreEqual(VideoId, options.LiveId);
        Assert.IsFalse(string.IsNullOrEmpty(options.ApiKey));
        Assert.IsFalse(string.IsNullOrEmpty(options.ClientVersion));
        Assert.IsFalse(string.IsNullOrEmpty(options.Continuation));
    }

    [TestMethod]
    public void GetReplayOptions_LivePage_IsNull()
    {
        Assert.IsNull(
            Parser.GetReplayOptions(
                WebSnapshotTestData.LoadWebSnapshot("HimemoriLuna.live.2026-02-17.html")
            )
        );
    }

    [TestMethod]
    public void ParseReplayPage_HasBothReplayViewsAndEntries()
    {
        Models.Response.LiveChatContinuation? chat = Parser
            .ParseReplayPage(ReplayPage)
            ?.ContinuationContents?.LiveChatContinuation;

        Assert.IsNotNull(chat);
        List<Models.Response.SubMenuItem>? views = chat.Header
            ?.LiveChatHeaderRenderer
            ?.ViewSelector
            ?.SortFilterSubMenuRenderer
            ?.SubMenuItems;
        Assert.AreEqual(2, views?.Count);
        Assert.IsNotNull(views![1].Continuation?.ReloadContinuationData?.Continuation);
        Assert.IsTrue(chat.Actions!.All(a => a.ReplayChatItemAction is not null));
    }

    [TestMethod]
    public async Task StreamChatReplayAsync_AllMessages_ReadsFromTheLiveChatReplayView()
    {
        Mock<YTHttpClient> http = new(Mock.Of<HttpClient>(), null!);
        http.Setup(h => h.GetOptionsAsync(null, null, VideoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WatchPage);
        http.Setup(h => h.GetReplayPageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReplayPage);
        List<(string Continuation, long Offset)> calls = [];
        http.Setup(h =>
                h.GetLiveChatReplayAsync(
                    It.IsAny<FetchOptions>(),
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (FetchOptions o, long offset, CancellationToken _) =>
                {
                    calls.Add((o.Continuation, offset));
                    return calls.Count == 1
                        ? ReplayApi
                        : """{"continuationContents":{"liveChatContinuation":{}}}""";
                }
            );
        using YTLiveChat.Services.YTLiveChat chat = new(new YTLiveChatOptions(), http.Object);

        List<ReplayChatItem> items = [];
        await foreach (ReplayChatItem item in chat.StreamChatReplayAsync(VideoId))
        {
            items.Add(item);
        }

        Models.Response.LiveChatContinuation page = Parser
            .ParseReplayPage(ReplayPage)!
            .ContinuationContents!.LiveChatContinuation!;
        string liveView = page.Header!
            .LiveChatHeaderRenderer!
            .ViewSelector!
            .SortFilterSubMenuRenderer!
            .SubMenuItems![1]
            .Continuation!
            .ReloadContinuationData!
            .Continuation!;
        Assert.AreEqual(
            liveView,
            calls[0].Continuation,
            "the replay page opens in Top chat; reading starts from the other view"
        );
        Assert.AreEqual(0L, calls[0].Offset);
        Assert.AreEqual(2, calls.Count);
        Assert.IsTrue(
            calls[1].Offset >= 5_981_623L,
            "the next page continues after the last entry read"
        );

        Assert.IsTrue(items.Count >= 5);
        ReplayChatItem message = items.First(i =>
            i.ChatItem?.Message.Length > 0
            && i.VideoOffset > TimeSpan.Zero
            && i.ChatItem.Superchat is null
        );
        Assert.AreEqual(TimeSpan.FromMilliseconds(5_968_286), message.VideoOffset);
        Assert.AreNotEqual(default, message.ChatItem!.Timestamp);
        ReplayChatItem sticker = items.Single(i => i.ChatItem?.Superchat?.Sticker is not null);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1_274_665), sticker.VideoOffset);
    }

    [TestMethod]
    public async Task StreamChatReplayAsync_From_SkipsEarlierEntriesAndSeeksThere()
    {
        Mock<YTHttpClient> http = new(Mock.Of<HttpClient>(), null!);
        http.Setup(h => h.GetOptionsAsync(null, null, VideoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WatchPage);
        http.Setup(h => h.GetReplayPageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReplayPage);
        List<long> offsets = [];
        http.Setup(h =>
                h.GetLiveChatReplayAsync(
                    It.IsAny<FetchOptions>(),
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (FetchOptions _, long offset, CancellationToken _) =>
                {
                    offsets.Add(offset);
                    return offsets.Count == 1
                        ? ReplayApi
                        : """{"continuationContents":{"liveChatContinuation":{}}}""";
                }
            );
        using YTLiveChat.Services.YTLiveChat chat = new(new YTLiveChatOptions(), http.Object);
        TimeSpan from = TimeSpan.FromMilliseconds(5_968_700);

        List<ReplayChatItem> items = [];
        await foreach (ReplayChatItem item in chat.StreamChatReplayAsync(VideoId, from))
        {
            items.Add(item);
        }

        Assert.AreEqual((long)from.TotalMilliseconds, offsets[0]);
        Assert.IsTrue(items.Count > 0);
        Assert.IsTrue(items.All(i => i.VideoOffset >= from));
    }

    [TestMethod]
    public async Task StreamChatReplayAsync_LiveStream_Throws()
    {
        Mock<YTHttpClient> http = new(Mock.Of<HttpClient>(), null!);
        http.Setup(h => h.GetOptionsAsync(null, null, "x", It.IsAny<CancellationToken>()))
            .ReturnsAsync(WebSnapshotTestData.LoadWebSnapshot("HimemoriLuna.live.2026-02-17.html"));
        using YTLiveChat.Services.YTLiveChat chat = new(new YTLiveChatOptions(), http.Object);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await foreach (ReplayChatItem _ in chat.StreamChatReplayAsync("x")) { }
        });
    }
}
