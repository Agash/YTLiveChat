namespace YTLiveChat.Tests.TestData;

/// <summary>
/// Provides test data for basic live chat text messages.
/// Each method returns a JSON string representing the 'liveChatTextMessageRenderer' part of an item.
/// </summary>
internal static class TextMessageTestData
{
    private static long GetTimestampUsec(int offsetSeconds = 0) =>
        (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (offsetSeconds * 1000L)) * 1000L;

    public static string SimpleTextMessage1()
    {
        long ts = GetTimestampUsec();
        return $$"""
            {
              "message": { "runs": [{ "text": "Hello World" }] },
              "id": "MSG_ID_SIMPLE_01",
              "timestampUsec": "{{ts}}",
              "authorName": { "simpleText": "TestUser1" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/avatar1_s32.png" }, { "url": "https://yt4.ggpht.com/placeholder/avatar1_s64.png" }] },
              "authorBadges": null,
              "authorExternalChannelId": "UC_CHANNEL_ID_01",
              "contextMenuEndpoint": { "liveChatItemContextMenuEndpoint": { "params": "CONTEXT_PARAMS_01" } },
              "trackingParams": "TRACKING_PARAMS_01"
            }
            """;
    }

    public static string TextMessageWithStandardEmoji()
    {
        long ts = GetTimestampUsec(5);
        return $$"""
            {
              "message": {
                "runs": [
                  { "text": "Congratulations " },
                  {
                    "emoji": {
                      "emojiId": "\uD83E\uDD73", "shortcuts": [":partying_face:"], "searchTerms": ["partying", "face"],
                      "image": { "thumbnails": [{"url": "https://fonts.gstatic.com/s/e/notoemoji/15.1/1f973/72.png"}], "accessibility": {"accessibilityData": {"label": "🥳 Partying Face"} } },
                      "isCustomEmoji": false
                    }
                  }
                ]
              },
              "id": "MSG_ID_STD_EMOJI_01",
              "timestampUsec": "{{ts}}",
              "authorName": { "simpleText": "EmojiFan" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/avatarEF_s32.png" }] },
              "authorBadges": null,
              "authorExternalChannelId": "UC_CHANNEL_ID_EF",
              "contextMenuEndpoint": { "liveChatItemContextMenuEndpoint": { "params": "CONTEXT_PARAMS_02" } },
              "trackingParams": "TRACKING_PARAMS_02"
            }
            """;
    }

    public static string TextMessageWithCustomEmoji()
    {
        long ts = GetTimestampUsec(10);
        return $$"""
            {
              "message": {
                "runs": [
                  { "text": "Check this out: " },
                  {
                    "emoji": {
                      "emojiId": "CUSTOM_EMOJI_ID_CAT", "shortcuts": [":customcat:"], "searchTerms": ["customcat_emote"],
                      "image": { "thumbnails": [{"url": "https://yt3.ggpht.com/placeholder/custom_cat_s48.png"}], "accessibility": {"accessibilityData": {"label": ":customcat:"} } },
                      "isCustomEmoji": true
                    }
                  }
                ]
              },
              "id": "MSG_ID_CUSTOM_EMOJI_01",
              "timestampUsec": "{{ts}}",
              "authorName": { "simpleText": "ChannelSupporter" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/avatarCS_s32.png" }] },
              "authorBadges": null,
              "authorExternalChannelId": "UC_CHANNEL_ID_CS",
              "contextMenuEndpoint": { "liveChatItemContextMenuEndpoint": { "params": "CONTEXT_PARAMS_03" } },
              "trackingParams": "TRACKING_PARAMS_03"
            }
            """;
    }

    public static string MultiPartTextMessageWithMixedContent()
    {
        long ts = GetTimestampUsec(15);
        return $$"""
            {
              "message": {
                "runs": [
                  { "text": "Text part 1, " },
                  {
                    "emoji": {
                      "emojiId": "\uD83D\uDC4D", "shortcuts": [":thumbs_up:"], "searchTerms": ["thumbs", "up"],
                      "image": { "thumbnails": [{"url": "https://fonts.gstatic.com/s/e/notoemoji/15.1/1f44d/72.png"}], "accessibility": {"accessibilityData": {"label": "👍 Thumbs Up"} } },
                      "isCustomEmoji": false
                    }
                  },
                  { "text": " then more text." }
                ]
              },
              "id": "MSG_ID_MIXED_01",
              "timestampUsec": "{{ts}}",
              "authorName": { "simpleText": "MixedUser" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/avatarMU_s32.png" }] },
              "authorBadges": null,
              "authorExternalChannelId": "UC_CHANNEL_ID_MU",
              "contextMenuEndpoint": { "liveChatItemContextMenuEndpoint": { "params": "CONTEXT_PARAMS_04" } },
              "trackingParams": "TRACKING_PARAMS_04"
            }
            """;
    }

    public static string TextMessageWithNonLatinCharacters()
    {
        long ts = GetTimestampUsec(20);
        return $$"""
            {
              "message": { "runs": [{ "text": "Привет мир" }] },
              "id": "MSG_ID_NON_LATIN_01",
              "timestampUsec": "{{ts}}",
              "authorName": { "simpleText": "Пользователь1" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/avatarNL_s32.png" }] },
              "authorBadges": null,
              "authorExternalChannelId": "UC_CHANNEL_ID_NL",
              "contextMenuEndpoint": { "liveChatItemContextMenuEndpoint": { "params": "CONTEXT_PARAMS_05" } },
              "trackingParams": "TRACKING_PARAMS_05"
            }
            """;
    }
}
