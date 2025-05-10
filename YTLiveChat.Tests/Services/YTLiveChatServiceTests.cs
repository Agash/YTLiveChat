using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Models;
using YTLiveChat.Contracts.Services;
using YTLiveChat.Models;
using YTLiveChat.Models.Response;
using YTLiveChat.Services;
using YTLiveChat.Tests.TestData;

namespace YTLiveChat.Tests.Services;

[TestClass]
public class YTLiveChatServiceTests
{
    private const int TestRequestFrequencyMilliseconds = 100;
    private const int DefaultTestTimeoutSeconds = 7;

    private Mock<YTHttpClient> _mockYtHttpClient = null!;
    private YTLiveChatOptions _ytLiveChatOptions = null!;
    private Mock<ILogger<YTLiveChat.Services.YTLiveChat>> _mockLogger = null!;
    private YTLiveChat.Services.YTLiveChat _service = null!;
    private CancellationTokenSource _testMethodCts = null!;

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _testMethodCts = new CancellationTokenSource();
        _ytLiveChatOptions = new YTLiveChatOptions
        {
            RequestFrequency = TestRequestFrequencyMilliseconds,
            DebugLogReceivedJsonItems = false,
        };
        _mockLogger = new Mock<ILogger<YTLiveChat.Services.YTLiveChat>>();
        _mockYtHttpClient = new Mock<YTHttpClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<ILogger<YTHttpClient>>()
        );
        _service = new YTLiveChat.Services.YTLiveChat(
            _ytLiveChatOptions,
            _mockYtHttpClient.Object,
            _mockLogger.Object
        );
        _mockLogger.Object.LogInformation(
            "TEST: TestInitialize completed for test: {TestName}",
            TestContext.TestName
        );
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        _mockLogger.Object.LogInformation(
            "TEST: TestCleanup started for test: {TestName}",
            TestContext.TestName
        );

        _testMethodCts.Cancel();
        _testMethodCts.Dispose();

        _service.Dispose();

        try
        {
            await Task.Delay(TestRequestFrequencyMilliseconds * 2, CancellationToken.None);
        }
        catch (TaskCanceledException)
        { /* Expected if test itself timed out */
        }

        _mockLogger.Object.LogInformation(
            "TEST: TestCleanup completed for test: {TestName}",
            TestContext.TestName
        );
    }

    private async Task<T> WaitForTcsResult<T>(TaskCompletionSource<T> tcs, string eventName)
    {
        try
        {
            return await tcs.Task.WaitAsync(
                TimeSpan.FromSeconds(DefaultTestTimeoutSeconds),
                _testMethodCts.Token
            );
        }
        catch (TimeoutException)
        {
            _mockLogger.Object.LogError(
                "[{TestName}] Timeout waiting for {EventName}.",
                TestContext.TestName,
                eventName
            );
            throw;
        }
        catch (OperationCanceledException) when (_testMethodCts.IsCancellationRequested)
        {
            _mockLogger.Object.LogWarning(
                "[{TestName}] Test method cancelled while waiting for {EventName}.",
                TestContext.TestName,
                eventName
            );
            throw;
        }
    }

    [TestMethod]
    public async Task Start_SuccessfulInitializationAndFirstPoll_EventsFired()
    {
        string liveId = "testLiveId_InitAndPoll_01";
        string apiKey = "apiKey_InitAndPoll_01";
        string clientVersion = "clientV_InitAndPoll_01";
        string initialContinuationFromHtml = "initialCont_FromHtml_01";
        string nextContinuationAfterPoll = "nextCont_AfterPoll_01";

        _mockLogger.Object.LogInformation("[{TestName}] Setting up mocks.", TestContext.TestName);

        string pageHtml = UtilityTestData.GetSampleLivePageHtml(
            liveId,
            apiKey,
            clientVersion,
            initialContinuationFromHtml
        );

        string itemRendererContentJson = TextMessageTestData.SimpleTextMessage1();
        string itemObjectJson =
            $$"""{ "liveChatTextMessageRenderer": {{itemRendererContentJson}} }""";

        string responseJsonForFirstPoll = UtilityTestData.WrapItemsInLiveChatResponse(
            [itemObjectJson],
            nextContinuationAfterPoll
        );
        LiveChatResponse? liveChatResponseForFirstPoll =
            JsonSerializer.Deserialize<LiveChatResponse>(responseJsonForFirstPoll);
        Assert.IsNotNull(liveChatResponseForFirstPoll, "Deserialization of mock response failed.");

        _mockYtHttpClient
            .Setup(client =>
                client.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(pageHtml);

        _mockYtHttpClient
            .Setup(client =>
                client.GetLiveChatAsync(
                    It.Is<FetchOptions>(fo =>
                        fo.LiveId == liveId
                        && fo.ApiKey == apiKey
                        && fo.ClientVersion == clientVersion
                        && fo.Continuation == initialContinuationFromHtml
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((liveChatResponseForFirstPoll, responseJsonForFirstPoll));

        TaskCompletionSource<InitialPageLoadedEventArgs> initialPageLoadedTcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        TaskCompletionSource<ChatReceivedEventArgs> chatReceivedTcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        _service.InitialPageLoaded += (sender, eventArgs) =>
            initialPageLoadedTcs.TrySetResult(eventArgs);
        _service.ChatReceived += (sender, eventArgs) => chatReceivedTcs.TrySetResult(eventArgs);

        _service.Start(liveId: liveId);

        InitialPageLoadedEventArgs initialArgs = await WaitForTcsResult(
            initialPageLoadedTcs,
            "InitialPageLoaded"
        );
        Assert.IsNotNull(initialArgs);
        Assert.AreEqual(liveId, initialArgs.LiveId);

        ChatReceivedEventArgs receivedArgs = await WaitForTcsResult(
            chatReceivedTcs,
            "ChatReceived"
        );
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual("MSG_ID_SIMPLE_01", receivedArgs.ChatItem.Id);

        _mockYtHttpClient.Verify(
            client => client.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _mockYtHttpClient.Verify(
            client =>
                client.GetLiveChatAsync(
                    It.Is<FetchOptions>(fo =>
                        fo.LiveId == liveId
                        && fo.ApiKey == apiKey
                        && fo.ClientVersion == clientVersion
                        && fo.Continuation == initialContinuationFromHtml
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once,
            "First GetLiveChatAsync call was not made with expected FetchOptions."
        );
    }

    [TestMethod]
    public async Task Start_GetOptionsAsyncThrows_ErrorEventFiredAndChatStopped()
    {
        string liveId = "failLiveId002";
        HttpRequestException httpException = new(
            "Simulated network failure during GetOptionsAsync"
        );
        string expectedInnerExceptionMessage =
            $"Failed to initialize from YouTube page: {httpException.Message}";

        _mockYtHttpClient
            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(httpException);

        TaskCompletionSource<ErrorOccurredEventArgs> errorTcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        TaskCompletionSource<ChatStoppedEventArgs> stoppedTcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _service.ErrorOccurred += (s, e) => errorTcs.TrySetResult(e);
        _service.ChatStopped += (s, e) => stoppedTcs.TrySetResult(e);

        _service.Start(liveId: liveId);

        ErrorOccurredEventArgs errorArgs = await WaitForTcsResult(errorTcs, "ErrorOccurred");
        Assert.IsNotNull(errorArgs);
        Assert.IsInstanceOfType<InvalidOperationException>(errorArgs.GetException());
        Assert.AreEqual(expectedInnerExceptionMessage, errorArgs.GetException().Message);
        Assert.AreEqual(httpException, errorArgs.GetException().InnerException);

        ChatStoppedEventArgs stoppedArgs = await WaitForTcsResult(stoppedTcs, "ChatStopped");
        Assert.IsNotNull(stoppedArgs);
        string expectedReason = $"Critical error: {expectedInnerExceptionMessage}";
        Assert.AreEqual(expectedReason, stoppedArgs.Reason);
    }

    [TestMethod]
    public async Task PollingLoop_ReceivesMultipleItemsAndUpdatesContinuation()
    {
        string liveId = "pollLoopLiveId_Multi_02";
        string apiKey = "key_MultiPoll_02";
        string clientVersion = "cv_MultiPoll_02";
        string initialContinuationFromHtml = "cont_Multi_A_02";
        string continuationAfterFirstPoll = "cont_Multi_B_02";
        string continuationAfterSecondPoll = "cont_Multi_C_02"; // For third poll if added

        _mockLogger.Object.LogInformation("[{TestName}] Setting up mocks.", TestContext.TestName);

        string pageHtml = UtilityTestData.GetSampleLivePageHtml(
            liveId,
            apiKey,
            clientVersion,
            initialContinuationFromHtml
        );
        _mockYtHttpClient
            .Setup(client =>
                client.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(pageHtml);

        // First Poll Setup
        string renderer1Content = TextMessageTestData.SimpleTextMessage1();
        string item1Json = $$"""{ "liveChatTextMessageRenderer": {{renderer1Content}} }""";
        string response1Json = UtilityTestData.WrapItemsInLiveChatResponse(
            [item1Json],
            continuationAfterFirstPoll
        );
        LiveChatResponse? liveChatResponse1 = JsonSerializer.Deserialize<LiveChatResponse>(
            response1Json
        );
        Assert.IsNotNull(liveChatResponse1);
        _mockYtHttpClient
            .Setup(client =>
                client.GetLiveChatAsync(
                    It.Is<FetchOptions>(fo =>
                        fo.Continuation == initialContinuationFromHtml && fo.ApiKey == apiKey
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((liveChatResponse1, response1Json))
            .Verifiable("First poll was not executed with correct parameters.");

        // Second Poll Setup
        string renderer2Content = TextMessageTestData.TextMessageWithStandardEmoji();
        string item2Json = $$"""{ "liveChatTextMessageRenderer": {{renderer2Content}} }""";
        string response2Json = UtilityTestData.WrapItemsInLiveChatResponse(
            [item2Json],
            continuationAfterSecondPoll
        );
        LiveChatResponse? liveChatResponse2 = JsonSerializer.Deserialize<LiveChatResponse>(
            response2Json
        );
        Assert.IsNotNull(liveChatResponse2);
        _mockYtHttpClient
            .Setup(client =>
                client.GetLiveChatAsync(
                    It.Is<FetchOptions>(fo =>
                        fo.Continuation == continuationAfterFirstPoll && fo.ApiKey == apiKey
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((liveChatResponse2, response2Json))
            .Verifiable("Second poll was not executed with correct parameters.");

        List<ChatItem> receivedItems = [];
        TaskCompletionSource<bool> initialPageLoadedTcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        TaskCompletionSource<bool> firstItemReceivedTcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        TaskCompletionSource<bool> secondItemReceivedTcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        _service.InitialPageLoaded += (s, e) => initialPageLoadedTcs.TrySetResult(true);
        _service.ChatReceived += (s, e) =>
        {
            receivedItems.Add(e.ChatItem);
            if (e.ChatItem.Id == "MSG_ID_SIMPLE_01")
                firstItemReceivedTcs.TrySetResult(true);
            if (e.ChatItem.Id == "MSG_ID_STD_EMOJI_01")
                secondItemReceivedTcs.TrySetResult(true);
        };

        _service.Start(liveId: liveId);

        await WaitForTcsResult(initialPageLoadedTcs, "InitialPageLoaded_Multi");
        await WaitForTcsResult(firstItemReceivedTcs, "FirstItem_Multi");
        await WaitForTcsResult(secondItemReceivedTcs, "SecondItem_Multi");

        Assert.AreEqual(2, receivedItems.Count);
        Assert.AreEqual("MSG_ID_SIMPLE_01", receivedItems[0].Id);
        Assert.AreEqual("MSG_ID_STD_EMOJI_01", receivedItems[1].Id);

        _mockYtHttpClient.VerifyAll();
    }

    [TestMethod]
    public async Task PollingLoop_StreamEnds_ChatStoppedEventFired()
    {
        string liveId = "endStreamLiveId004";
        string initialCont = "contFinal004";
        string apiKey = "keyFinal004";
        string clientVersion = "cvFinal004";
        string rendererContent = TextMessageTestData.SimpleTextMessage1();
        string itemObjectJson = $$"""{ "liveChatTextMessageRenderer": {{rendererContent}} }""";

        _mockYtHttpClient
            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UtilityTestData.GetSampleLivePageHtml(liveId, apiKey, clientVersion, initialCont)
            );

        string responseJson = UtilityTestData.StreamEndedResponse(itemObjectJson);
        LiveChatResponse? liveChatResponse = JsonSerializer.Deserialize<LiveChatResponse>(
            responseJson
        );
        Assert.IsNotNull(liveChatResponse);
        _mockYtHttpClient
            .Setup(c =>
                c.GetLiveChatAsync(
                    It.Is<FetchOptions>(fo =>
                        fo.Continuation == initialCont && fo.ApiKey == apiKey
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((liveChatResponse, responseJson));

        TaskCompletionSource<ChatStoppedEventArgs> stoppedTcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        TaskCompletionSource<bool> receivedItemTcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        TaskCompletionSource<bool> initialPageLoadedTcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        _service.InitialPageLoaded += (s, e) => initialPageLoadedTcs.TrySetResult(true);
        _service.ChatReceived += (s, e) => receivedItemTcs.TrySetResult(true);
        _service.ChatStopped += (s, e) => stoppedTcs.TrySetResult(e);

        _service.Start(liveId: liveId);

        await WaitForTcsResult(initialPageLoadedTcs, "InitialPageLoaded_StreamEnd");
        await WaitForTcsResult(receivedItemTcs, "ReceivedItem_StreamEnd");

        ChatStoppedEventArgs stoppedArgs = await WaitForTcsResult(
            stoppedTcs,
            "ChatStopped_StreamEnd"
        );
        Assert.IsNotNull(stoppedArgs);
        Assert.AreEqual("Stream ended or continuation lost", stoppedArgs.Reason);
    }

    // --- New Service Tests for SuperChat and Membership ---
    [TestMethod]
    public async Task PollingLoop_ReceivesSuperChat_EventFiredWithCorrectData()
    {
        string liveId = "superChatTest001";
        string apiKey = "apiKeySC";
        string clientVersion = "cvSC";
        string initialCont = "initialContSC";
        string nextCont = "nextContSC";

        _mockYtHttpClient
            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UtilityTestData.GetSampleLivePageHtml(liveId, apiKey, clientVersion, initialCont)
            );

        string superChatRendererJson = SuperChatTestData.SuperChatMessageFromLatestLog(); // Uses $10.00 SC
        string itemObjectJson =
            $$"""{ "liveChatPaidMessageRenderer": {{superChatRendererJson}} }""";
        string responseJson = UtilityTestData.WrapItemsInLiveChatResponse(
            [itemObjectJson],
            nextCont
        );
        LiveChatResponse? liveChatResponse = JsonSerializer.Deserialize<LiveChatResponse>(
            responseJson
        );
        Assert.IsNotNull(liveChatResponse);

        _mockYtHttpClient
            .Setup(c =>
                c.GetLiveChatAsync(
                    It.Is<FetchOptions>(fo =>
                        fo.Continuation == initialCont && fo.ApiKey == apiKey
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((liveChatResponse, responseJson));

        TaskCompletionSource<ChatReceivedEventArgs> chatReceivedTcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _service.InitialPageLoaded += (s, e) =>
            _mockLogger.Object.LogInformation("Initial Page Loaded for SC Test");
        _service.ChatReceived += (s, e) => chatReceivedTcs.TrySetResult(e);

        _service.Start(liveId: liveId);

        ChatReceivedEventArgs receivedArgs = await WaitForTcsResult(
            chatReceivedTcs,
            "SuperChatReceived"
        );
        Assert.IsNotNull(receivedArgs.ChatItem);
        Assert.AreEqual("SC_ID_LATEST_01", receivedArgs.ChatItem.Id);
        Assert.IsNotNull(receivedArgs.ChatItem.Superchat);
        Assert.AreEqual("$10.00", receivedArgs.ChatItem.Superchat.AmountString);
        Assert.AreEqual(10.00m, receivedArgs.ChatItem.Superchat.AmountValue);
        Assert.AreEqual("USD", receivedArgs.ChatItem.Superchat.Currency);
    }

    [TestMethod]
    public async Task PollingLoop_ReceivesNewMember_EventFiredWithCorrectData()
    {
        string liveId = "newMemberTest001";
        string apiKey = "apiKeyNM";
        string clientVersion = "cvNM";
        string initialCont = "initialContNM";
        string nextCont = "nextContNM";

        _mockYtHttpClient
            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UtilityTestData.GetSampleLivePageHtml(liveId, apiKey, clientVersion, initialCont)
            );

        string newMemberRendererJson = MembershipTestData.NewMemberChickenMcNugget();
        string itemObjectJson =
            $$"""{ "liveChatMembershipItemRenderer": {{newMemberRendererJson}} }""";
        string responseJson = UtilityTestData.WrapItemsInLiveChatResponse(
            [itemObjectJson],
            nextCont
        );
        LiveChatResponse? liveChatResponse = JsonSerializer.Deserialize<LiveChatResponse>(
            responseJson
        );
        Assert.IsNotNull(liveChatResponse);

        _mockYtHttpClient
            .Setup(c =>
                c.GetLiveChatAsync(
                    It.Is<FetchOptions>(fo =>
                        fo.Continuation == initialCont && fo.ApiKey == apiKey
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((liveChatResponse, responseJson));

        TaskCompletionSource<ChatReceivedEventArgs> chatReceivedTcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _service.InitialPageLoaded += (s, e) =>
            _mockLogger.Object.LogInformation("Initial Page Loaded for New Member Test");
        _service.ChatReceived += (s, e) => chatReceivedTcs.TrySetResult(e);

        _service.Start(liveId: liveId);

        ChatReceivedEventArgs receivedArgs = await WaitForTcsResult(
            chatReceivedTcs,
            "NewMemberReceived"
        );
        Assert.IsNotNull(receivedArgs.ChatItem);
        Assert.AreEqual("NEW_MEMBER_CHICKEN_ID", receivedArgs.ChatItem.Id);
        Assert.IsNotNull(receivedArgs.ChatItem.MembershipDetails);
        Assert.AreEqual(MembershipEventType.New, receivedArgs.ChatItem.MembershipDetails.EventType);
        Assert.AreEqual("Member (6 months)", receivedArgs.ChatItem.MembershipDetails.LevelName);
        Assert.IsTrue(receivedArgs.ChatItem.IsMembership);
    }
}
