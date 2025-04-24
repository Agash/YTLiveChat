using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Models;
using YTLiveChat.Contracts.Services;
using YTLiveChat.Models; // For FetchOptions
using YTLiveChat.Models.Response; // For LiveChatResponse
using YTLiveChat.Services;
using System.Text.Json; // For JsonException simulation
using YTLiveChat.Tests.Helpers; // For ParserTestData

namespace YTLiveChat.Tests.Services;

[TestClass]
public class YTLiveChatServiceTests
{
    private Mock<YTHttpClientFactory> _mockHttpClientFactory = null!;
    private Mock<YTHttpClient> _mockHttpClient = null!;
    private Mock<IOptions<YTLiveChatOptions>> _mockOptions = null!;
    private YTLiveChatOptions _options = null!;
    private YTLiveChatService _service = null!; // Using concrete class for testing internal logic

    // Helper class to allow testing protected virtual methods like On... events
    private class YTLiveChatService : YTLiveChat.Services.YTLiveChat
    {
        public YTLiveChatService(IOptions<YTLiveChatOptions> options, YTHttpClientFactory httpClientFactory)
            : base(options, httpClientFactory) { }

        public FetchOptions? GetCurrentFetchOptions() => _fetchOptions; // Expose internal state for testing

        // Public wrappers for protected event triggers
        public void TriggerInitialPageLoaded(InitialPageLoadedEventArgs e) => OnInitialPageLoaded(e);
        public void TriggerChatStopped(ChatStoppedEventArgs e) => OnChatStopped(e);
        public void TriggerChatReceived(ChatReceivedEventArgs e) => OnChatReceived(e);
        public void TriggerErrorOccurred(ErrorOccurredEventArgs e) => OnErrorOccurred(e);
    }


    [TestInitialize]
    public void TestInitialize()
    {
        _mockHttpClientFactory = new Mock<YTHttpClientFactory>(MockBehavior.Strict, new Mock<IServiceProvider>().Object); // Strict factory
        _mockHttpClient = new Mock<YTHttpClient>(MockBehavior.Default, new HttpClient()); // Default client mock
        _mockOptions = new Mock<IOptions<YTLiveChatOptions>>();
        _options = new YTLiveChatOptions { RequestFrequency = 100 }; // Use a short freq for tests

        _mockOptions.Setup(o => o.Value).Returns(_options);
        _mockHttpClientFactory.Setup(f => f.Create()).Returns(_mockHttpClient.Object);

        // Setup Dispose for the mocked HttpClient
        _mockHttpClient.Setup(c => c.Dispose());


        _service = new YTLiveChatService(_mockOptions.Object, _mockHttpClientFactory.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _service?.Dispose(); // Ensure service cleans up resources like CancellationTokenSource
    }

    private FetchOptions CreateSampleFetchOptions(string continuation = "initial")
    {
        return new FetchOptions { ApiKey = "key", ClientVersion = "v1", LiveId = "live1", Continuation = continuation };
    }

    private LiveChatResponse CreateSampleSuccessResponse(string nextContinuation, int itemCount = 1)
    {
        var items = Enumerable.Range(0, itemCount).Select(i =>
             new YTLiveChat.Models.Response.Action // Use internal Action type
             {
                 AddChatItemAction = new AddChatItemAction
                 {
                     Item = new AddChatItemActionItem // Use internal item type
                     {
                         LiveChatTextMessageRenderer = new LiveChatTextMessageRenderer
                         {
                             Id = $"msg-{nextContinuation}-{i}",
                             TimestampUsec = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() + "000",
                             AuthorName = new SimpleText { Text = $"User {i}" },
                             AuthorExternalChannelId = "chan1",
                             Message = new Message { Runs = [new MessageText { Text = $"Hello {i}" }] }
                         }
                     }
                 }
             }).ToList();

        return new LiveChatResponse
        {
            ContinuationContents = new ContinuationContents
            {
                LiveChatContinuation = new LiveChatContinuation
                {
                    Actions = items,
                    Continuations =
                    [
                         new Continuation { TimedContinuationData = new TimedContinuationData { Continuation = nextContinuation, TimeoutMs = 5000 } }
                    ]
                }
            }
        };
    }

    private LiveChatResponse CreateSampleResponseNoContinuation(int itemCount = 1)
    {
        var response = CreateSampleSuccessResponse("ignored", itemCount);
        // Remove continuation
        response.ContinuationContents!.LiveChatContinuation!.Continuations = null;
        // Add alternative continuation type (which might be parsed but is usually null/empty if stream ends)
        response.ContinuationContents!.LiveChatContinuation!.Continuations =
                  [
                       new Continuation { InvalidationContinuationData = new InvalidationContinuationData { Continuation = null } } // Simulate end
                  ];
        return response;
    }

    [TestMethod]
    public async Task Start_InitializesOptionsAndFiresEvent()
    {
        // Arrange
        string liveId = "testLiveId";
        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId); // Use correct ID
        var expectedOptions = Parser.GetOptionsFromLivePage(initialHtml);
        bool eventFired = false;
        string? receivedLiveId = null;

        _mockHttpClient.Setup(c => c.GetOptionsAsync(null, null, liveId)).ReturnsAsync(initialHtml);
        // Setup GetLiveChatAsync to return something simple to prevent immediate stop
        _mockHttpClient.Setup(c => c.GetLiveChatAsync(It.IsAny<FetchOptions>()))
                       .ReturnsAsync((CreateSampleSuccessResponse("cont-1"), "{}"));

        _service.InitialPageLoaded += (sender, args) =>
        {
            eventFired = true;
            receivedLiveId = args.LiveId;
        };

        // Act
        _service.Start(liveId: liveId);
        await Task.Delay(_options.RequestFrequency * 2); // Allow time for initialization and first poll

        // Assert
        _mockHttpClient.Verify(c => c.GetOptionsAsync(null, null, liveId), Times.Once);
        Assert.IsTrue(eventFired, "InitialPageLoaded event did not fire.");
        Assert.AreEqual(liveId, receivedLiveId, "InitialPageLoaded event had wrong LiveId.");
        var currentOptions = _service.GetCurrentFetchOptions();
        Assert.IsNotNull(currentOptions);
        Assert.AreEqual(expectedOptions.Continuation, currentOptions.Continuation); // Check if options were stored
    }

    [TestMethod]
    public async Task Start_GetOptionsFails_FiresErrorAndStops()
    {
        // Arrange
        string liveId = "failLiveId";
        var initException = new HttpRequestException("Network Error");
        bool errorFired = false;
        Exception? capturedError = null;
        bool stoppedFired = false;
        string? stopReason = null;


        _mockHttpClient.Setup(c => c.GetOptionsAsync(null, null, liveId)).ThrowsAsync(initException);

        _service.ErrorOccurred += (sender, args) =>
        {
            errorFired = true;
            capturedError = args.GetException();
        };
        _service.ChatStopped += (sender, args) =>
        {
            stoppedFired = true;
            stopReason = args.Reason;
        };

        // Act
        _service.Start(liveId: liveId);
        await Task.Delay(_options.RequestFrequency); // Allow time for initialization attempt

        // Assert
        _mockHttpClient.Verify(c => c.GetOptionsAsync(null, null, liveId), Times.Once);
        Assert.IsTrue(errorFired, "ErrorOccurred event did not fire.");
        Assert.IsInstanceOfType<InvalidOperationException>(capturedError); // Wraps original exception
        Assert.AreEqual(initException, capturedError.InnerException);
        Assert.IsTrue(stoppedFired, "ChatStopped event did not fire.");
        Assert.IsTrue(stopReason?.Contains("Initialization error"), $"Unexpected stop reason: {stopReason}");
    }


    [TestMethod]
    public async Task PollingLoop_ReceivesItems_FiresChatReceived()
    {
        // Arrange
        string liveId = "pollLiveId";
        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
        var initialOptions = Parser.GetOptionsFromLivePage(initialHtml);
        var response1 = CreateSampleSuccessResponse("cont-2", 2); // 2 items
        var response2 = CreateSampleSuccessResponse("cont-3", 1); // 1 item
        List<ChatItem> receivedItems = [];


        _mockHttpClient.Setup(c => c.GetOptionsAsync(null, null, liveId)).ReturnsAsync(initialHtml);
        _mockHttpClient.SetupSequence(c => c.GetLiveChatAsync(It.IsAny<FetchOptions>()))
                       .ReturnsAsync((response1, JsonSerializer.Serialize(response1)))
                       .ReturnsAsync((response2, JsonSerializer.Serialize(response2)));

        _service.ChatReceived += (sender, args) => receivedItems.Add(args.ChatItem);

        // Act
        _service.Start(liveId: liveId);
        await Task.Delay(_options.RequestFrequency * 3); // Allow time for init + 2 polls
        _service.Stop(); // Stop polling

        // Assert
        Assert.AreEqual(3, receivedItems.Count); // 2 from first poll, 1 from second
        Assert.AreEqual("msg-cont-2-0", receivedItems[0].Id);
        Assert.AreEqual("msg-cont-2-1", receivedItems[1].Id);
        Assert.AreEqual("msg-cont-3-0", receivedItems[2].Id);

        // Verify continuation token updated
        _mockHttpClient.Verify(c => c.GetLiveChatAsync(It.Is<FetchOptions>(o => o.Continuation == initialOptions.Continuation)), Times.Once);
        _mockHttpClient.Verify(c => c.GetLiveChatAsync(It.Is<FetchOptions>(o => o.Continuation == "cont-2")), Times.Once);
        var finalOptions = _service.GetCurrentFetchOptions();
        Assert.AreEqual("cont-3", finalOptions?.Continuation);
    }

    [TestMethod]
    public async Task PollingLoop_ReceivesNoContinuation_StopsAndFiresEvent()
    {
        // Arrange
        string liveId = "endLiveId";
        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
        var responseEnd = CreateSampleResponseNoContinuation(1); // Response with no valid next continuation
        bool stoppedFired = false;
        string? stopReason = null;

        _mockHttpClient.Setup(c => c.GetOptionsAsync(null, null, liveId)).ReturnsAsync(initialHtml);
        _mockHttpClient.Setup(c => c.GetLiveChatAsync(It.IsAny<FetchOptions>()))
                       .ReturnsAsync((responseEnd, JsonSerializer.Serialize(responseEnd))); // Only one poll needed


        _service.ChatStopped += (sender, args) =>
        {
            stoppedFired = true;
            stopReason = args.Reason;
        };

        // Act
        _service.Start(liveId: liveId);
        await Task.Delay(_options.RequestFrequency * 2); // Allow time for init + poll

        // Assert
        Assert.IsTrue(stoppedFired, "ChatStopped event did not fire.");
        Assert.IsTrue(stopReason?.Contains("Stream ended or continuation lost"), $"Unexpected stop reason: {stopReason}");
        // Verify GetLiveChatAsync was only called once after init
        _mockHttpClient.Verify(c => c.GetLiveChatAsync(It.IsAny<FetchOptions>()), Times.Once);
    }


    [TestMethod]
    public async Task PollingLoop_HttpClientThrowsHttpRequestException_FiresErrorAndRetries()
    {
        // Arrange
        string liveId = "retryLiveId";
        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
        var successResponse = CreateSampleSuccessResponse("cont-GOOD", 1);
        var exception = new HttpRequestException("Network glitch");
        int errorCount = 0;

        _mockHttpClient.Setup(c => c.GetOptionsAsync(null, null, liveId)).ReturnsAsync(initialHtml);
        _mockHttpClient.SetupSequence(c => c.GetLiveChatAsync(It.IsAny<FetchOptions>()))
                       .ThrowsAsync(exception) // First poll fails
                       .ReturnsAsync((successResponse, JsonSerializer.Serialize(successResponse))); // Second poll succeeds


        _service.ErrorOccurred += (sender, args) =>
        {
            if (args.GetException() == exception) errorCount++;
        };

        // Act
        _service.Start(liveId: liveId);
        // Wait long enough for init, failed poll + delay, successful poll
        await Task.Delay(_options.RequestFrequency + 5000 + _options.RequestFrequency + 500); // Initial poll + retry delay + next poll interval + buffer
        _service.Stop();

        // Assert
        Assert.AreEqual(1, errorCount, "ErrorOccurred event did not fire exactly once for the exception.");
        // Verify GetLiveChatAsync was called twice (initial fail, retry success)
        _mockHttpClient.Verify(c => c.GetLiveChatAsync(It.IsAny<FetchOptions>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task PollingLoop_HttpClientThrows403Forbidden_FiresErrorAndStops()
    {
        // Arrange
        string liveId = "forbiddenLiveId";
        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
        var exception = new HttpRequestException("Forbidden", null, System.Net.HttpStatusCode.Forbidden);
        bool errorFired = false;
        bool stoppedFired = false;
        string? stopReason = null;

        _mockHttpClient.Setup(c => c.GetOptionsAsync(null, null, liveId)).ReturnsAsync(initialHtml);
        _mockHttpClient.Setup(c => c.GetLiveChatAsync(It.IsAny<FetchOptions>()))
                       .ThrowsAsync(exception); // Poll fails with 403

        _service.ErrorOccurred += (sender, args) => { if (args.GetException() == exception) errorFired = true; };
        _service.ChatStopped += (sender, args) =>
        {
            stoppedFired = true;
            stopReason = args.Reason;
        };

        // Act
        _service.Start(liveId: liveId);
        await Task.Delay(_options.RequestFrequency * 2); // Allow time for init + failed poll

        // Assert
        Assert.IsTrue(errorFired, "ErrorOccurred did not fire.");
        Assert.IsTrue(stoppedFired, "ChatStopped did not fire.");
        Assert.AreEqual("Received Forbidden (403)", stopReason);
        _mockHttpClient.Verify(c => c.GetLiveChatAsync(It.IsAny<FetchOptions>()), Times.Once); // Should not retry
    }


    [TestMethod]
    public async Task Stop_CancelsTokenAndFiresEvent()
    {
        // Arrange
        string liveId = "stopLiveId";
        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
        bool stoppedFired = false;
        string? stopReason = null;
        var tcs = new TaskCompletionSource(); // To wait for the internal task to react to cancellation

        _mockHttpClient.Setup(c => c.GetOptionsAsync(null, null, liveId)).ReturnsAsync(initialHtml);
        // Make GetLiveChatAsync wait indefinitely until cancelled
        _mockHttpClient.Setup(c => c.GetLiveChatAsync(It.IsAny<FetchOptions>()))
                       .Returns(async (FetchOptions _) =>
                       {
                           try { await Task.Delay(Timeout.InfiniteTimeSpan, _service.GetCancellationToken()); } // Use service's token
                           catch (OperationCanceledException) { tcs.TrySetResult(); throw; } // Signal when cancellation is processed
                           return (null, null);
                       });


        _service.ChatStopped += (sender, args) =>
        {
            stoppedFired = true;
            stopReason = args.Reason;
        };

        // Act
        _service.Start(liveId: liveId);
        await Task.Delay(_options.RequestFrequency); // Ensure StartAsync has run and potentially hit the GetLiveChatAsync delay
        _service.Stop();
        await Task.WhenAny(tcs.Task, Task.Delay(2000)); // Wait for cancellation to be processed or timeout

        // Assert
        Assert.IsTrue(stoppedFired, "ChatStopped did not fire.");
        Assert.AreEqual("Operation Cancelled", stopReason);
    }


    [TestMethod]
    public void Start_WhenAlreadyRunning_DoesNothingIfNotOverwrite()
    {
        // Arrange
        string liveId = "noOverwrite";
        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
        _mockHttpClient.Setup(c => c.GetOptionsAsync(null, null, liveId)).ReturnsAsync(initialHtml);
        _mockHttpClient.Setup(c => c.GetLiveChatAsync(It.IsAny<FetchOptions>()))
                       .ReturnsAsync((CreateSampleSuccessResponse("cont-no-overwrite"), "{}"));


        // Act
        _service.Start(liveId: liveId); // First start
        _service.Start(liveId: liveId, overwrite: false); // Second start, no overwrite

        // Assert
        // Verify init (GetOptionsAsync) was only called ONCE
        _mockHttpClient.Verify(c => c.GetOptionsAsync(null, null, liveId), Times.Once);
    }

    [TestMethod]
    public async Task Start_WhenAlreadyRunning_RestartsIfOverwrite()
    {
        // Arrange
        string liveId1 = "overwrite1";
        string liveId2 = "overwrite2";
        string html1 = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId1);
        string html2 = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId2);
        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        _mockHttpClient.Setup(c => c.GetOptionsAsync(null, null, liveId1)).ReturnsAsync(html1);
        _mockHttpClient.Setup(c => c.GetOptionsAsync(null, null, liveId2)).ReturnsAsync(html2);

        // Setup first run to wait until cancelled
        _mockHttpClient.Setup(c => c.GetLiveChatAsync(It.Is<FetchOptions>(o => o.LiveId == liveId1)))
                       .Returns(async (FetchOptions _) =>
                       {
                           try { await Task.Delay(Timeout.InfiniteTimeSpan, _service.GetCancellationToken()); }
                           catch (OperationCanceledException) { tcs1.TrySetResult(); throw; }
                           return (null, null);
                       });
        // Setup second run
        _mockHttpClient.Setup(c => c.GetLiveChatAsync(It.Is<FetchOptions>(o => o.LiveId == liveId2)))
                       .ReturnsAsync((CreateSampleSuccessResponse("cont-overwrite"), "{}"))
                       .Callback(() => tcs2.TrySetResult());


        // Act
        _service.Start(liveId: liveId1); // First start
        await Task.Delay(_options.RequestFrequency); // Let it start polling

        _service.Start(liveId: liveId2, overwrite: true); // Second start with overwrite
        // Wait for the first task to be cancelled and the second one to start polling
        await Task.WhenAll(
                Task.WhenAny(tcs1.Task, Task.Delay(2000)), // Wait for first task cancellation signal
                Task.WhenAny(tcs2.Task, Task.Delay(2000))  // Wait for second task polling signal
            );


        // Assert
        _mockHttpClient.Verify(c => c.GetOptionsAsync(null, null, liveId1), Times.Once); // First init
        _mockHttpClient.Verify(c => c.GetOptionsAsync(null, null, liveId2), Times.Once); // Second init (overwrite)
        Assert.IsTrue(tcs1.Task.IsCompletedSuccessfully, "First task was not cancelled.");
        Assert.IsTrue(tcs2.Task.IsCompletedSuccessfully, "Second task did not start polling.");
        Assert.AreEqual(liveId2, _service.GetCurrentFetchOptions()?.LiveId); // Ensure options reflect the second start
    }

    // Add tests for Debug Logging behavior if needed (requires mocking file system or checking console output)
}

// Add this helper extension method to YTLiveChatServiceTests.cs or a shared test utilities file
public static class YTLiveChatServiceTestExtensions
{
    // Helper to get the cancellation token from the service (using reflection - for testing only!)
    public static CancellationToken GetCancellationToken(this YTLiveChatService service)
    {
        var fieldInfo = typeof(YTLiveChat.Services.YTLiveChat).GetField("_cancellationTokenSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cts = (CancellationTokenSource?)fieldInfo?.GetValue(service);
        return cts?.Token ?? CancellationToken.None;
    }
}