using System.Text.Json;
using YTLiveChat.Models.Response;
using YTLiveChat.Tests.TestData;

namespace YTLiveChat.Tests.Services;

[TestClass]
public class PollBackoffTests
{
    private static readonly TimeSpan s_fastest = TimeSpan.FromSeconds(1);

    [TestMethod]
    public void NextPollWait_ResponseWithActions_GoesBackToTheFastest()
    {
        string json = UtilityTestData.WrapItemsInLiveChatResponse(
            [$$"""{ "liveChatTextMessageRenderer": {{TextMessageTestData.SimpleTextMessage1()}} }"""],
            "next"
        );

        TimeSpan wait = YTLiveChat.Services.YTLiveChat.NextPollWait(
            TimeSpan.FromSeconds(4),
            s_fastest,
            5000,
            Response(json)
        );

        Assert.AreEqual(s_fastest, wait);
    }

    [TestMethod]
    public void NextPollWait_EmptyResponses_StretchByHalfUpToTheIdleCeiling()
    {
        LiveChatResponse empty = Response(UtilityTestData.WrapItemsInLiveChatResponse([], "next"));
        TimeSpan wait = s_fastest;
        List<double> waits = [];
        for (int i = 0; i < 8; i++)
        {
            wait = YTLiveChat.Services.YTLiveChat.NextPollWait(wait, s_fastest, 5000, empty);
            waits.Add(wait.TotalMilliseconds);
        }

        CollectionAssert.AreEqual(new double[] { 1500, 2250, 3375, 5000, 5000, 5000, 5000, 5000 }, waits);
    }

    [TestMethod]
    public void NextPollWait_YouTubeTimeoutBelowTheIdleCeiling_CapsTheWait()
    {
        LiveChatResponse empty = Response(
            UtilityTestData.WrapItemsInLiveChatResponse([], "next", timeoutMs: 2000)
        );

        TimeSpan wait = YTLiveChat.Services.YTLiveChat.NextPollWait(
            TimeSpan.FromSeconds(4),
            s_fastest,
            5000,
            empty
        );

        Assert.AreEqual(TimeSpan.FromSeconds(2), wait);
    }

    private static LiveChatResponse Response(string json) =>
        JsonSerializer.Deserialize<LiveChatResponse>(json)
        ?? throw new InvalidOperationException("Test response did not deserialize.");
}
