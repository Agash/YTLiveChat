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
    private const int DefaultTestTimeoutSeconds = 7; // Increased default timeout slightly more for parallel runs

    private Mock<YTHttpClient> _mockYtHttpClient = null!;
    private YTLiveChatOptions _ytLiveChatOptions = null!;
    private Mock<ILogger<YTLiveChat.Services.YTLiveChat>> _mockLogger = null!;
    private YTLiveChat.Services.YTLiveChat _service = null!;
    private CancellationTokenSource _testMethodCts = null!; // CTS for each test method

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _testMethodCts = new CancellationTokenSource(); // New CTS for each test
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

        // Signal any ongoing operations within the test method itself to stop
        _testMethodCts.Cancel();
        _testMethodCts.Dispose();

        // Dispose the service, which calls Stop() internally
        _service.Dispose();

        // Give a very brief moment for any outstanding tasks from _service to acknowledge cancellation.
        // This is a pragmatic approach if _service.Dispose() isn't fully awaiting its internal task.
        // In an ideal world, IYTLiveChat would offer an async DisposeAsync or StopAsync that could be awaited.
        try
        {
            // Allow a short period for the background task to notice cancellation and exit.
            // This helps prevent its logging/operations from bleeding into the next test.
            await Task.Delay(TestRequestFrequencyMilliseconds * 2, CancellationToken.None); // Wait a bit longer than one poll interval
        }
        catch (TaskCanceledException)
        {
            // Expected if the delay itself is cancelled by a test runner's timeout, though unlikely here
        }

        _mockLogger.Object.LogInformation(
            "TEST: TestCleanup completed for test: {TestName}",
            TestContext.TestName
        );
    }

    // Helper to wait for TCS with the test method's CancellationToken
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
            throw; // Re-throw to fail the test
        }
        catch (OperationCanceledException) when (_testMethodCts.IsCancellationRequested)
        {
            _mockLogger.Object.LogWarning(
                "[{TestName}] Test method cancelled while waiting for {EventName}.",
                TestContext.TestName,
                eventName
            );
            throw; // Re-throw to indicate test cancellation
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
                        fo.Continuation == initialContinuationFromHtml
                        && fo.ApiKey == apiKey
                        && fo.ClientVersion == clientVersion
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

        _mockYtHttpClient.VerifyAll(); // If you add .Verifiable() to setups
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
        string continuationAfterSecondPoll = "cont_Multi_C_02";

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

        string renderer1Content = TextMessageTestData.SimpleTextMessage1();
        string item1Json = $$"""{ "liveChatTextMessageRenderer": {{renderer1Content}} }""";
        string response1Json = UtilityTestData.WrapItemsInLiveChatResponse(
            [item1Json],
            continuationAfterFirstPoll
        );
        LiveChatResponse? liveChatResponse1 = JsonSerializer.Deserialize<LiveChatResponse>(
            response1Json
        );

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
            .Verifiable();

        string renderer2Content = TextMessageTestData.TextMessageWithStandardEmoji();
        string item2Json = $$"""{ "liveChatTextMessageRenderer": {{renderer2Content}} }""";
        string response2Json = UtilityTestData.WrapItemsInLiveChatResponse(
            [item2Json],
            continuationAfterSecondPoll
        );
        LiveChatResponse? liveChatResponse2 = JsonSerializer.Deserialize<LiveChatResponse>(
            response2Json
        );

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
            .Verifiable();

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
}
