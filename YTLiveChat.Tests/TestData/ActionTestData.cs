namespace YTLiveChat.Tests.TestData;

internal static class ActionTestData
{
    public static string ViewerEngagementSubscribersOnly()
    {
        long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000L;
        return $$"""
            {
              "addChatItemAction": {
                "item": {
                  "liveChatViewerEngagementMessageRenderer": {
                    "id": "VE_MSG_TEST_01",
                    "timestampUsec": "{{ts}}",
                    "icon": { "iconType": "YOUTUBE_ROUND" },
                    "message": {
                      "runs": [
                        { "text": "Subscribers-only mode. Messages that appear are from people who've subscribed to this channel for " },
                        { "text": "10 minutes" },
                        { "text": " or longer." }
                      ]
                    }
                  }
                },
                "clientId": "TEST_CLIENT_ID_VE_01"
              }
            }
            """;
    }

    public static string AddBannerPinnedMessage()
    {
        return """
            {
              "addBannerToLiveChatCommand": {
                "bannerRenderer": {
                  "liveChatBannerRenderer": {
                    "header": {
                      "liveChatBannerHeaderRenderer": {
                        "icon": { "iconType": "KEEP" },
                        "text": {
                          "runs": [
                            { "text": "Pinned by " },
                            { "text": "@Host" }
                          ]
                        }
                      }
                    },
                    "contents": {
                      "liveChatTextMessageRenderer": {
                        "message": { "runs": [{ "text": "Pinned message body" }] },
                        "id": "PINNED_TEXT_ID_01",
                        "authorName": { "simpleText": "@Host" },
                        "authorExternalChannelId": "UC_HOST_01"
                      }
                    },
                    "actionId": "PINNED_ACTION_ID_01",
                    "targetId": "live-chat-banner"
                  }
                }
              }
            }
            """;
    }

    public static string RemoveChatItem()
    {
        return """
            {
              "removeChatItemAction": {
                "targetItemId": "REMOVED_MSG_ID_01"
              }
            }
            """;
    }

    public static string ReportModerationStateEmpty()
    {
        return """
            {
              "liveChatReportModerationStateCommand": {}
            }
            """;
    }
}
