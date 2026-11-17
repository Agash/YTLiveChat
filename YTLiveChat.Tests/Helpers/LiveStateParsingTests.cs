using YTLiveChat.Contracts.Models;
using YTLiveChat.Helpers;
using YTLiveChat.Tests.TestData;

namespace YTLiveChat.Tests.Helpers;

// Streams tabs captured on 2026-11-17 in the lockupViewModel shape, from a German-located request: the texts are
// localized, so these also check that the status does not depend on the language.
[TestClass]
public class LiveStateParsingTests
{
    [TestMethod]
    public void ExtractStreamsFromPage_LiveLockup_IsLiveWithViewers()
    {
        IReadOnlyList<StreamInfo> streams = Parser.ExtractStreamsFromPage(
            WebSnapshotTestData.LoadWebSnapshot("_usadapekora.streams.2026-11-17.html")
        );

        StreamInfo live = streams[0];
        Assert.AreEqual("YW6mbD3_ktI", live.LiveId);
        Assert.AreEqual(StreamStatus.Live, live.Status);
        Assert.AreEqual(25066, live.ViewerCount);
        Assert.IsFalse(string.IsNullOrEmpty(live.Title));
        Assert.IsNotNull(live.ThumbnailUrl);
        Assert.AreEqual(1, streams.Count(s => s.Status == StreamStatus.Live));
    }

    [TestMethod]
    public void ExtractStreamsFromPage_PastLockup_HasDurationAndViews()
    {
        IReadOnlyList<StreamInfo> streams = Parser.ExtractStreamsFromPage(
            WebSnapshotTestData.LoadWebSnapshot("_usadapekora.streams.2026-11-17.html")
        );

        StreamInfo past = streams[1];
        Assert.AreEqual("zFbMAcjuADY", past.LiveId);
        Assert.AreEqual(StreamStatus.Past, past.Status);
        Assert.AreEqual(new TimeSpan(4, 20, 57), past.Duration);
        Assert.AreEqual(584748L, past.ViewCount);
        Assert.IsNotNull(past.PublishedTimeText);
    }

    [TestMethod]
    public void ExtractStreamsFromPage_UpcomingLockup_IsUpcomingWhateverTheLanguage()
    {
        IReadOnlyList<StreamInfo> streams = Parser.ExtractStreamsFromPage(
            WebSnapshotTestData.LoadWebSnapshot("_HakosBaelz.streams.2026-11-17.html")
        );

        StreamInfo upcoming = streams[0];
        Assert.AreEqual("197OEpjj8RI", upcoming.LiveId);
        Assert.AreEqual(StreamStatus.Upcoming, upcoming.Status);
        Assert.IsNull(upcoming.Duration);
        Assert.AreEqual(StreamStatus.Past, streams[1].Status);
    }

    [TestMethod]
    [DataRow("waiting.rUEyequT3ro.2026-11-17.json", false)]
    [DataRow("live.rUEyequT3ro.2026-11-17.json", true)]
    [DataRow("ended.a8PQ2VAohXg.2026-11-17.json", false)]
    public void IsBroadcastingPlayerResponse_CapturedResponse_MatchesTheStreamState(
        string fileName,
        bool broadcasting
    )
    {
        string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Player", fileName));

        Assert.AreEqual(broadcasting, Parser.IsBroadcastingPlayerResponse(json));
    }

    [TestMethod]
    public void DetectInaccessibleLiveReason_ChatTurnedOff_IsChatDisabled()
    {
        // The shape of a live watch page with chat turned off, as served on 2026-11-17.
        const string page = """
            <script>var ytInitialPlayerResponse = {"playabilityStatus":{"status":"OK"}};</script>
            <script>var ytInitialData = {"contents":{"twoColumnWatchNextResults":{"conversationBar":
            {"conversationBarRenderer":{"availabilityMessage":{"messageRenderer":{"text":{"runs":
            [{"text":"Chat is disabled for this live stream."}]}}}}}}}};</script>
            """;

        Assert.AreEqual("chat-disabled", Parser.DetectInaccessibleLiveReason(page));
    }

    // Notices captured 2026-11-17, tracking data removed.
    [TestMethod]
    [DataRow("TAB_SUBSCRIPTIONS", ChatMode.SubscribersOnly)]
    [DataRow("LIVE", ChatMode.LiveCommentary)]
    [DataRow("QUESTION_ANSWER", ChatMode.Open)]
    [DataRow("SOMETHING_NEW", ChatMode.Unknown)]
    public void ToChatModeChange_CapturedNotice_ReadsTheModeFromTheIcon(string iconType, ChatMode mode)
    {
        string json = """
            {"addChatItemAction":{"item":{"liveChatModeChangeMessageRenderer":{
            "id":"ChwKGkNNN2QzTHpsanBnREZVam9jZ2tkbFN3aWt3","timestampUsec":"1794911252203394",
            "icon":{"iconType":"ICON_TYPE"},
            "text":{"runs":[{"text":"@TVN_Chile","bold":true},{"text":" turned on subscribers-only mode","bold":true}]},
            "subtext":{"runs":[{"text":"Only channel subscribers of ","italics":true},{"text":"5 minutes","italics":true},
            {"text":" or longer can send messages","italics":true}]}}},"clientId":"CM7d3LzljpgDFUjocgkdlSwikw"}}
            """;
        YTLiveChat.Models.Response.Action action =
            System.Text.Json.JsonSerializer.Deserialize<YTLiveChat.Models.Response.Action>(
                json.Replace("ICON_TYPE", iconType, StringComparison.Ordinal)
            )!;

        ChatModeChangeItem? change = action.ToChatModeChange();

        Assert.IsNotNull(change);
        Assert.AreEqual(mode, change.Mode);
        Assert.AreEqual(iconType, change.IconType);
        Assert.HasCount(2, change.Text);
        Assert.HasCount(3, change.Subtext);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(1794911252203), change.Timestamp);
    }
}
