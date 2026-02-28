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

    public static string NewMemberFromLatestLogWithNewMemberBadge() => """
            {
              "id": "ChwKGkNMUEltT1BRM1pJREZTX0J3Z1FkNUljbU1n",
              "timestampUsec": "1771231835991083",
              "authorExternalChannelId": "UCZff3zMXqYWgH-tL1OHBU1g",
              "headerSubtext": {
                "runs": [
                  { "text": "Welcome to " },
                  { "text": "the Ukiverse" },
                  { "text": "!" }
                ]
              },
              "authorName": { "simpleText": "@米喬-h9l" },
              "authorPhoto": {
                "thumbnails": [
                  {
                    "url": "https://yt4.ggpht.com/u5HHJMNL1S5In8-GDajC7s5ANJU_oz5-rK1bYxwDvjZqPwE9wE7AsQ0sbegHZTDWgTMTWwiBXLI=s32-c-k-c0x00ffffff-no-rj",
                    "width": 32,
                    "height": 32
                  },
                  {
                    "url": "https://yt4.ggpht.com/u5HHJMNL1S5In8-GDajC7s5ANJU_oz5-rK1bYxwDvjZqPwE9wE7AsQ0sbegHZTDWgTMTWwiBXLI=s64-c-k-c0x00ffffff-no-rj",
                    "width": 64,
                    "height": 64
                  }
                ]
              },
              "authorBadges": [{
                "liveChatAuthorBadgeRenderer": {
                  "customThumbnail": {
                    "thumbnails": [
                      {
                        "url": "https://yt3.ggpht.com/lnnHYSBx0JQt-my3jZPKLGHiuuVg0XV48VdMb98CDiibHUkMQpO0X-kovuPHQxeZU_X7iZnN1cQ=s16-c-k",
                        "width": 16,
                        "height": 16
                      },
                      {
                        "url": "https://yt3.ggpht.com/lnnHYSBx0JQt-my3jZPKLGHiuuVg0XV48VdMb98CDiibHUkMQpO0X-kovuPHQxeZU_X7iZnN1cQ=s32-c-k",
                        "width": 32,
                        "height": 32
                      }
                    ]
                  },
                  "tooltip": "New member",
                  "accessibility": { "accessibilityData": { "label": "New member" } }
                }
              }]
            }
            """;

    public static string NewMemberLocalizedThreeRuns()
    {
        long ts = GetTimestampUsec(122);
        return $$"""
            {
              "id": "NEW_MEMBER_LOCALIZED_3RUNS",
              "timestampUsec": "{{ts}}",
              "authorExternalChannelId": "UC_NEW_MEMBER_LOCALIZED_01",
              "headerSubtext": {
                "runs": [
                  { "text": "ようこそ " },
                  { "text": "ミトメイトぷち" },
                  { "text": "！" }
                ]
              },
              "authorName": { "simpleText": "@LocalizedMember" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/localized_member_s32.png" }] },
              "authorBadges": [{
                "liveChatAuthorBadgeRenderer": {
                  "customThumbnail": { "thumbnails": [{ "url": "https://yt3.ggpht.com/localized_member_badge_s16.png" }] },
                  "tooltip": "New member",
                  "accessibility": { "accessibilityData": { "label": "New member" } }
                }
              }]
            }
            """;
    }

    public static string NewMemberFromLog6WithMemberTenureBadge() => """
            {
              "id": "ChwKGkNOSExsNVhoM1pJREZiN0t3Z1Fkc3N3VFlR",
              "timestampUsec": "1771236235800174",
              "authorExternalChannelId": "UCNVGqyxXNE4iwVjeUGh_uEw",
              "headerSubtext": {
                "runs": [
                  { "text": "Welcome to " },
                  { "text": "ヘルエスタ王国民シップ" },
                  { "text": "!" }
                ]
              },
              "authorName": { "simpleText": "@しのゆ-j7x" },
              "authorPhoto": {
                "thumbnails": [
                  {
                    "url": "https://yt4.ggpht.com/JUzWYwXznMQcydxN_4Clu_lsAbCpVMg1XpxV6I5Da_Go8a-GDzfWXRMYRM_brqOQFuyrMFQZWQ=s32-c-k-c0x00ffffff-no-rj",
                    "width": 32,
                    "height": 32
                  },
                  {
                    "url": "https://yt4.ggpht.com/JUzWYwXznMQcydxN_4Clu_lsAbCpVMg1XpxV6I5Da_Go8a-GDzfWXRMYRM_brqOQFuyrMFQZWQ=s64-c-k-c0x00ffffff-no-rj",
                    "width": 64,
                    "height": 64
                  }
                ]
              },
              "authorBadges": [
                {
                  "liveChatAuthorBadgeRenderer": {
                    "customThumbnail": {
                      "thumbnails": [
                        {
                          "url": "https://yt3.ggpht.com/A5kmYO7qcuyKYWp4-sdC5ZWk5UYTO3c8Nn0KVXC_hqjTx5bEdguX5faK-zvQvo6RTEsR3PHi3A=s16-c-k",
                          "width": 16,
                          "height": 16
                        },
                        {
                          "url": "https://yt3.ggpht.com/A5kmYO7qcuyKYWp4-sdC5ZWk5UYTO3c8Nn0KVXC_hqjTx5bEdguX5faK-zvQvo6RTEsR3PHi3A=s32-c-k",
                          "width": 32,
                          "height": 32
                        }
                      ]
                    },
                    "tooltip": "Member (1 year)",
                    "accessibility": {
                      "accessibilityData": { "label": "Member (1 year)" }
                    }
                  }
                }
              ]
            }
            """;

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
    public static string NewMemberWithExclamationTier_RatBoss()
    {
        long ts = GetTimestampUsec(130); // Unique offset
        return $$"""
            {
              "id": "NEW_MEMBER_RAT_BOSS_ID",
              "timestampUsec": "{{ts}}",
              "authorExternalChannelId": "UC_CHANNEL_ID_RAT_BOSS",
              "headerSubtext": { "simpleText": "Welcome to Rat Boss!!" },
              "authorName": { "simpleText": "RatBossUser" },
              "authorPhoto": { "thumbnails": [{ "url": "https://yt4.ggpht.com/placeholder_ratboss_s32.png" }] },
              "authorBadges": [{
                "liveChatAuthorBadgeRenderer": {
                  "customThumbnail": { "thumbnails": [{ "url": "https://yt3.ggpht.com/placeholder_ratboss_badge_s16.png" }] },
                  "tooltip": "Member (1 month)",
                  "accessibility": { "accessibilityData": { "label": "Member (1 month)" } }
                }
              }]
            }
            """; // Corresponds to liveChatMembershipItemRenderer
    }
}