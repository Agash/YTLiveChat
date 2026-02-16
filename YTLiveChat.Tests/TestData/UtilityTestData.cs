namespace YTLiveChat.Tests.TestData;

internal static class UtilityTestData
{
    /// <summary>
    /// Wraps one or more full action JSON objects into a full LiveChatResponse structure.
    /// Each string in actionJsons should be one action object, for example:
    /// { "addChatItemAction": { ... } } or { "removeChatItemAction": { ... } }.
    /// </summary>
    public static string WrapActionsInLiveChatResponse(
        string[] actionJsons,
        string? continuationToken = "0ofPLACEHOLDER_CONTINUATION_TOKEN",
        long timeoutMs = 10000,
        string liveIdTopicSuffix = "fIpBwfFn3sg"
    )
    {
        string actionsJson = string.Join(",", actionJsons);

        string continuationJsonBlock = continuationToken == null
            ? "\"continuations\": null"
            : $$"""
                "continuations": [
                  {
                    "invalidationContinuationData": {
                      "invalidationId": {
                        "objectSource": 1056,
                        "objectId": "Y2hhdH5{{liveIdTopicSuffix}}==",
                        "topic": "chat~{{liveIdTopicSuffix}}",
                        "subscribeToGcmTopics": true,
                        "protoCreationTimestampMs": "{{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}"
                      },
                      "timeoutMs": {{timeoutMs}},
                      "continuation": "{{continuationToken}}"
                    }
                  }
                ]
                """;
        return $$"""
            {
              "responseContext": {
                "serviceTrackingParams": [
                  { "service": "CSI", "params": [ { "key": "c", "value": "WEB" }, { "key": "cver", "value": "2.20250508.00.00" } ] }
                ],
                "mainAppWebResponseContext": { "loggedOut": false, "trackingParam": "TRACKING_PARAM_PLACEHOLDER" },
                "webResponseContextExtensionData": { "hasDecorated": true }
              },
              "continuationContents": {
                "liveChatContinuation": {
                  "actions": [
                    {{actionsJson}}
                  ],
                  {{continuationJsonBlock}},
                  "trackingParams": "LIVE_CHAT_CONTINUATION_TRACKING_PARAMS"
                }
              },
              "liveChatStreamingResponseExtension": {
                "lastPublishAtUsec": "{{(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000)
                + 500000}}"
              }
            }
            """;
    }

    /// <summary>
    /// Wraps one or more item renderer JSON strings into a full LiveChatResponse structure.
    /// Each string in itemRendererJsons should be the JSON for the *item object* itself,
    /// e.g., {"liveChatTextMessageRenderer": { ... }}
    /// </summary>
    public static string WrapItemsInLiveChatResponse(
        string[] itemObjectJsons, // Renamed for clarity: these are full item objects
        string? continuationToken = "0ofPLACEHOLDER_CONTINUATION_TOKEN",
        long timeoutMs = 10000,
        string liveIdTopicSuffix = "fIpBwfFn3sg" // Example suffix for topic
    )
    {
        string[] actionJsons =
        [
            .. itemObjectJsons.Select(itemJson =>
                $$"""
                {
                  "addChatItemAction": {
                    "item": {{itemJson}},
                    "clientId": "CLIENT_ID_PLACEHOLDER_{{Guid.NewGuid().ToString("N")[..8]}}"
                  }
                }
                """
            ),
        ];

        return WrapActionsInLiveChatResponse(
            actionJsons,
            continuationToken,
            timeoutMs,
            liveIdTopicSuffix
        );
    }

    /// <summary>
    /// Returns a LiveChatResponse JSON indicating the stream has ended (no continuation).
    /// Expects full item object JSON (e.g. {"liveChatTextMessageRenderer": {...}} )
    /// </summary>
    public static string StreamEndedResponse(string lastItemObjectJson) =>
        WrapItemsInLiveChatResponse([lastItemObjectJson], continuationToken: null);

    /// <summary>
    /// Returns an empty LiveChatResponse (no actions, no continuation).
    /// </summary>
    public static string EmptyLiveChatResponse() =>
        UtilityTestData.WrapItemsInLiveChatResponse([], continuationToken: null); // Use the main wrapper with empty actions

    /// <summary>
    /// Returns an initial page HTML structure with placeholders.
    /// </summary>
    public static string GetSampleLivePageHtml(
        string liveId,
        string apiKey,
        string clientVersion,
        string initialContinuation
    )
    {
        return $$"""
            <!DOCTYPE html><html><head>
            <link rel="canonical" href="https://www.youtube.com/watch?v={{liveId}}">
            <script>
            window.ytcfg.set({
                "INNERTUBE_API_KEY": "{{apiKey}}",
                "INNERTUBE_CONTEXT_CLIENT_VERSION": "{{clientVersion}}",
                "INNERTUBE_CONTEXT": { "client": { "clientName": "WEB" } },
                "INITIAL_DATA": { "contents": { "twoColumnWatchNextResults": { "conversationBar": { "liveChatRenderer": { "continuations": [ { "reloadContinuationData": { "continuation": "{{initialContinuation}}" } }]} } } } }
            });
            </script></head><body></body></html>
            """;
    }

    /// <summary>
    /// Returns an initial page HTML indicating the stream is a replay/finished.
    /// </summary>
    public static string GetFinishedStreamPageHtml(string liveId)
    {
        return $$"""
            <!DOCTYPE html><html><head>
            <link rel="canonical" href="https://www.youtube.com/watch?v={{liveId}}">
            <script>
            window.ytcfg.set({
                "INNERTUBE_API_KEY": "TEST_API_KEY_FINISHED",
                "INNERTUBE_CONTEXT_CLIENT_VERSION": "TEST_CLIENT_VERSION_FINISHED",
                "INITIAL_DATA": { "contents": { "twoColumnWatchNextResults": { "conversationBar": { "liveChatRenderer": { "continuations": [ { "reloadContinuationData": { "continuation": "TEST_CONTINUATION_FINISHED" } }]} } } } }
            });
            var moreData = { "isReplay": true };
            </script></head><body></body></html>
            """;
    }
}
