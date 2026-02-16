namespace YTLiveChat.Tests.TestData;

/// <summary>
/// Provides test data for Super Chat and Super Sticker messages.
/// Each method returns a JSON string representing the renderer part of an item.
/// </summary>
internal static class SuperChatTestData
{
    private static long GetTimestampUsec(int offsetSeconds = 0) =>
        (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (offsetSeconds * 1000L)) * 1000L;

    private static string BuildPaidMessageRendererJson(
        string id,
        string authorName,
        string channelId,
        string amountString,
        int offsetSeconds
    )
    {
        long ts = GetTimestampUsec(offsetSeconds);
        return $$"""
            {
              "message": { "runs": [{ "text": "currency parsing sample" }] },
              "purchaseAmountText": { "simpleText": "{{amountString}}" },
              "headerBackgroundColor": 4294947584,
              "headerTextColor": 3741319168,
              "bodyBackgroundColor": 4294953512,
              "bodyTextColor": 3741319168,
              "authorNameTextColor": 2315255808,
              "timestampColor": 2147483648,
              "isV2Style": true,
              "textInputBackgroundColor": 822083583,
              "id": "{{id}}",
              "timestampUsec": "{{ts}}",
              "authorName": { "simpleText": "{{authorName}}" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/currency_sample_s32.png" }] },
              "authorBadges": null,
              "authorExternalChannelId": "{{channelId}}",
              "contextMenuEndpoint": { "liveChatItemContextMenuEndpoint": { "params": "CONTEXT_PARAMS_CURRENCY_SAMPLE" } },
              "trackingParams": "TRACKING_PARAMS_CURRENCY_SAMPLE"
            }
            """;
    }

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

    public static string SuperChatMessageAudPrefixSymbol() =>
        BuildPaidMessageRendererJson(
            "SC_ID_CURRENCY_AUD_01",
            "AudSupporter",
            "UC_CHANNEL_ID_AUD_01",
            "A$10.00",
            360
        );

    public static string SuperChatMessageHkdPrefixSymbol() =>
        BuildPaidMessageRendererJson(
            "SC_ID_CURRENCY_HKD_01",
            "HkdSupporter",
            "UC_CHANNEL_ID_HKD_01",
            "HK$25.00",
            361
        );

    public static string SuperChatMessagePlnCode() =>
        BuildPaidMessageRendererJson(
            "SC_ID_CURRENCY_PLN_01",
            "PlnSupporter",
            "UC_CHANNEL_ID_PLN_01",
            "10.00 PLN",
            362
        );

    public static string SuperChatMessageArsCodePrefix() =>
        BuildPaidMessageRendererJson(
            "SC_ID_CURRENCY_ARS_01",
            "ArsSupporter",
            "UC_CHANNEL_ID_ARS_01",
            "ARS 2500",
            363
        );

    public static string SuperChatMessageVndSymbol() =>
        BuildPaidMessageRendererJson(
            "SC_ID_CURRENCY_VND_01",
            "VndSupporter",
            "UC_CHANNEL_ID_VND_01",
            "₫20,000",
            364
        );
}
