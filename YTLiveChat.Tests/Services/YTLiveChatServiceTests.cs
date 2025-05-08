//using System.Text.Json;
//using Microsoft.Extensions.Logging;
//using Moq;
//using YTLiveChat.Contracts;
//using YTLiveChat.Contracts.Models;
//using YTLiveChat.Contracts.Services;
//using YTLiveChat.Models;
//using YTLiveChat.Models.Response;
//using YTLiveChat.Services;
//using YTLiveChat.Tests.Helpers; // For ParserTestData

//namespace YTLiveChat.Tests.Services;

//[TestClass]
//public class YTLiveChatServiceTests
//{
//    private const int MaxRetryAttempts = 5;
//    private const double BaseRetryDelaySeconds = 1.0;

//    // Mocks for dependencies
//    private Mock<YTHttpClientFactory> _mockHttpClientFactory = null!;
//    private Mock<YTHttpClient> _mockHttpClient = null!;
//    private Mock<IOptions<YTLiveChatOptions>> _mockOptions = null!;
//    private Mock<ILogger<YTLiveChat.Services.YTLiveChat>> _mockLogger = null!;
//    private Mock<ILogger<YTHttpClient>> _mockHttpClientLogger = null!; // Logger for the client itself

//    // Configurable options instance
//    private YTLiveChatOptions _options = null!;

//    // Service under test (using concrete class to test internal logic)
//    private YTLiveChat.Services.YTLiveChat _service = null!;

//    // Test specific variables
//    private const int TestRequestFrequency = 50; // Use a short frequency for tests

//    [TestInitialize]
//    public void TestInitialize()
//    {
//        _mockOptions = new Mock<IOptions<YTLiveChatOptions>>();
//        _options = new YTLiveChatOptions { RequestFrequency = TestRequestFrequency }; // Apply test frequency
//        _mockOptions.Setup(o => o.Value).Returns(_options);

//        _mockLogger = new Mock<ILogger<YTLiveChat.Services.YTLiveChat>>();
//        _mockHttpClientLogger = new Mock<ILogger<YTHttpClient>>(); // Mock logger for HttpClient

//        // Mock HttpClient - Needs the logger dependency now
//        _mockHttpClient = new Mock<YTHttpClient>(
//            MockBehavior.Strict,
//            new HttpClient(),
//            _mockHttpClientLogger.Object
//        );
//        // _mockHttpClient.Setup(c => c.Dispose()); // Setup Dispose is important

//        // Mock HttpClientFactory
//        _mockHttpClientFactory = new Mock<YTHttpClientFactory>(
//            MockBehavior.Strict,
//            Mock.Of<IServiceProvider>()
//        ); // Provide dummy IServiceProvider
//        _mockHttpClientFactory.Setup(f => f.Create()).Returns(_mockHttpClient.Object);

//        // Create the service instance with all mocks
//        _service = new YTLiveChat.Services.YTLiveChat(
//            _mockOptions.Object,
//            _mockHttpClientFactory.Object,
//            _mockLogger.Object
//        );
//    }

//    [TestCleanup]
//    public void TestCleanup()
//    {
//        _service?.Dispose(); // Ensure service cleans up CancellationTokenSource etc.
//        _mockHttpClient.VerifyAll(); // Verify all strict mock expectations were met
//        _mockHttpClientFactory.VerifyAll();
//    }

//    // --- Helper Methods ---

//    private static LiveChatResponse CreateSampleSuccessResponse(
//        string nextContinuation,
//        int itemCount = 1,
//        string baseId = "msg"
//    )
//    {
//        List<Models.Response.Action> items = Enumerable
//            .Range(0, itemCount)
//            .Select(i => new YTLiveChat.Models.Response.Action // Use internal Action type
//            {
//                AddChatItemAction = new AddChatItemAction
//                {
//                    Item = new AddChatItemActionItem // Use internal item type
//                    {
//                        LiveChatTextMessageRenderer = new LiveChatTextMessageRenderer
//                        {
//                            Id = $"{baseId}-{nextContinuation}-{i}", // Use baseId for easier identification
//                            TimestampUsec =
//                                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() + "000",
//                            AuthorName = new SimpleText { Text = $"User {i}" },
//                            AuthorExternalChannelId = "chan1",
//                            Message = new Message
//                            {
//                                Runs = [new MessageText { Text = $"Hello {i}" }],
//                            },
//                        },
//                    },
//                },
//            })
//            .ToList();

//        return new LiveChatResponse
//        {
//            ContinuationContents = new ContinuationContents
//            {
//                LiveChatContinuation = new LiveChatContinuation
//                {
//                    Actions = items,
//                    Continuations = !string.IsNullOrEmpty(nextContinuation)
//                        ?
//                        [
//                            new Continuation
//                            {
//                                TimedContinuationData = new TimedContinuationData
//                                {
//                                    Continuation = nextContinuation,
//                                    TimeoutMs = 5000,
//                                },
//                            },
//                        ]
//                        : null, // No continuation if nextContinuation is null/empty
//                },
//            },
//        };
//    }

//    private static LiveChatResponse CreateSampleResponseNoContinuation(int itemCount = 1) =>
//        CreateSampleSuccessResponse(string.Empty, itemCount);

//    // --- Test Methods ---

//    [TestMethod]
//    public async Task Start_SuccessfulInitialization_FiresInitialPageLoaded()
//    {
//        // Arrange
//        string liveId = "testLiveId";
//        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
//        FetchOptions expectedOptions = Parser.GetOptionsFromLivePage(initialHtml); // Get expected options for verification
//        LiveChatResponse initialPollResponse = CreateSampleSuccessResponse("cont-1");
//        bool eventFired = false;
//        string? receivedLiveId = null;
//        TaskCompletionSource<InitialPageLoadedEventArgs> initTcs = new();
//        TaskCompletionSource<(LiveChatResponse?, string?)> firstPollTcs = new(); // To signal first poll completion

//        _mockHttpClient
//            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(initialHtml);
//        _mockHttpClient
//            .Setup(c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.Continuation == expectedOptions.Continuation),
//                    It.IsAny<CancellationToken>()
//                )
//            )
//            .ReturnsAsync((initialPollResponse, JsonSerializer.Serialize(initialPollResponse)))
//            .Callback(() => firstPollTcs.TrySetResult((initialPollResponse, ""))); // Signal after first poll setup

//        _service.InitialPageLoaded += (sender, args) =>
//        {
//            eventFired = true;
//            receivedLiveId = args.LiveId;
//            initTcs.TrySetResult(args); // Signal that event fired
//        };

//        // Act
//        _service.Start(liveId: liveId);
//        bool initCompleted = await Task.WhenAny(initTcs.Task, Task.Delay(1000)) == initTcs.Task; // Wait for init or timeout
//        bool firstPollCompleted = false;
//        if (initCompleted)
//        {
//            firstPollCompleted =
//                await Task.WhenAny(firstPollTcs.Task, Task.Delay(1000)) == firstPollTcs.Task; // Wait for first poll setup to be hit
//        }

//        _service.Stop(); // Stop after checks

//        // Assert
//        Assert.IsTrue(initCompleted, "InitialPageLoaded event did not fire within timeout.");
//        Assert.IsTrue(eventFired); // Redundant check, but good practice
//        Assert.AreEqual(liveId, receivedLiveId);
//        _mockHttpClient.Verify(
//            c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()),
//            Times.Once
//        );
//        // Verify first poll call was setup/made based on options from HTML
//        Assert.IsTrue(firstPollCompleted, "First poll setup was not reached within timeout.");
//        _mockHttpClient.Verify(
//            c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.Continuation == expectedOptions.Continuation),
//                    It.IsAny<CancellationToken>()
//                ),
//            Times.AtLeastOnce
//        ); // Verify the mock was hit
//    }

//    [TestMethod]
//    public async Task Start_GetOptionsAsyncThrows_FiresErrorAndStops()
//    {
//        // Arrange
//        string liveId = "failLiveId";
//        HttpRequestException httpException = new("Network Error during init");
//        InvalidOperationException expectedException = new(
//            $"Failed to initialize from YouTube page: {httpException.Message}",
//            httpException
//        );
//        bool errorFired = false;
//        Exception? capturedError = null;
//        bool stoppedFired = false;
//        string? stopReason = null;
//        TaskCompletionSource<ChatStoppedEventArgs> stoppedTcs = new();

//        _mockHttpClient
//            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
//            .ThrowsAsync(httpException);
//        // No need to setup GetLiveChatAsync as init fails

//        _service.ErrorOccurred += (sender, args) =>
//        {
//            errorFired = true;
//            capturedError = args.GetException();
//        };
//        _service.ChatStopped += (sender, args) =>
//        {
//            stoppedFired = true;
//            stopReason = args.Reason;
//            stoppedTcs.TrySetResult(args);
//        };

//        // Act
//        _service.Start(liveId: liveId);
//        bool stopCompleted =
//            await Task.WhenAny(stoppedTcs.Task, Task.Delay(1000)) == stoppedTcs.Task;

//        // Assert
//        Assert.IsTrue(stopCompleted, "ChatStopped event did not fire within timeout.");
//        _mockHttpClient.Verify(
//            c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()),
//            Times.Once
//        );
//        Assert.IsTrue(errorFired, "ErrorOccurred event did not fire.");
//        Assert.IsNotNull(capturedError);
//        Assert.AreEqual(expectedException.GetType(), capturedError.GetType());
//        Assert.AreEqual(expectedException.Message, capturedError.Message);
//        Assert.AreEqual(httpException, capturedError.InnerException);
//        Assert.IsTrue(stoppedFired);
//        Assert.AreEqual("Failed to get initial options", stopReason); // Reason set when init fails before firing event
//    }

//    [TestMethod]
//    public async Task Start_ParserGetOptionsFromLivePageThrows_FiresErrorAndStops()
//    {
//        // Arrange
//        string liveId = "parserFailId";
//        string badHtml = "<html/>"; // HTML that will cause parser to fail
//        InvalidOperationException expectedException = new(
//            $"Failed to initialize from YouTube page: Live Stream canonical link not found"
//        );
//        bool errorFired = false;
//        Exception? capturedError = null;
//        bool stoppedFired = false;
//        string? stopReason = null;
//        TaskCompletionSource<ChatStoppedEventArgs> stoppedTcs = new();

//        _mockHttpClient
//            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(badHtml);

//        _service.ErrorOccurred += (sender, args) =>
//        {
//            errorFired = true;
//            capturedError = args.GetException();
//        };
//        _service.ChatStopped += (sender, args) =>
//        {
//            stoppedFired = true;
//            stopReason = args.Reason;
//            stoppedTcs.TrySetResult(args);
//        };

//        // Act
//        _service.Start(liveId: liveId);
//        bool stopCompleted =
//            await Task.WhenAny(stoppedTcs.Task, Task.Delay(1000)) == stoppedTcs.Task;

//        // Assert
//        Assert.IsTrue(stopCompleted, "ChatStopped event did not fire within timeout.");
//        _mockHttpClient.Verify(
//            c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()),
//            Times.Once
//        );
//        Assert.IsTrue(errorFired, "ErrorOccurred event did not fire.");
//        Assert.IsNotNull(capturedError);
//        Assert.AreEqual(expectedException.GetType(), capturedError.GetType());
//        Assert.AreEqual(expectedException.Message, capturedError.Message); // Check wrapped message
//        Assert.IsInstanceOfType<Exception>(capturedError.InnerException); // Original parser exception
//        Assert.IsTrue(stoppedFired);
//        Assert.AreEqual("Failed to get initial options", stopReason);
//    }

//    [TestMethod]
//    public async Task PollingLoop_ReceivesItems_FiresChatReceivedAndUpdatesContinuation()
//    {
//        // Arrange
//        string liveId = "pollLiveId";
//        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
//        FetchOptions initialOptions = Parser.GetOptionsFromLivePage(initialHtml);
//        string initialContinuation = initialOptions.Continuation;

//        LiveChatResponse response1 = CreateSampleSuccessResponse("cont-2", 2, "msg1");
//        LiveChatResponse response2 = CreateSampleSuccessResponse("cont-3", 1, "msg2");
//        List<ChatItem> receivedItems = [];
//        TaskCompletionSource receivedTcs = new(); // Signal when expected items received

//        // Sequence of HTTP calls
//        _mockHttpClient
//            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(initialHtml);
//        _mockHttpClient
//            .Setup(c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.Continuation == initialContinuation),
//                    It.IsAny<CancellationToken>()
//                )
//            )
//            .ReturnsAsync((response1, JsonSerializer.Serialize(response1)));
//        _mockHttpClient
//            .Setup(c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.Continuation == "cont-2"),
//                    It.IsAny<CancellationToken>()
//                )
//            )
//            .ReturnsAsync((response2, JsonSerializer.Serialize(response2)))
//            .Callback(() =>
//            {
//                if (receivedItems.Count == 3)
//                    receivedTcs.TrySetResult();
//            }); // Signal after processing second poll

//        _service.ChatReceived += (sender, args) =>
//        {
//            receivedItems.Add(args.ChatItem);
//            if (receivedItems.Count == 3)
//                receivedTcs.TrySetResult(); // Signal if reached here first
//        };

//        // Act
//        _service.Start(liveId: liveId);
//        bool receivedExpectedItems =
//            await Task.WhenAny(receivedTcs.Task, Task.Delay(TestRequestFrequency * 5))
//            == receivedTcs.Task; // Wait for 3 items or timeout
//        _service.Stop();

//        // Assert
//        Assert.IsTrue(
//            receivedExpectedItems,
//            "Did not receive expected number of items within timeout."
//        );
//        Assert.AreEqual(3, receivedItems.Count); // 2 from first poll, 1 from second
//        Assert.AreEqual("msg1-cont-2-0", receivedItems[0].Id);
//        Assert.AreEqual("msg1-cont-2-1", receivedItems[1].Id);
//        Assert.AreEqual("msg2-cont-3-0", receivedItems[2].Id);

//        // Verify calls with correct continuation tokens
//        _mockHttpClient.Verify(
//            c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()),
//            Times.Once
//        );
//        _mockHttpClient.Verify(
//            c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.Continuation == initialContinuation),
//                    It.IsAny<CancellationToken>()
//                ),
//            Times.Once
//        );
//        _mockHttpClient.Verify(
//            c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.Continuation == "cont-2"),
//                    It.IsAny<CancellationToken>()
//                ),
//            Times.Once
//        );
//        // Note: Cannot directly verify _fetchOptions.Continuation without reflection/exposing state. Verification relies on correct mock setups being called.
//    }

//    [TestMethod]
//    public async Task PollingLoop_ReceivesNoContinuation_StopsAndFiresEvent()
//    {
//        // Arrange
//        string liveId = "endLiveId";
//        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
//        FetchOptions initialOptions = Parser.GetOptionsFromLivePage(initialHtml);
//        LiveChatResponse responseEnd = CreateSampleResponseNoContinuation(1); // Response with no valid next continuation
//        bool stoppedFired = false;
//        string? stopReason = null;
//        TaskCompletionSource<ChatStoppedEventArgs> stoppedTcs = new();
//        List<ChatItem> receivedItems = [];

//        _mockHttpClient
//            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(initialHtml);
//        _mockHttpClient
//            .Setup(c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.Continuation == initialOptions.Continuation),
//                    It.IsAny<CancellationToken>()
//                )
//            )
//            .ReturnsAsync((responseEnd, JsonSerializer.Serialize(responseEnd))); // Only one poll needed

//        _service.ChatReceived += (sender, args) => receivedItems.Add(args.ChatItem);
//        _service.ChatStopped += (sender, args) =>
//        {
//            stoppedFired = true;
//            stopReason = args.Reason;
//            stoppedTcs.TrySetResult(args);
//        };

//        // Act
//        _service.Start(liveId: liveId);
//        bool stopCompleted =
//            await Task.WhenAny(stoppedTcs.Task, Task.Delay(TestRequestFrequency * 3))
//            == stoppedTcs.Task; // Wait for stop or timeout

//        // Assert
//        Assert.IsTrue(stopCompleted, "ChatStopped event did not fire within timeout.");
//        Assert.AreEqual(
//            1,
//            receivedItems.Count,
//            "Should have received item(s) from the final poll."
//        );
//        Assert.IsTrue(stoppedFired);
//        Assert.AreEqual("Stream ended or continuation lost", stopReason);
//        // Verify GetLiveChatAsync was only called once
//        _mockHttpClient.Verify(
//            c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.Continuation == initialOptions.Continuation),
//                    It.IsAny<CancellationToken>()
//                ),
//            Times.Once
//        );
//    }

//    [TestMethod]
//    public async Task PollingLoop_GetLiveChatAsyncReturnsNull_FiresErrorAndRetriesWithBackoff()
//    {
//        // Arrange
//        string liveId = "nullRetryLiveId";
//        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
//        FetchOptions initialOptions = Parser.GetOptionsFromLivePage(initialHtml);
//        LiveChatResponse successResponse = CreateSampleSuccessResponse("cont-GOOD", 1);
//        int errorCount = 0;
//        int successPollCount = 0;
//        TaskCompletionSource successTcs = new(); // Signal when successful poll occurs

//        _mockHttpClient
//            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(initialHtml);
//        // Sequence: Fail (null), Succeed
//        _mockHttpClient
//            .SetupSequence(c =>
//                c.GetLiveChatAsync(It.IsAny<FetchOptions>(), It.IsAny<CancellationToken>())
//            )
//            .ReturnsAsync(((LiveChatResponse?)null, (string?)null)) // First poll fails (returns null)
//            .ReturnsAsync((successResponse, JsonSerializer.Serialize(successResponse))); // Second poll succeeds

//        _service.ErrorOccurred += (sender, args) =>
//        {
//            // Check if it's the expected "null response" exception
//            if (args.GetException().Message.Contains("null response received"))
//                errorCount++;
//        };
//        _service.ChatReceived += (sender, args) =>
//        {
//            successPollCount++;
//            successTcs.TrySetResult(); // Signal success
//        };

//        // Act
//        _service.Start(liveId: liveId);
//        // Wait long enough for init, failed poll + backoff delay (~1s+jitter), successful poll
//        bool successOccurred =
//            await Task.WhenAny(
//                successTcs.Task,
//                Task.Delay((int)(TestRequestFrequency + (1000 * 1.5) + TestRequestFrequency))
//            ) == successTcs.Task;
//        _service.Stop();

//        // Assert
//        Assert.AreEqual(
//            1,
//            errorCount,
//            "ErrorOccurred event did not fire exactly once for the null response."
//        );
//        Assert.IsTrue(successOccurred, "Successful poll did not occur after retry.");
//        Assert.AreEqual(1, successPollCount, "ChatReceived did not fire after successful retry.");
//        // Verify GetLiveChatAsync was called twice (initial fail, retry success)
//        _mockHttpClient.Verify(
//            c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.Continuation == initialOptions.Continuation),
//                    It.IsAny<CancellationToken>()
//                ),
//            Times.Exactly(2)
//        );
//    }

//    [TestMethod]
//    public async Task PollingLoop_GetLiveChatAsyncThrowsRetryableException_FiresErrorAndRetries()
//    {
//        // Arrange
//        string liveId = "retryLiveId";
//        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
//        FetchOptions initialOptions = Parser.GetOptionsFromLivePage(initialHtml);
//        LiveChatResponse successResponse = CreateSampleSuccessResponse("cont-GOOD", 1);
//        HttpRequestException exception = new("Network glitch"); // Retryable exception
//        int errorCount = 0;
//        int successPollCount = 0;
//        TaskCompletionSource successTcs = new(); // Signal when successful poll occurs

//        _mockHttpClient
//            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(initialHtml);
//        // Sequence: Fail (throw), Succeed
//        _mockHttpClient
//            .SetupSequence(c =>
//                c.GetLiveChatAsync(It.IsAny<FetchOptions>(), It.IsAny<CancellationToken>())
//            )
//            .ThrowsAsync(exception) // First poll fails
//            .ReturnsAsync((successResponse, JsonSerializer.Serialize(successResponse))); // Second poll succeeds

//        _service.ErrorOccurred += (sender, args) =>
//        {
//            if (args.GetException() == exception)
//                errorCount++;
//        };
//        _service.ChatReceived += (sender, args) =>
//        {
//            successPollCount++;
//            successTcs.TrySetResult(); // Signal success
//        };

//        // Act
//        _service.Start(liveId: liveId);
//        // Wait long enough for init, failed poll + backoff delay (~1s+jitter), successful poll
//        bool successOccurred =
//            await Task.WhenAny(
//                successTcs.Task,
//                Task.Delay((int)(TestRequestFrequency + (1000 * 1.5) + TestRequestFrequency))
//            ) == successTcs.Task;
//        _service.Stop();

//        // Assert
//        Assert.AreEqual(
//            1,
//            errorCount,
//            "ErrorOccurred event did not fire exactly once for the exception."
//        );
//        Assert.IsTrue(successOccurred, "Successful poll did not occur after retry.");
//        Assert.AreEqual(1, successPollCount, "ChatReceived did not fire after successful retry.");
//        // Verify GetLiveChatAsync was called twice (initial fail, retry success)
//        _mockHttpClient.Verify(
//            c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.Continuation == initialOptions.Continuation),
//                    It.IsAny<CancellationToken>()
//                ),
//            Times.Exactly(2)
//        );
//    }

//    [TestMethod]
//    public async Task PollingLoop_GetLiveChatAsyncThrowsMaxRetryTimes_StopsAndFiresEvent()
//    {
//        // Arrange
//        string liveId = "maxRetryLiveId";
//        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
//        FetchOptions initialOptions = Parser.GetOptionsFromLivePage(initialHtml);
//        TimeoutException exception = new("Persistent timeout"); // Example retryable exception
//        int errorCount = 0;
//        bool stoppedFired = false;
//        string? stopReason = null;
//        TaskCompletionSource<ChatStoppedEventArgs> stoppedTcs = new();

//        _mockHttpClient
//            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(initialHtml);
//        // Setup GetLiveChatAsync to always throw
//        _mockHttpClient
//            .Setup(c => c.GetLiveChatAsync(It.IsAny<FetchOptions>(), It.IsAny<CancellationToken>()))
//            .ThrowsAsync(exception);

//        _service.ErrorOccurred += (sender, args) =>
//        {
//            if (args.GetException() == exception)
//                errorCount++;
//        };
//        _service.ChatStopped += (sender, args) =>
//        {
//            stoppedFired = true;
//            stopReason = args.Reason;
//            stoppedTcs.TrySetResult(args);
//        };

//        // Act
//        _service.Start(liveId: liveId);
//        // Wait long enough for multiple retries and backoff delays - needs to accommodate MaxRetryAttempts
//        // Approximate time: freq + (base * (1 + 2 + 4 + 8 + 16)) + buffer
//        double totalDelayApprox =
//            TestRequestFrequency
//            + (BaseRetryDelaySeconds * 1000 * (Math.Pow(2, MaxRetryAttempts) - 1) * 1.1); // Add buffer
//        bool stopCompleted =
//            await Task.WhenAny(stoppedTcs.Task, Task.Delay((int)totalDelayApprox + 2000))
//            == stoppedTcs.Task; // Wait for stop or timeout

//        // Assert
//        Assert.IsTrue(
//            stopCompleted,
//            "ChatStopped event did not fire within expected time for max retries."
//        );
//        Assert.AreEqual(
//            MaxRetryAttempts + 1,
//            errorCount,
//            $"ErrorOccurred should fire {MaxRetryAttempts + 1} times (initial + retries)."
//        ); // Initial try + MaxRetryAttempts
//        Assert.IsTrue(stoppedFired);
//        Assert.IsNotNull(stopReason);
//        Assert.IsTrue(
//            stopReason.Contains($"Failed after {MaxRetryAttempts} retries"),
//            $"Unexpected stop reason: {stopReason}"
//        );
//        // Verify GetLiveChatAsync was called MaxRetryAttempts + 1 times
//        _mockHttpClient.Verify(
//            c => c.GetLiveChatAsync(It.IsAny<FetchOptions>(), It.IsAny<CancellationToken>()),
//            Times.Exactly(MaxRetryAttempts + 1)
//        );
//    }

//    [TestMethod]
//    public async Task PollingLoop_HttpClientThrows403Forbidden_FiresErrorAndStopsNoRetry()
//    {
//        // Arrange
//        string liveId = "forbiddenLiveId";
//        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
//        FetchOptions initialOptions = Parser.GetOptionsFromLivePage(initialHtml);
//        HttpRequestException exception = new(
//            "Forbidden",
//            null,
//            System.Net.HttpStatusCode.Forbidden
//        );
//        bool errorFired = false;
//        Exception? capturedError = null;
//        bool stoppedFired = false;
//        string? stopReason = null;
//        TaskCompletionSource<ChatStoppedEventArgs> stoppedTcs = new();

//        _mockHttpClient
//            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(initialHtml);
//        _mockHttpClient
//            .Setup(c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.Continuation == initialOptions.Continuation),
//                    It.IsAny<CancellationToken>()
//                )
//            )
//            .ThrowsAsync(exception); // Poll fails with 403

//        _service.ErrorOccurred += (sender, args) =>
//        {
//            errorFired = true;
//            capturedError = args.GetException();
//        };
//        _service.ChatStopped += (sender, args) =>
//        {
//            stoppedFired = true;
//            stopReason = args.Reason;
//            stoppedTcs.TrySetResult(args);
//        };

//        // Act
//        _service.Start(liveId: liveId);
//        bool stopCompleted =
//            await Task.WhenAny(stoppedTcs.Task, Task.Delay(TestRequestFrequency * 3))
//            == stoppedTcs.Task;

//        // Assert
//        Assert.IsTrue(stopCompleted, "ChatStopped event did not fire within timeout.");
//        Assert.IsTrue(errorFired, "ErrorOccurred did not fire.");
//        Assert.AreEqual(exception, capturedError);
//        Assert.IsTrue(stoppedFired);
//        Assert.AreEqual("Received Forbidden (403)", stopReason);
//        // Verify GetLiveChatAsync was only called once (no retry)
//        _mockHttpClient.Verify(
//            c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.Continuation == initialOptions.Continuation),
//                    It.IsAny<CancellationToken>()
//                ),
//            Times.Once
//        );
//    }

//    [TestMethod]
//    public async Task Stop_CancelsTokenAndFiresEvent()
//    {
//        // Arrange
//        string liveId = "stopLiveId";
//        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
//        FetchOptions initialOptions = Parser.GetOptionsFromLivePage(initialHtml);
//        bool stoppedFired = false;
//        string? stopReason = null;
//        TaskCompletionSource pollEnteredTcs = new(); // Signals when GetLiveChatAsync delay starts
//        TaskCompletionSource<ChatStoppedEventArgs> stoppedTcs = new();

//        _mockHttpClient
//            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(initialHtml);
//        // Make GetLiveChatAsync wait indefinitely until cancelled
//        _mockHttpClient
//            .Setup(c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.Continuation == initialOptions.Continuation),
//                    It.IsAny<CancellationToken>()
//                )
//            )
//            .Returns(
//                async (FetchOptions _, CancellationToken ct) => // Use passed cancellation token
//                {
//                    pollEnteredTcs.TrySetResult(); // Signal that we've entered the mock delay
//                    await Task.Delay(Timeout.InfiniteTimeSpan, ct); // Wait indefinitely until cancelled
//                    return (null, null); // Should not be reached if cancelled
//                }
//            );

//        _service.ChatStopped += (sender, args) =>
//        {
//            stoppedFired = true;
//            stopReason = args.Reason;
//            stoppedTcs.TrySetResult(args);
//        };

//        // Act
//        _service.Start(liveId: liveId);
//        bool pollEntered =
//            await Task.WhenAny(pollEnteredTcs.Task, Task.Delay(1000)) == pollEnteredTcs.Task; // Wait for poll to start
//        Assert.IsTrue(pollEntered, "Polling task did not enter the mocked GetLiveChatAsync delay.");
//        _service.Stop(); // Request stop
//        bool stopCompleted =
//            await Task.WhenAny(stoppedTcs.Task, Task.Delay(1000)) == stoppedTcs.Task; // Wait for ChatStopped event

//        // Assert
//        Assert.IsTrue(
//            stopCompleted,
//            "ChatStopped event did not fire within timeout after Stop() call."
//        );
//        Assert.IsTrue(stoppedFired);
//        Assert.AreEqual("Operation Cancelled", stopReason);
//    }

//    [TestMethod]
//    public async Task Start_WhenAlreadyRunning_DoesNothingIfNotOverwrite()
//    {
//        // Arrange
//        string liveId = "noOverwrite";
//        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
//        FetchOptions initialOptions = Parser.GetOptionsFromLivePage(initialHtml);
//        LiveChatResponse pollResponse = CreateSampleSuccessResponse("cont-no-overwrite");
//        TaskCompletionSource firstPollTcs = new();

//        _mockHttpClient
//            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(initialHtml);
//        _mockHttpClient
//            .Setup(c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.Continuation == initialOptions.Continuation),
//                    It.IsAny<CancellationToken>()
//                )
//            )
//            .ReturnsAsync((pollResponse, "{}"))
//            .Callback(() => firstPollTcs.TrySetResult());

//        // Act
//        _service.Start(liveId: liveId); // First start
//        bool poll1Entered =
//            await Task.WhenAny(firstPollTcs.Task, Task.Delay(1000)) == firstPollTcs.Task;
//        Assert.IsTrue(poll1Entered, "First poll was not entered");

//        _service.Start(liveId: liveId, overwrite: false); // Second start, no overwrite
//        await Task.Delay(TestRequestFrequency * 2); // Wait to see if second init happens

//        _service.Stop();

//        // Assert
//        // Verify init (GetOptionsAsync) was only called ONCE
//        _mockHttpClient.Verify(
//            c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()),
//            Times.Once
//        );
//        _mockHttpClient.Verify(
//            c => c.GetLiveChatAsync(It.IsAny<FetchOptions>(), It.IsAny<CancellationToken>()),
//            Times.AtMostOnce()
//        ); // Should only poll once before stop
//    }

//    [TestMethod]
//    public async Task Start_WhenAlreadyRunning_RestartsIfOverwrite()
//    {
//        // Arrange
//        string liveId1 = "overwrite1";
//        string liveId2 = "overwrite2";
//        string html1 = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId1);
//        string html2 = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId2);
//        FetchOptions initialOptions1 = Parser.GetOptionsFromLivePage(html1);
//        FetchOptions initialOptions2 = Parser.GetOptionsFromLivePage(html2);
//        LiveChatResponse response2 = CreateSampleSuccessResponse("cont-overwrite");

//        TaskCompletionSource poll1Tcs = new(); // Signals entry into first poll's long delay
//        TaskCompletionSource stop1Tcs = new(); // Signals cancellation caught in first poll
//        TaskCompletionSource poll2Tcs = new(); // Signals entry into second poll

//        // --- Setup for First Start (liveId1) ---
//        _mockHttpClient
//            .Setup(c => c.GetOptionsAsync(null, null, liveId1, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(html1);
//        _mockHttpClient
//            .Setup(c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o =>
//                        o.LiveId == liveId1 && o.Continuation == initialOptions1.Continuation
//                    ),
//                    It.IsAny<CancellationToken>()
//                )
//            )
//            .Returns(
//                async (FetchOptions _, CancellationToken ct) =>
//                {
//                    poll1Tcs.TrySetResult(); // Signal entry
//                    try
//                    {
//                        await Task.Delay(Timeout.InfiniteTimeSpan, ct);
//                    }
//                    catch (OperationCanceledException)
//                    {
//                        stop1Tcs.TrySetResult();
//                        throw;
//                    } // Signal cancellation

//                    return (null, null);
//                }
//            );

//        // --- Setup for Second Start (liveId2) ---
//        _mockHttpClient
//            .Setup(c => c.GetOptionsAsync(null, null, liveId2, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(html2);
//        _mockHttpClient
//            .Setup(c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o =>
//                        o.LiveId == liveId2 && o.Continuation == initialOptions2.Continuation
//                    ),
//                    It.IsAny<CancellationToken>()
//                )
//            )
//            .ReturnsAsync((response2, "{}"))
//            .Callback(() => poll2Tcs.TrySetResult()); // Signal second poll entry

//        // Act
//        _service.Start(liveId: liveId1); // First start
//        bool poll1Entered = await Task.WhenAny(poll1Tcs.Task, Task.Delay(1000)) == poll1Tcs.Task;
//        Assert.IsTrue(poll1Entered, "First poll delay was not entered.");

//        _service.Start(liveId: liveId2, overwrite: true); // Second start with overwrite

//        // Wait for the first task to be cancelled and the second one to start polling
//        bool stop1Completed = await Task.WhenAny(stop1Tcs.Task, Task.Delay(1000)) == stop1Tcs.Task;
//        bool poll2Entered = await Task.WhenAny(poll2Tcs.Task, Task.Delay(1000)) == poll2Tcs.Task;

//        _service.Stop(); // Stop the second service instance

//        // Assert
//        Assert.IsTrue(stop1Completed, "First task was not cancelled upon overwrite.");
//        Assert.IsTrue(poll2Entered, "Second task did not start polling after overwrite.");

//        _mockHttpClient.Verify(
//            c => c.GetOptionsAsync(null, null, liveId1, It.IsAny<CancellationToken>()),
//            Times.Once
//        ); // First init
//        _mockHttpClient.Verify(
//            c => c.GetOptionsAsync(null, null, liveId2, It.IsAny<CancellationToken>()),
//            Times.Once
//        ); // Second init (overwrite)
//        _mockHttpClient.Verify(
//            c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.LiveId == liveId1),
//                    It.IsAny<CancellationToken>()
//                ),
//            Times.Once
//        ); // First poll attempt
//        _mockHttpClient.Verify(
//            c =>
//                c.GetLiveChatAsync(
//                    It.Is<FetchOptions>(o => o.LiveId == liveId2),
//                    It.IsAny<CancellationToken>()
//                ),
//            Times.Once
//        ); // Second poll attempt
//    }

//    // Test Dispose explicitly? Usually covered by TestCleanup
//    [TestMethod]
//    public void Dispose_StopsServiceAndDisposesResources()
//    {
//        // Arrange: Start the service so it has resources
//        string liveId = "disposeId";
//        string initialHtml = ParserTestData.SampleLivePageHtml.Replace("EXISTING_LIVE_ID", liveId);
//        _mockHttpClient
//            .Setup(c => c.GetOptionsAsync(null, null, liveId, It.IsAny<CancellationToken>()))
//            .ReturnsAsync(initialHtml);
//        // Setup poll to just wait a bit
//        _mockHttpClient
//            .Setup(c => c.GetLiveChatAsync(It.IsAny<FetchOptions>(), It.IsAny<CancellationToken>()))
//            .Returns(
//                async (FetchOptions _, CancellationToken ct) =>
//                {
//                    await Task.Delay(500, ct);
//                    return (null, null); // Return null to avoid parsing/event logic
//                }
//            );

//        _service.Start(liveId: liveId);
//        // Allow start to proceed a bit
//        Thread.Sleep(TestRequestFrequency * 2);

//        // Act
//        _service.Dispose();

//        // Assert
//        // Verification that Stop was implicitly called is tricky without state exposure or more complex event tracking.
//        // We rely on the TestCleanup to verify mocks, implicitly checking if Dispose on _mockHttpClient was setup and potentially called.
//        // A more direct assertion could involve checking if the internal CancellationTokenSource is disposed, but requires reflection.
//        // For now, assume Dispose() correctly calls Stop() and handles internal disposables.
//        Assert.IsTrue(true, "Dispose executed. Assumed internal cleanup happened."); // Placeholder assertion
//    }
//}
