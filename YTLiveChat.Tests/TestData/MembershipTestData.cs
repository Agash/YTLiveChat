// YTLiveChat.Tests/TestData/MembershipTestData.cs
namespace YTLiveChat.Tests.TestData;

internal static class MembershipTestData
{
    private static long GetTimestampUsec(int offsetSeconds = 0) =>
        (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (offsetSeconds * 1000L)) * 1000L;

    public static string MembershipMilestone27Months()
    {
        long ts = GetTimestampUsec(100);
        return $$"""
            {
              "headerPrimaryText": { "runs": [{ "text": "Member for " }, { "text": "27" }, { "text": " months" }] },
              "headerSubtext": { "simpleText": "The Fam" },
              "message": { "runs": [{ "text": "YOOOOOO hope all is a well my man" }] },
              "id": "MILESTONE_ID_27M",
              "timestampUsec": "{{ts}}",
              "authorName": { "simpleText": "MilestoneUser27" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/avatarMS27_s32.png" }] },
              "authorBadges": [{
                  "liveChatAuthorBadgeRenderer": {
                    "customThumbnail": { "thumbnails": [{"url": "https://yt3.ggpht.com/placeholder/badge_2yr_s32.png"}] },
                    "tooltip": "Member (2 years)" 
                  }
              }],
              "authorExternalChannelId": "UC_CHANNEL_ID_MILESTONE_27M"
            }
            """;
    }

    public static string MembershipMilestone9Months()
    {
        long ts = GetTimestampUsec(110);
        return $$"""
            {
              "headerPrimaryText": { "runs": [{ "text": "Member for " }, { "text": "9" }, { "text": " months" }] },
              "headerSubtext": { "simpleText": "The Fam" },
              "message": { "runs": [{ "text": "missed a bit, what's going on?" }] },
              "id": "MILESTONE_ID_9M",
              "timestampUsec": "{{ts}}",
              "authorName": { "simpleText": "MilestoneUser9" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/avatarMS9_s32.png" }] },
              "authorBadges": [{
                  "liveChatAuthorBadgeRenderer": {
                    "customThumbnail": { "thumbnails": [{"url": "https://yt3.ggpht.com/placeholder/badge_6mo_s32.png"}] },
                    "tooltip": "Member (6 months)"
                  }
              }],
              "authorExternalChannelId": "UC_CHANNEL_ID_MILESTONE_9M"
            }
            """;
    }

    // This method was called by ParserTests but might have been missing from this file with this exact name
    public static string NewMemberChickenMcNugget()
    {
        long ts = GetTimestampUsec(120); // Unique offset
        return $$"""
            {
              "id": "NEW_MEMBER_CHICKEN_ID",
              "timestampUsec": "{{ts}}",
              "authorExternalChannelId": "UC6pydynNYoEGABzcY_zuolw_placeholder",
              "headerSubtext": { "runs": [{ "text": "Welcome to " }, { "text": "The Plusers" }, { "text": "!" }] },
              "authorName": { "simpleText": "ChickenMcNuggetPlaceholder" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder_chicken_s32.png" }] },
              "authorBadges": [{
                "liveChatAuthorBadgeRenderer": {
                  "customThumbnail": { "thumbnails": [{ "url": "https://yt3.ggpht.com/placeholder_chicken_badge_s16.png" }] },
                  "tooltip": "Member (6 months)",
                  "accessibility": { "accessibilityData": { "label": "Member (6 months)" } }
                }
              }]
            }
            """; // Corresponds to liveChatMembershipItemRenderer
    }

    public static string GiftPurchase_1_Gift_Kelly()
    {
        long ts = GetTimestampUsec(500);
        return $$"""
            {
              "header": {
                "liveChatSponsorshipsHeaderRenderer": {
                  "authorName": { "simpleText": "KellyTheGifter" },
                  "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/avatarKG_s32.png" }] },
                  "primaryText": { "runs": [{ "text": "Sent "}, {"text": "1"}, {"text": " "}, {"text": "RaidAway+"}, {"text": " gift memberships"}] },
                  "authorBadges": null 
                }
              },
              "id": "GIFT_PURCHASE_ID_KELLY",
              "timestampUsec": "{{ts}}",
              "authorExternalChannelId": "UC_CHANNEL_ID_GIFTER_KELLY"
            }
            """;
    }

    public static string GiftPurchase_20_Gifts_WhittBud_Mod()
    {
        long ts = GetTimestampUsec(501);
        return $$"""
            {
              "header": {
                "liveChatSponsorshipsHeaderRenderer": {
                  "authorName": { "simpleText": "WhittTheModGifter" },
                  "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/avatarWMG_s32.png" }] },
                  "primaryText": { "runs": [{"text": "Sent "}, {"text": "20"}, {"text": " "}, {"text": "RaidAway+"}, {"text": " gift memberships"}] },
                  "authorBadges": [
                    { "liveChatAuthorBadgeRenderer": { "icon": { "iconType": "MODERATOR" }, "tooltip": "Moderator" } },
                    { "liveChatAuthorBadgeRenderer": { "customThumbnail": { "thumbnails": [{"url":"https://yt3.ggpht.com/placeholder/badge_whitt_s16.png"}] }, "tooltip": "Member (2 months)" } }
                  ]
                }
              },
              "id": "GIFT_PURCHASE_ID_WHITT",
              "timestampUsec": "{{ts}}",
              "authorExternalChannelId": "UC_CHANNEL_ID_GIFTER_WHITT"
            }
            """;
    }

    public static string GiftPurchase_5_Gifts_JanaBeh()
    {
        long ts = GetTimestampUsec(502);
        return $$"""
            {
              "header": {
                "liveChatSponsorshipsHeaderRenderer": {
                  "authorName": { "simpleText": "JanaTheGifter" },
                  "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/avatarJG_s32.png" }] },
                  "primaryText": { "runs": [{"text": "Sent "}, {"text": "5"}, {"text": " "}, {"text": "RaidAway+"}, {"text": " gift memberships"}] },
                  "authorBadges": null
                }
              },
              "id": "GIFT_PURCHASE_ID_JANA",
              "timestampUsec": "{{ts}}",
              "authorExternalChannelId": "UC_CHANNEL_ID_GIFTER_JANA"
            }
            """;
    }

    public static string GiftPurchase_50_Gifts_Derpickie_Mod()
    {
        long ts = GetTimestampUsec(503);
        return $$"""
            {
              "header": {
                "liveChatSponsorshipsHeaderRenderer": {
                  "authorName": { "simpleText": "DerpickieTheModGifter" },
                  "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/avatarDPG_s32.png" }] },
                  "primaryText": { "runs": [{"text": "Sent "}, {"text": "50"}, {"text": " "}, {"text": "RaidAway+"}, {"text": " gift memberships"}] },
                  "authorBadges": [
                    { "liveChatAuthorBadgeRenderer": { "icon": { "iconType": "MODERATOR" }, "tooltip": "Moderator" } },
                    { "liveChatAuthorBadgeRenderer": { "customThumbnail": { "thumbnails": [{"url":"https://yt3.ggpht.com/placeholder/badge_derpickie_s16.png"}] }, "tooltip": "Member (6 months)" } }
                  ]
                }
              },
              "id": "GIFT_PURCHASE_ID_DERPICKIE",
              "timestampUsec": "{{ts}}",
              "authorExternalChannelId": "UC_CHANNEL_ID_GIFTER_DERPICKIE"
            }
            """;
    }

    public static string GiftRedemption_FromKelly_ToHikari()
    {
        long ts = GetTimestampUsec(510);
        return $$"""
            {
              "message": { "runs": [ {"text": "received a gift membership by ", "italics": true }, {"text": "Kelly Lewis", "bold": true, "italics": true} ] },
              "id": "GIFT_REDEMPTION_ID_HIKARI",
              "timestampUsec": "{{ts}}",
              "authorName": { "simpleText": "HikariTheRecipient" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/avatarHR_s32.png" }] },
              "authorBadges": null, 
              "authorExternalChannelId": "UC_CHANNEL_ID_RECIPIENT_HIKARI"
            }
            """;
    }

    public static string GiftRedemption_FromJana_ToJackalope()
    {
        long ts = GetTimestampUsec(512);
        return $$"""
            {
              "message": { "runs": [ {"text": "received a gift membership by ", "italics": true }, {"text": "Jana Beh", "bold": true, "italics": true} ] },
              "id": "GIFT_REDEMPTION_ID_JACKALOPE",
              "timestampUsec": "{{ts}}",
              "authorName": { "simpleText": "JackalopeTheRecipient" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder/avatarJLR_s32.png" }] },
              "authorBadges": null, 
              "authorExternalChannelId": "UC_CHANNEL_ID_RECIPIENT_JACKALOPE"
            }
            """;
    }
}
