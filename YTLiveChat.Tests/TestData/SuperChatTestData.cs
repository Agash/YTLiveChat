// YTLiveChat.Tests/TestData/SuperChatTestData.cs
namespace YTLiveChat.Tests.TestData;

/// <summary>
/// Provides test data for Super Chat and Super Sticker messages.
/// Each method returns a JSON string representing the renderer part of an item.
/// </summary>
internal static class SuperChatTestData
{
    private static long GetTimestampUsec(int offsetSeconds = 0) =>
        (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (offsetSeconds * 1000L)) * 1000L;

    public static string SuperChatMessagePaidMessage1()
    {
        long ts = GetTimestampUsec(300);
        return $$"""
            {
              "message": { "runs": [{ "text": "Great stream! Keep it up!" }] },
              "purchaseAmountText": { "simpleText": "$10.00" },
              "headerBackgroundColor": 4294947584, 
              "headerTextColor": 3741319168, 
              "bodyBackgroundColor": 4294953512, 
              "bodyTextColor": 3741319168, 
              "authorNameTextColor": 2315255808,
              "timestampColor": 2147483648,
              "isV2Style": true,
              "textInputBackgroundColor": 822083583,
              "id": "SC_ID_PAID_MSG_01",
              "timestampUsec": "{{ts}}",
              "authorName": { "simpleText": "SuperFanPaidMsg1" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/avatarSFPM1_s32.png" }] },
              "authorBadges": null,
              "authorExternalChannelId": "UC_CHANNEL_ID_SF_PM_01",
              "contextMenuEndpoint": { "liveChatItemContextMenuEndpoint": { "params": "CONTEXT_PARAMS_SC_PM_01" } },
              "trackingParams": "TRACKING_PARAMS_SC_PM_01"
            }
            """;
    }

    public static string SuperChatMessagePaidMessage2_DifferentAmountAndCurrency()
    {
        long ts = GetTimestampUsec(425);
        return $$"""
            {
              "message": { "runs": [{ "text": "This is awesome!" }] },
              "purchaseAmountText": { "simpleText": "€5.00" },
              "headerBackgroundColor": 4278239141, 
              "headerTextColor": 4278190080, 
              "bodyBackgroundColor": 4280150454, 
              "bodyTextColor": 4278190080, 
              "authorNameTextColor": 2315255808,
              "timestampColor": 2147483648,
              "isV2Style": true,
              "textInputBackgroundColor": 822083583,
              "id": "SC_ID_PLACEHOLDER_02",
              "timestampUsec": "{{ts}}",
              "authorName": { "simpleText": "EuroSupporter" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/avatarES_s32.png" }] },
              "authorBadges": null,
              "authorExternalChannelId": "UC_CHANNEL_ID_ES_01",
              "contextMenuEndpoint": { "liveChatItemContextMenuEndpoint": { "params": "CONTEXT_PARAMS_SC_02" } },
              "trackingParams": "TRACKING_PARAMS_SC_02"
            }
            """;
    }

    // This method was called by ParserTests but might have been missing from this file in a previous step
    public static string SuperChatMessageFromLatestLog() // ArmbarAssassin $10.00
    {
        long ts = GetTimestampUsec(355); // Ensure unique offset
        return $$"""
            {
              "message": { "runs": [{ "text": "Rich and Kylie Subathon make it happen captain. At 3 rip shots brotha" }] },
              "purchaseAmountText": { "simpleText": "$10.00" },
              "headerBackgroundColor": 4294947584,
              "headerTextColor": 3741319168,
              "bodyBackgroundColor": 4294953512,
              "bodyTextColor": 3741319168,
              "authorNameTextColor": 2315255808,
              "timestampColor": 2147483648,
              "isV2Style": true,
              "textInputBackgroundColor": 822083583,
              "id": "SC_ID_LATEST_01",
              "timestampUsec": "{{ts}}",
              "authorName": { "simpleText": "ArmbarAssassinPlaceholder" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/assassin_s32.png" }] },
              "authorBadges": null,
              "authorExternalChannelId": "UC_CHANNEL_ID_ASSASSIN_LATEST",
              "contextMenuEndpoint": { "liveChatItemContextMenuEndpoint": { "params": "CONTEXT_PARAMS_SC_LATEST_01" } },
              "trackingParams": "TRACKING_PARAMS_SC_LATEST_01"
            }
            """;
    }
}
