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

    /// <summary>
    /// Full addChatItemAction wrapping a liveChatPaidStickerRenderer with a membership author badge
    /// and no lowerBumper. Real data from watch_20260421_070317.jsonl — @buroburo_563,
    /// Member (3 years), ¥1,600, sticker "Sunglasses perpetually fall onto video game controller's proud face".
    /// </summary>
    public static string PaidStickerWithMemberBadge() => """
            {
              "clickTrackingParams": "CAEQl98BIhMI177AhdLokwMV4Ex6BR199Bc3ygEEW0e7Hw==",
              "addChatItemAction": {
                "item": {
                  "liveChatPaidStickerRenderer": {
                    "id": "ChwKGkNPeVE2X19SNkpNREZRWER3Z1FkWlNJYUVR",
                    "contextMenuEndpoint": {
                      "clickTrackingParams": "CAUQ77sEIhMI177AhdLokwMV4Ex6BR199Bc3ygEEW0e7Hw==",
                      "commandMetadata": {
                        "webCommandMetadata": {
                          "ignoreNavigation": true
                        }
                      },
                      "liveChatItemContextMenuEndpoint": {
                        "params": "Q2g0S0hBb2FRMDk1VVRaZlgxSTJTazFFUmxGWVJIZG5VV1JhVTBsaFJWRWFLU29uQ2hoVlEyaEJibkZqWDBGWk5WOUpNMUI0TldScFp6TllNVkVTQzNOb01HbzRSbXd0ZDBkbklBRW9CRElhQ2hoVlEycDVkMUl5Y1VwZk9VSm5VM0JFVW1WU1gyeEZNMmM0QWtnQVVCUSUzRA=="
                      }
                    },
                    "contextMenuAccessibility": {
                      "accessibilityData": {
                        "label": "Chat actions"
                      }
                    },
                    "timestampUsec": "1776008178412469",
                    "authorPhoto": {
                      "thumbnails": [
                        {
                          "url": "https://yt4.ggpht.com/uQv4ZjqYBloLvPxN6JXtGnK5DBkh4PZ09jeAEeffDvOcB1ZN1zB7wbn3X7lyV-waihgsozt8=s32-c-k-c0x00ffffff-no-rj",
                          "width": 32,
                          "height": 32
                        },
                        {
                          "url": "https://yt4.ggpht.com/uQv4ZjqYBloLvPxN6JXtGnK5DBkh4PZ09jeAEeffDvOcB1ZN1zB7wbn3X7lyV-waihgsozt8=s64-c-k-c0x00ffffff-no-rj",
                          "width": 64,
                          "height": 64
                        }
                      ]
                    },
                    "authorName": {
                      "simpleText": "@buroburo_563"
                    },
                    "authorExternalChannelId": "UCjywR2qJ_9BgSpDReR_lE3g",
                    "sticker": {
                      "thumbnails": [
                        {
                          "url": "//lh3.googleusercontent.com/7NUanCM7WTzYks25uS2uYxdMLzKo09_p5IKE--vikS7FYXyRFDzIUQt0L7QIdKm3nxMDcRBhp0NNFAdDbQ=s104-rg",
                          "width": 104,
                          "height": 104
                        },
                        {
                          "url": "//lh3.googleusercontent.com/7NUanCM7WTzYks25uS2uYxdMLzKo09_p5IKE--vikS7FYXyRFDzIUQt0L7QIdKm3nxMDcRBhp0NNFAdDbQ=s208-rg",
                          "width": 208,
                          "height": 208
                        }
                      ],
                      "accessibility": {
                        "accessibilityData": {
                          "label": "Sunglasses perpetually fall onto video game controller's proud face"
                        }
                      }
                    },
                    "authorBadges": [
                      {
                        "liveChatAuthorBadgeRenderer": {
                          "customThumbnail": {
                            "thumbnails": [
                              {
                                "url": "https://yt3.ggpht.com/yocNS0Uw2yJ1Ph4gHS8o1q83HEEF-BBUSezHCQ56z80b_dSB9v80B3gYwneOhN_cLNCOZe9fDw=s16-c-k",
                                "width": 16,
                                "height": 16
                              },
                              {
                                "url": "https://yt3.ggpht.com/yocNS0Uw2yJ1Ph4gHS8o1q83HEEF-BBUSezHCQ56z80b_dSB9v80B3gYwneOhN_cLNCOZe9fDw=s32-c-k",
                                "width": 32,
                                "height": 32
                              }
                            ]
                          },
                          "tooltip": "Member (3 years)",
                          "accessibility": {
                            "accessibilityData": {
                              "label": "Member (3 years)"
                            }
                          }
                        }
                      }
                    ],
                    "moneyChipBackgroundColor": 4294953512,
                    "moneyChipTextColor": 3741319168,
                    "purchaseAmountText": {
                      "simpleText": "¥1,600"
                    },
                    "stickerDisplayWidth": 104,
                    "stickerDisplayHeight": 104,
                    "backgroundColor": 4294953512,
                    "authorNameTextColor": 2315255808,
                    "trackingParams": "CAUQ77sEIhMI177AhdLokwMV4Ex6BR199Bc3",
                    "isV2Style": true
                  }
                },
                "clientId": "COyQ6__R6JMDFQXDwgQdZSIaEQ"
              }
            }
            """;

    /// <summary>
    /// Full addChatItemAction wrapping a liveChatPaidStickerRenderer with lowerBumper.
    /// Real data from watch_20260421_075106.jsonl — @shujieh2297, 1st Super, NT$14.00,
    /// sticker "Beaming face with smiling eyes". Has two thumbnail sources (s40/s80)
    /// to confirm LastOrDefault (highest-res) is used.
    /// </summary>
    public static string PaidStickerWithLowerBumper() => """
            {
              "clickTrackingParams": "CAEQl98BIhMIjLXdsob_kwMV98tPCB31Ww6TygEEdZ2q5A==",
              "addChatItemAction": {
                "item": {
                  "liveChatPaidStickerRenderer": {
                    "id": "ChwKGkNLcWltb1NHXzVNREZaYjF3Z1FkVUd3Sl9R",
                    "contextMenuEndpoint": {
                      "clickTrackingParams": "CAIQ77sEIhMIjLXdsob_kwMV98tPCB31Ww6TygEEdZ2q5A==",
                      "commandMetadata": {
                        "webCommandMetadata": {
                          "ignoreNavigation": true
                        }
                      },
                      "liveChatItemContextMenuEndpoint": {
                        "params": "Q2g0S0hBb2FRMHR4YVcxdlUwZGZOVTFFUmxwaU1YZG5VV1JWUjNkS1gxRWFLU29uQ2hoVlF6QlVXR1ZmVEZsYU5ITmpZVmN5V0UxNWFUVmZhM2NTQzJKNVUyaGtSMW90ZDFGM0lBRW9CRElhQ2hoVlEwRmtUSGhWWDJ4dVFqZEtjbW8wUjJ0SFYwTjFaa0U0QWtnQVVCUSUzRA=="
                      }
                    },
                    "contextMenuAccessibility": {
                      "accessibilityData": {
                        "label": "Chat actions"
                      }
                    },
                    "timestampUsec": "1776778145964447",
                    "authorPhoto": {
                      "thumbnails": [
                        {
                          "url": "https://yt4.ggpht.com/ytc/AIdro_nlK0XZCHPihWVQ4ZXtf5b8YpsaViKGdlcNQOujPdbuOV75OJmCMvjy0UMhDlldDDfeTw=s32-c-k-c0x00ffffff-no-rj",
                          "width": 32,
                          "height": 32
                        },
                        {
                          "url": "https://yt4.ggpht.com/ytc/AIdro_nlK0XZCHPihWVQ4ZXtf5b8YpsaViKGdlcNQOujPdbuOV75OJmCMvjy0UMhDlldDDfeTw=s64-c-k-c0x00ffffff-no-rj",
                          "width": 64,
                          "height": 64
                        }
                      ]
                    },
                    "authorName": {
                      "simpleText": "@shujieh2297"
                    },
                    "authorExternalChannelId": "UCAdLxU_lnB7Jrj4GkGWCufA",
                    "sticker": {
                      "thumbnails": [
                        {
                          "url": "//lh3.googleusercontent.com/yAtGAw9ew-yy9o6oQ9EDVAfbmusNmazN-nunVbcixsmCIFER30HMdjt5nchJ6viSBuYNfrMwwBrkZ83oFA=s40-rp",
                          "width": 40,
                          "height": 40
                        },
                        {
                          "url": "//lh3.googleusercontent.com/yAtGAw9ew-yy9o6oQ9EDVAfbmusNmazN-nunVbcixsmCIFER30HMdjt5nchJ6viSBuYNfrMwwBrkZ83oFA=s80-rp",
                          "width": 80,
                          "height": 80
                        }
                      ],
                      "accessibility": {
                        "accessibilityData": {
                          "label": "Beaming face with smiling eyes"
                        }
                      }
                    },
                    "moneyChipBackgroundColor": 4280191205,
                    "moneyChipTextColor": 4294967295,
                    "purchaseAmountText": {
                      "simpleText": "NT$14.00"
                    },
                    "stickerDisplayWidth": 40,
                    "stickerDisplayHeight": 40,
                    "backgroundColor": 4280191205,
                    "authorNameTextColor": 3019898879,
                    "trackingParams": "CAIQ77sEIhMIjLXdsob_kwMV98tPCB31Ww6T",
                    "headerOverlayImage": {
                      "thumbnails": [
                        {
                          "url": "https://www.gstatic.com/youtube/img/pdg/novelty/1st_purchase_celebration_novelty_animation/1st_Purchase_Celebration_Novelty_STK_IL_T1_v2.webp",
                          "width": 68,
                          "height": 48
                        }
                      ]
                    },
                    "isV2Style": true,
                    "pdgPurchasedNoveltyLoggingDirectives": {
                      "loggingDirectives": {
                        "trackingParams": "CAUQ7s4LIhMIjLXdsob_kwMV98tPCB31Ww6T",
                        "visibility": {
                          "types": "4"
                        }
                      }
                    },
                    "lowerBumper": {
                      "liveChatItemBumperViewModel": {
                        "content": {
                          "bumperUserEduContentViewModel": {
                            "text": {
                              "content": "Let's celebrate their 1st Super on a live stream",
                              "styleRuns": [
                                {
                                  "startIndex": 0,
                                  "length": 48
                                }
                              ]
                            },
                            "trackingParams": "CAQQk5YLIhMIjLXdsob_kwMV98tPCB31Ww6T",
                            "image": {
                              "sources": [
                                {
                                  "clientResource": {
                                    "imageName": "CELEBRATION",
                                    "imageColor": 4294901760
                                  }
                                }
                              ]
                            }
                          }
                        },
                        "pdgPurchasedBumperLoggingDirectives": {
                          "loggingDirectives": {
                            "trackingParams": "CAMQ784LIhMIjLXdsob_kwMV98tPCB31Ww6T",
                            "visibility": {
                              "types": "4"
                            }
                          }
                        }
                      }
                    }
                  }
                },
                "clientId": "CKqimoSG_5MDFZb1wgQdUGwJ_Q"
              }
            }
            """;
}
