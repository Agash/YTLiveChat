namespace YTLiveChat.Tests.TestData;

internal static class ActionTestData
{
    /// <summary>
    /// Subscribers-only mode engagement message (5 minutes).
    /// Real data from dump_engagement.json (first entry).
    /// </summary>
    public static string ViewerEngagementSubscribersOnly() => """
            {
              "addChatItemAction": {
                "item": {
                  "liveChatViewerEngagementMessageRenderer": {
                    "id": "Ci0KK1NVQlNDUklCRVJTX09OTFlfVkVNMjAyNi8wNC8xMS0wNDo1NjowNC41Nzk%3D",
                    "timestampUsec": "1775908564579677",
                    "icon": {
                      "iconType": "YOUTUBE_ROUND"
                    },
                    "message": {
                      "runs": [
                        { "text": "Subscribers-only mode. Messages that appear are from people who\u2019ve subscribed to this channel for " },
                        { "text": "5 minutes" },
                        { "text": " or longer." }
                      ]
                    },
                    "actionButton": {
                      "buttonRenderer": {
                        "style": "STYLE_BLUE_TEXT",
                        "size": "SIZE_DEFAULT",
                        "isDisabled": false,
                        "text": {
                          "simpleText": "Learn more"
                        },
                        "navigationEndpoint": {
                          "clickTrackingParams": "CB0Q8FsiEwih2ZL53uWTAxVTlvQHHQeDCorKAQQ1LYS7",
                          "commandMetadata": {
                            "webCommandMetadata": {
                              "url": "//support.google.com/youtube/?p=subs_only_chat_viewer\u0026hl=en",
                              "webPageType": "WEB_PAGE_TYPE_UNKNOWN",
                              "rootVe": 83769
                            }
                          },
                          "urlEndpoint": {
                            "url": "//support.google.com/youtube/?p=subs_only_chat_viewer\u0026hl=en",
                            "target": "TARGET_NEW_WINDOW"
                          }
                        },
                        "trackingParams": "CB0Q8FsiEwih2ZL53uWTAxVTlvQHHQeDCoo=",
                        "accessibilityData": {
                          "accessibilityData": {
                            "label": "Learn more"
                          }
                        }
                      }
                    },
                    "trackingParams": "CAEQl98BIhMIodmS-d7lkwMVU5b0Bx0HgwqK"
                  }
                }
              }
            }
            """;

    /// <summary>
    /// Subscribers-only mode engagement message (20 minutes).
    /// Real data from dump_engagement.json (second entry).
    /// </summary>
    public static string ViewerEngagementSubscribersOnly20Min() => """
            {
              "addChatItemAction": {
                "item": {
                  "liveChatViewerEngagementMessageRenderer": {
                    "id": "Ci0KK1NVQlNDUklCRVJTX09OTFlfVkVNMjAyNi8wNC8xMS0wNDo1NjowNC41NTM%3D",
                    "timestampUsec": "1775908564553221",
                    "icon": {
                      "iconType": "YOUTUBE_ROUND"
                    },
                    "message": {
                      "runs": [
                        { "text": "Subscribers-only mode. Messages that appear are from people who\u2019ve subscribed to this channel for " },
                        { "text": "20 minutes" },
                        { "text": " or longer." }
                      ]
                    },
                    "actionButton": {
                      "buttonRenderer": {
                        "style": "STYLE_BLUE_TEXT",
                        "size": "SIZE_DEFAULT",
                        "isDisabled": false,
                        "text": {
                          "simpleText": "Learn more"
                        },
                        "navigationEndpoint": {
                          "clickTrackingParams": "CB4Q8FsiEwi6m5H53uWTAxWsjPQHHQfUBYXKAQQKZYme",
                          "commandMetadata": {
                            "webCommandMetadata": {
                              "url": "//support.google.com/youtube/?p=subs_only_chat_viewer\u0026hl=en",
                              "webPageType": "WEB_PAGE_TYPE_UNKNOWN",
                              "rootVe": 83769
                            }
                          },
                          "urlEndpoint": {
                            "url": "//support.google.com/youtube/?p=subs_only_chat_viewer\u0026hl=en",
                            "target": "TARGET_NEW_WINDOW"
                          }
                        },
                        "trackingParams": "CB4Q8FsiEwi6m5H53uWTAxWsjPQHHQfUBYU=",
                        "accessibilityData": {
                          "accessibilityData": {
                            "label": "Learn more"
                          }
                        }
                      }
                    },
                    "trackingParams": "CAEQl98BIhMIupuR-d7lkwMVrIz0Bx0H1AWF"
                  }
                }
              }
            }
            """;

    /// <summary>
    /// Community guidelines reminder engagement message.
    /// Real data from dump_engagement.json (third entry).
    /// </summary>
    public static string ViewerEngagementCommunityGuidelines() => """
            {
              "addChatItemAction": {
                "item": {
                  "liveChatViewerEngagementMessageRenderer": {
                    "id": "CjEKL0NPTU1VTklUWV9HVUlERUxJTkVTX1ZFTTIwMjYvMDQvMTEtMDQ6NTY6MDQuNjEx",
                    "timestampUsec": "1775908564612021",
                    "icon": {
                      "iconType": "YOUTUBE_ROUND"
                    },
                    "message": {
                      "runs": [
                        { "text": "Welcome to live chat! Remember to guard your privacy and abide by our community guidelines." }
                      ]
                    },
                    "actionButton": {
                      "buttonRenderer": {
                        "style": "STYLE_BLUE_TEXT",
                        "size": "SIZE_DEFAULT",
                        "isDisabled": false,
                        "text": {
                          "simpleText": "Learn more"
                        },
                        "navigationEndpoint": {
                          "clickTrackingParams": "CB4Q8FsiEwiNvpT53uWTAxV7enoFHbYxBibKAQT1hpv_",
                          "commandMetadata": {
                            "webCommandMetadata": {
                              "url": "//support.google.com/youtube/answer/2853856?hl=en#safe",
                              "webPageType": "WEB_PAGE_TYPE_UNKNOWN",
                              "rootVe": 83769
                            }
                          },
                          "urlEndpoint": {
                            "url": "//support.google.com/youtube/answer/2853856?hl=en#safe",
                            "target": "TARGET_NEW_WINDOW"
                          }
                        },
                        "trackingParams": "CB4Q8FsiEwiNvpT53uWTAxV7enoFHbYxBiY=",
                        "accessibilityData": {
                          "accessibilityData": {
                            "label": "Learn more"
                          }
                        }
                      }
                    },
                    "trackingParams": "CAEQl98BIhMIjb6U-d7lkwMVe3p6BR22MQYm"
                  }
                }
              }
            }
            """;

    /// <summary>
    /// Poll result summary engagement message (POLL icon, no timestamp, no actionButton).
    /// Real data from dump_engagement.json (seventh entry).
    /// </summary>
    public static string ViewerEngagementPollResult() => """
            {
              "addChatItemAction": {
                "item": {
                  "liveChatViewerEngagementMessageRenderer": {
                    "id": "ChwKGkNNS082cVdsNXBNREZicGJUQWdkME1VN2x3",
                    "icon": {
                      "iconType": "POLL"
                    },
                    "message": {
                      "runs": [
                        { "text": "DIG (70%)" },
                        { "text": "\n" },
                        { "text": "ZZZZZ (30%)" },
                        { "text": "\n" },
                        { "text": "\n" },
                        { "text": "Poll complete: 1.3K votes" }
                      ]
                    },
                    "contextMenuEndpoint": {
                      "commandMetadata": {
                        "webCommandMetadata": {
                          "ignoreNavigation": true
                        }
                      },
                      "liveChatItemContextMenuEndpoint": {
                        "params": "Q2g0S0hBb2FRMDFMVHpaeFYydzFjRTFFUm1Kd1lsUkJaMlF3VFZVM2JIY2FLU29uQ2hoVlEyUnVOVUpSTURaWWNXZFliMEY0U1doaWNYYzFVbWNTQzNodmFtTkhWVTFhVGxvMElBRW9CRElhQ2hoVlEyUnVOVUpSTURaWWNXZFliMEY0U1doaWNYYzFVbWM0QWtnQVVCWSUzRA=="
                      }
                    },
                    "contextMenuAccessibility": {
                      "accessibilityData": {
                        "label": "Chat actions"
                      }
                    }
                  }
                }
              }
            }
            """;

    public static string AddBannerPinnedMessage() => """
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
                        "timestampUsec": "1776004576422639",
                        "authorName": { "simpleText": "@Host" },
                        "authorExternalChannelId": "UC_HOST_01",
                        "authorPhoto": {
                          "thumbnails": [
                            { "url": "https://yt4.ggpht.com/test_avatar=s32", "width": 32, "height": 32 },
                            { "url": "https://yt4.ggpht.com/test_avatar=s64", "width": 64, "height": 64 }
                          ]
                        },
                        "authorBadges": [
                          {
                            "liveChatAuthorBadgeRenderer": {
                              "icon": { "iconType": "VERIFIED" },
                              "tooltip": "Verified",
                              "accessibility": { "accessibilityData": { "label": "Verified" } }
                            }
                          }
                        ]
                      }
                    },
                    "actionId": "PINNED_ACTION_ID_01",
                    "bannerType": "LIVE_CHAT_BANNER_TYPE_PINNED_MESSAGE",
                    "targetId": "live-chat-banner"
                  }
                }
              }
            }
            """;

    /// <summary>
    /// Cross-channel redirect banner from real live capture (watch_20260412_152414.jsonl).
    /// Banner type: LIVE_CHAT_BANNER_TYPE_CROSS_CHANNEL_REDIRECT
    /// </summary>
    public static string AddBannerRedirectCommand() => """
            {
              "addBannerToLiveChatCommand": {
                "bannerRenderer": {
                  "liveChatBannerRenderer": {
                    "contents": {
                      "liveChatBannerRedirectRenderer": {
                        "bannerMessage": {
                          "runs": [
                            { "text": "Don't miss out! People are going to watch something from ", "fontFace": "FONT_FACE_ROBOTO_REGULAR" },
                            { "text": "@TakanashiKiara", "bold": true, "fontFace": "FONT_FACE_ROBOTO_REGULAR" }
                          ]
                        },
                        "authorPhoto": {
                          "thumbnails": [
                            { "url": "https://yt4.ggpht.com/vnzn_RiKneABPPnp1-0SO4IAZQRXqVsL5RNDQYGR9GhT-Flm47vM4UJeyGfn4U_gteKqJMBwNA=s32-c-k-c0x00ffffff-no-rj", "width": 32, "height": 32 },
                            { "url": "https://yt4.ggpht.com/vnzn_RiKneABPPnp1-0SO4IAZQRXqVsL5RNDQYGR9GhT-Flm47vM4UJeyGfn4U_gteKqJMBwNA=s64-c-k-c0x00ffffff-no-rj", "width": 64, "height": 64 }
                          ]
                        },
                        "inlineActionButton": {
                          "buttonRenderer": {
                            "style": "STYLE_DEFAULT",
                            "size": "SIZE_DEFAULT",
                            "isDisabled": false,
                            "text": { "runs": [{ "text": "Go now" }] },
                            "command": {
                              "commandMetadata": { "webCommandMetadata": { "url": "/watch?v=OcULALBAXRA", "webPageType": "WEB_PAGE_TYPE_WATCH", "rootVe": 3832 } },
                              "watchEndpoint": { "videoId": "OcULALBAXRA" }
                            }
                          }
                        }
                      }
                    },
                    "actionId": "ChwKGkNKLW1yNjd4NkpNREZhRE5GZ2tkVUFNWUNn",
                    "bannerType": "LIVE_CHAT_BANNER_TYPE_CROSS_CHANNEL_REDIRECT",
                    "targetId": "live-chat-banner"
                  }
                }
              }
            }
            """;

    /// <summary>
    /// Cross-channel redirect banner with a "Learn more" button (no watchEndpoint/videoId).
    /// This variant appears when another channel's viewers raid into this stream.
    /// Real data from dump_banner_new.json entry 6 (@holoen_ceciliaimmergreen).
    /// Banner type: LIVE_CHAT_BANNER_TYPE_CROSS_CHANNEL_REDIRECT
    /// </summary>
    public static string AddBannerRedirectLearnMore() => """
            {
              "addBannerToLiveChatCommand": {
                "bannerRenderer": {
                  "liveChatBannerRenderer": {
                    "contents": {
                      "liveChatBannerRedirectRenderer": {
                        "bannerMessage": {
                          "runs": [
                            { "text": "@holoen_ceciliaimmergreen", "bold": true, "fontFace": "FONT_FACE_ROBOTO_REGULAR" },
                            { "text": " and their viewers just joined. Say hello!", "fontFace": "FONT_FACE_ROBOTO_REGULAR" }
                          ]
                        },
                        "authorPhoto": {
                          "thumbnails": [
                            { "url": "https://yt4.ggpht.com/sSuJylnDA4Si69bKWVzwUhrOhgIkBCzGE6DHgDyHCJux8TKi7WU8GyKaKZHEN0a3QG7s2yJ399g=s32-c-k-c0x00ffffff-no-rj", "width": 32, "height": 32 },
                            { "url": "https://yt4.ggpht.com/sSuJylnDA4Si69bKWVzwUhrOhgIkBCzGE6DHgDyHCJux8TKi7WU8GyKaKZHEN0a3QG7s2yJ399g=s64-c-k-c0x00ffffff-no-rj", "width": 64, "height": 64 }
                          ]
                        },
                        "inlineActionButton": {
                          "buttonRenderer": {
                            "style": "STYLE_DEFAULT",
                            "size": "SIZE_DEFAULT",
                            "isDisabled": false,
                            "text": { "runs": [{ "text": "Learn more" }] },
                            "command": {
                              "commandMetadata": {
                                "webCommandMetadata": {
                                  "url": "https://support.google.com/youtube/answer/10359590?hl=en",
                                  "webPageType": "WEB_PAGE_TYPE_UNKNOWN",
                                  "rootVe": 83769
                                }
                              },
                              "urlEndpoint": {
                                "url": "https://support.google.com/youtube/answer/10359590?hl=en",
                                "target": "TARGET_NEW_WINDOW"
                              }
                            }
                          }
                        }
                      }
                    },
                    "actionId": "ChwKGkNPNzM0NEdnNlpNREZUUFFsQWtkM25ZN3NR",
                    "targetId": "live-chat-banner",
                    "isStackable": true,
                    "bannerType": "LIVE_CHAT_BANNER_TYPE_CROSS_CHANNEL_REDIRECT"
                  }
                },
                "bannerProperties": {
                  "isEphemeral": true,
                  "bannerTimeoutMs": "20000"
                }
              }
            }
            """;

    /// <summary>
    /// AI-generated chat summary banner (LIVE_CHAT_BANNER_TYPE_CHAT_SUMMARY).
    /// Contains liveChatBannerChatSummaryRenderer with bold title, disclaimer, and body text.
    /// Real data from dump_poll_banners.json entry 0 (watch_20260415_060005.jsonl).
    /// </summary>
    public static string AddBannerChatSummary() => """
            {
              "clickTrackingParams": "CAEQl98BIhMI89Gw65bvkwMVrAE6Ah1euhdEygEExHmptQ==",
              "addBannerToLiveChatCommand": {
                "bannerRenderer": {
                  "liveChatBannerRenderer": {
                    "contents": {
                      "liveChatBannerChatSummaryRenderer": {
                        "liveChatSummaryId": "z4B7kTpSbWc_1776232797929176",
                        "chatSummary": {
                          "runs": [
                            { "text": "Chat summary", "bold": true, "fontFace": "FONT_FACE_ROBOTO_MEDIUM" },
                            { "text": "\n" },
                            { "text": "Auto-generated experiment \u2022 Quality may vary", "deemphasize": true, "fontFace": "FONT_FACE_ROBOTO_MEDIUM" },
                            { "text": "\n" },
                            { "text": "Viewers in the chat are saying happy birthday and discussing the construction of an open-air home theater setup. They are sharing suggestions on the design, layout, and functionality of the space.", "fontFace": "FONT_FACE_ROBOTO_REGULAR" }
                          ]
                        },
                        "icon": { "iconType": "SPARK" },
                        "trackingParams": "CB4Q77sMIhMI89Gw65bvkwMVrAE6Ah1euhdE",
                        "collapsedStateEntityKey": "Ehx6NEI3a1RwU2JXY18xNzc2MjMyNzk3OTI5MTc2IIsBKAE%3D"
                      }
                    },
                    "actionId": "z4B7kTpSbWc_1776232797929176",
                    "targetId": "live-chat-banner",
                    "isStackable": true,
                    "bannerProperties": {
                      "isEphemeral": true,
                      "autoCollapseDelay": { "seconds": "30" },
                      "bannerCollapsedStateEntityKey": "Ehx6NEI3a1RwU2JXY18xNzc2MjMyNzk3OTI5MTc2IIsBKAE%3D"
                    },
                    "bannerType": "LIVE_CHAT_BANNER_TYPE_CHAT_SUMMARY"
                  }
                },
                "bannerProperties": {
                  "isEphemeral": true,
                  "bannerTimeoutMs": "12000"
                }
              }
            }
            """;

    /// <summary>
    /// Pinned message banner from @InugamiKorone with OWNER + VERIFIED badges and Japanese text with 3 flushed-face emojis.
    /// Real data from dump_banners.json (entry 0).
    /// </summary>
    public static string AddBannerPinnedMessage_InugamiKorone() => """
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
                            { "text": "@InugamiKorone" }
                          ]
                        },
                        "contextMenuButton": {
                          "buttonRenderer": {
                            "icon": { "iconType": "MORE_VERT" },
                            "accessibility": { "label": "Chat actions" },
                            "accessibilityData": { "accessibilityData": { "label": "Chat actions" } },
                            "command": {
                              "commandMetadata": { "webCommandMetadata": { "ignoreNavigation": true } },
                              "liveChatItemContextMenuEndpoint": {
                                "params": "Q2g0S0hBb2FRMDFsV1hsbVRGZzNXazFFUmxOM1ZISlJXV1JOTXpoVlJtY2FLU29uQ2hoVlEyaEJibkZqWDBGWk5WOUpNMUI0TldScFp6TllNVkVTQzFORk5ESjZkSEZLZFdGRklBRW9CRElhQ2hoVlEyaEJibkZqWDBGWk5WOUpNMUI0TldScFp6TllNVkU0QVVnQVVBRSUzRA=="
                              }
                            }
                          }
                        }
                      }
                    },
                    "contents": {
                      "liveChatTextMessageRenderer": {
                        "message": {
                          "runs": [
                            { "text": "\u3044\u3063\u3071\u3044\u3044\u3063\u3071\u3044\u3042\u308A\u304C\u3068\u3046\uFF5E\uFF01\u3053\u308C\u304B\u3089\u3082\u3088\u308D\u3057\u304F\u306D\u3063" },
                            {
                              "emoji": {
                                "emojiId": "\uD83D\uDE33",
                                "shortcuts": [ ":flushed_face:", ":flushed:" ],
                                "searchTerms": [ "flushed", "face" ],
                                "image": {
                                  "thumbnails": [ { "url": "https://fonts.gstatic.com/s/e/notoemoji/15.1/1f633/72.png" } ],
                                  "accessibility": { "accessibilityData": { "label": "\uD83D\uDE33" } }
                                }
                              }
                            },
                            {
                              "emoji": {
                                "emojiId": "\uD83D\uDE33",
                                "shortcuts": [ ":flushed_face:", ":flushed:" ],
                                "searchTerms": [ "flushed", "face" ],
                                "image": {
                                  "thumbnails": [ { "url": "https://fonts.gstatic.com/s/e/notoemoji/15.1/1f633/72.png" } ],
                                  "accessibility": { "accessibilityData": { "label": "\uD83D\uDE33" } }
                                }
                              }
                            },
                            {
                              "emoji": {
                                "emojiId": "\uD83D\uDE33",
                                "shortcuts": [ ":flushed_face:", ":flushed:" ],
                                "searchTerms": [ "flushed", "face" ],
                                "image": {
                                  "thumbnails": [ { "url": "https://fonts.gstatic.com/s/e/notoemoji/15.1/1f633/72.png" } ],
                                  "accessibility": { "accessibilityData": { "label": "\uD83D\uDE33" } }
                                }
                              }
                            }
                          ]
                        },
                        "authorName": { "simpleText": "@InugamiKorone" },
                        "authorPhoto": {
                          "thumbnails": [
                            { "url": "https://yt4.ggpht.com/ytc/AIdro_nrS6tFctvjyWv1mKzKBIetHJBfpqwHOpvRFc3KU2P_5yc=s32-c-k-c0x00ffffff-no-rj", "width": 32, "height": 32 },
                            { "url": "https://yt4.ggpht.com/ytc/AIdro_nrS6tFctvjyWv1mKzKBIetHJBfpqwHOpvRFc3KU2P_5yc=s64-c-k-c0x00ffffff-no-rj", "width": 64, "height": 64 }
                          ]
                        },
                        "id": "ChwKGkNNZVl5ZkxYN1pNREZTd1RyUVlkTTM4VUZn",
                        "timestampUsec": "1776181549619677",
                        "authorBadges": [
                          {
                            "liveChatAuthorBadgeRenderer": {
                              "icon": { "iconType": "VERIFIED" },
                              "tooltip": "Verified",
                              "accessibility": { "accessibilityData": { "label": "Verified" } }
                            }
                          },
                          {
                            "liveChatAuthorBadgeRenderer": {
                              "icon": { "iconType": "OWNER" },
                              "tooltip": "Owner",
                              "accessibility": { "accessibilityData": { "label": "Owner" } }
                            }
                          }
                        ],
                        "authorExternalChannelId": "UChAnqc_AY5_I3Px5dig3X1Q"
                      }
                    },
                    "actionId": "ChwKGkNMcTd1dlRYN1pNREZXcmV3Z1FkRndzSXJ3",
                    "viewerIsCreator": false,
                    "targetId": "live-chat-banner",
                    "isStackable": false,
                    "backgroundType": "LIVE_CHAT_BANNER_BACKGROUND_TYPE_STATIC",
                    "bannerProperties": {
                      "autoCollapseDelay": { "seconds": "7" },
                      "bannerCollapsedStateEntityKey": "EihDaHdLR2tOTWNUZDFkbFJZTjFwTlJFWlhjbVYzWjFGa1JuZHpTWEozIIsBKAE%3D"
                    },
                    "bannerType": "LIVE_CHAT_BANNER_TYPE_PINNED_MESSAGE"
                  }
                }
              }
            }
            """;

    /// <summary>
    /// Cross-channel redirect banner from @KureijiOllie ("Learn more" / raid variant).
    /// Real data from dump_banners.json (entry 3).
    /// Banner type: LIVE_CHAT_BANNER_TYPE_CROSS_CHANNEL_REDIRECT
    /// isEphemeral: true, bannerTimeoutMs: "20000"
    /// </summary>
    public static string AddBannerRedirectLearnMore_KureijiOllie() => """
            {
              "addBannerToLiveChatCommand": {
                "bannerRenderer": {
                  "liveChatBannerRenderer": {
                    "contents": {
                      "liveChatBannerRedirectRenderer": {
                        "bannerMessage": {
                          "runs": [
                            { "text": "@KureijiOllie", "bold": true, "fontFace": "FONT_FACE_ROBOTO_REGULAR" },
                            { "text": " and their viewers just joined. Say hello!", "fontFace": "FONT_FACE_ROBOTO_REGULAR" }
                          ]
                        },
                        "authorPhoto": {
                          "thumbnails": [
                            { "url": "https://yt4.ggpht.com/fCQ1LUhWHfIGkCLeZl2BG_uQhQ6IqxJ3AJJxFbG6uEpLJ1hlJ2JOoBG7FJiAREeDeEVtwJoZKA=s32-c-k-c0x00ffffff-no-rj", "width": 32, "height": 32 },
                            { "url": "https://yt4.ggpht.com/fCQ1LUhWHfIGkCLeZl2BG_uQhQ6IqxJ3AJJxFbG6uEpLJ1hlJ2JOoBG7FJiAREeDeEVtwJoZKA=s64-c-k-c0x00ffffff-no-rj", "width": 64, "height": 64 }
                          ]
                        },
                        "inlineActionButton": {
                          "buttonRenderer": {
                            "style": "STYLE_DEFAULT",
                            "size": "SIZE_DEFAULT",
                            "isDisabled": false,
                            "text": { "runs": [{ "text": "Learn more" }] },
                            "command": {
                              "commandMetadata": {
                                "webCommandMetadata": {
                                  "url": "https://support.google.com/youtube/answer/10359590?hl=en",
                                  "webPageType": "WEB_PAGE_TYPE_UNKNOWN",
                                  "rootVe": 83769
                                }
                              },
                              "urlEndpoint": {
                                "url": "https://support.google.com/youtube/answer/10359590?hl=en",
                                "target": "TARGET_NEW_WINDOW"
                              }
                            }
                          }
                        },
                        "contextMenuButton": {
                          "buttonRenderer": {
                            "icon": { "iconType": "MORE_VERT" },
                            "accessibility": { "label": "Chat actions" },
                            "accessibilityData": { "accessibilityData": { "label": "Chat actions" } },
                            "command": {
                              "commandMetadata": { "webCommandMetadata": { "ignoreNavigation": true } },
                              "liveChatItemContextMenuEndpoint": {
                                "params": "Q2g0S0hBb2FRMDkyTTNSdldEUTNXazFFUm1aWVEyeEJhMlJvVlc5NVRVRWFLU29uQ2hoVlEwaHplRFJJY1dFdE1VOVNhbEZVYURsVVdVUm9kM2NTQ3pGWGJFTmlURzk2V0U1RklBRW9CRElFRWdJSUJqZ0JTQUJRSmclM0QlM0Q="
                              }
                            }
                          }
                        }
                      }
                    },
                    "actionId": "ChwKGkNPdjN0b1g0N1pNREZmWENsQWtkaFVveU1B",
                    "targetId": "live-chat-banner",
                    "isStackable": true,
                    "bannerType": "LIVE_CHAT_BANNER_TYPE_CROSS_CHANNEL_REDIRECT"
                  }
                },
                "bannerProperties": {
                  "isEphemeral": true,
                  "bannerTimeoutMs": "20000"
                }
              }
            }
            """;

    /// <summary>
    /// Cross-channel redirect banner from @usadapekora ("Go now" / stream-ending redirect variant).
    /// Real data from dump_banners_20260415.json.
    /// Banner type: LIVE_CHAT_BANNER_TYPE_CROSS_CHANNEL_REDIRECT
    /// actionId: ChwKGkNMdk96OG55NzVNREZYYkNsQWtkNFVVRndB
    /// </summary>
    public static string AddBannerRedirectGoNow_UsadaPekora() => """
            {
              "addBannerToLiveChatCommand": {
                "bannerRenderer": {
                  "liveChatBannerRenderer": {
                    "contents": {
                      "liveChatBannerRedirectRenderer": {
                        "bannerMessage": {
                          "runs": [
                            { "text": "Don't miss out! People are going to watch something from ", "fontFace": "FONT_FACE_ROBOTO_REGULAR" },
                            { "text": "@usadapekora", "bold": true, "fontFace": "FONT_FACE_ROBOTO_REGULAR" }
                          ]
                        },
                        "authorPhoto": {
                          "thumbnails": [
                            { "url": "https://yt4.ggpht.com/B-5Iau5CJVDiUOeCvCzHiwdkUijqoi2n0tNwfgIv_yDAvMbLHS4vq1IvK2RxL8y69BxTwmPhow=s32-c-k-c0x00ffffff-no-rj", "width": 32, "height": 32 },
                            { "url": "https://yt4.ggpht.com/B-5Iau5CJVDiUOeCvCzHiwdkUijqoi2n0tNwfgIv_yDAvMbLHS4vq1IvK2RxL8y69BxTwmPhow=s64-c-k-c0x00ffffff-no-rj", "width": 64, "height": 64 }
                          ]
                        },
                        "inlineActionButton": {
                          "buttonRenderer": {
                            "style": "STYLE_DEFAULT",
                            "size": "SIZE_DEFAULT",
                            "isDisabled": false,
                            "text": { "runs": [{ "text": "Go now" }] },
                            "command": {
                              "commandMetadata": {
                                "webCommandMetadata": {
                                  "url": "/watch?v=AFcfu7GuxVs",
                                  "webPageType": "WEB_PAGE_TYPE_WATCH",
                                  "rootVe": 3832
                                }
                              },
                              "watchEndpoint": {
                                "videoId": "AFcfu7GuxVs"
                              }
                            }
                          }
                        }
                      }
                    },
                    "actionId": "ChwKGkNMdk96OG55NzVNREZYYkNsQWtkNFVVRndB",
                    "targetId": "live-chat-banner",
                    "isStackable": true,
                    "bannerType": "LIVE_CHAT_BANNER_TYPE_CROSS_CHANNEL_REDIRECT"
                  }
                }
              }
            }
            """;

    /// <summary>
    /// Full addChatItemAction containing a liveChatTextMessageRenderer with a leaderboard rank badge
    /// (beforeContentButtons, iconName CROWN, title "#3").
    /// Real data from watch_20260411_045605.jsonl — @Sin_mikity, Member (2 years), rank #3.
    /// </summary>
    public static string TextMessageWithLeaderboardRank_SinMikity() => """
            {
              "clickTrackingParams": "CAEQl98BIhMIv6rmss3okwMVoxQ6Ah22hCJsygEEZbNZxg==",
              "addChatItemAction": {
                "item": {
                  "liveChatTextMessageRenderer": {
                    "message": {
                      "runs": [
                        {
                          "text": "孔明の罠"
                        }
                      ]
                    },
                    "authorName": {
                      "simpleText": "@Sin_mikity"
                    },
                    "authorPhoto": {
                      "thumbnails": [
                        {
                          "url": "https://yt4.ggpht.com/ytc/AIdro_nTIvXcfu5dzZ19et32rgwkkBvueVjeoW3TkRlCpnK1mo6JGfRO-K2-YkHSCDLndHsbOSzD=s32-c-k-c0x00ffffff-no-rj",
                          "width": 32,
                          "height": 32
                        },
                        {
                          "url": "https://yt4.ggpht.com/ytc/AIdro_nTIvXcfu5dzZ19et32rgwkkBvueVjeoW3TkRlCpnK1mo6JGfRO-K2-YkHSCDLndHsbOSzD=s64-c-k-c0x00ffffff-no-rj",
                          "width": 64,
                          "height": 64
                        }
                      ]
                    },
                    "contextMenuEndpoint": {
                      "clickTrackingParams": "CAEQl98BIhMIv6rmss3okwMVoxQ6Ah22hCJsygEEZbNZxg==",
                      "commandMetadata": {
                        "webCommandMetadata": {
                          "ignoreNavigation": true
                        }
                      },
                      "liveChatItemContextMenuEndpoint": {
                        "params": "Q2g0S0hBb2FRMDl5UTJsTVRFNDJTazFFUmxkWVFuZG5VV1EzVFZGd2RWRWFLU29uQ2hoVlF6QlVXR1ZmVEZsYU5ITmpZVmN5V0UxNWFUVmZhM2NTQzNaSGNHWnpiRU5VVkdaVklBRW9CRElhQ2hoVlEwNXpiMUpZUWxneFlWUlNVME40VW5sa1JsbzNibmM0QWtnQVVBRSUzRA=="
                      }
                    },
                    "id": "ChwKGkNPckNpTExONkpNREZXWEJ3Z1FkN01RcHVR",
                    "timestampUsec": "1776006931294583",
                    "authorBadges": [
                      {
                        "liveChatAuthorBadgeRenderer": {
                          "customThumbnail": {
                            "thumbnails": [
                              {
                                "url": "https://yt3.ggpht.com/rDBuKcXbuF7NW-l-OxhW1Pcx_iZeJtPx2ZCSd48vLzaHi9sATVlx-HRI35Ntz59ac6B-AiVciA=s16-c-k",
                                "width": 16,
                                "height": 16
                              },
                              {
                                "url": "https://yt3.ggpht.com/rDBuKcXbuF7NW-l-OxhW1Pcx_iZeJtPx2ZCSd48vLzaHi9sATVlx-HRI35Ntz59ac6B-AiVciA=s32-c-k",
                                "width": 32,
                                "height": 32
                              }
                            ]
                          },
                          "tooltip": "Member (2 years)",
                          "accessibility": {
                            "accessibilityData": {
                              "label": "Member (2 years)"
                            }
                          }
                        }
                      }
                    ],
                    "authorExternalChannelId": "UCNsoRXBX1aTRSCxRydFZ7nw",
                    "contextMenuAccessibility": {
                      "accessibilityData": {
                        "label": "Chat actions"
                      }
                    },
                    "trackingParams": "CAEQl98BIhMIv6rmss3okwMVoxQ6Ah22hCJs",
                    "beforeContentButtons": [
                      {
                        "buttonViewModel": {
                          "iconName": "CROWN",
                          "title": "#3",
                          "onTap": {
                            "innertubeCommand": {
                              "clickTrackingParams": "CB8Q8FsYayITCL-q5rLN6JMDFaMUOgIdtoQibMoBBGWzWcY=",
                              "showEngagementPanelEndpoint": {
                                "identifier": {
                                  "surface": "ENGAGEMENT_PANEL_SURFACE_LIVE_CHAT",
                                  "tag": "PAlive_viewer_leaderboard"
                                },
                                "globalConfiguration": {
                                  "params": "wgovGAAiKSonChhVQzBUWGVfTFlaNHNjYVcyWE15aTVfa3cSC3ZHcGZzbENUVGZVMAE%3D"
                                }
                              }
                            }
                          },
                          "accessibilityText": "#3",
                          "style": "BUTTON_VIEW_MODEL_STYLE_CUSTOM",
                          "trackingParams": "CB8Q8FsYayITCL-q5rLN6JMDFaMUOgIdtoQibA==",
                          "isFullWidth": true,
                          "type": "BUTTON_VIEW_MODEL_TYPE_FILLED",
                          "buttonSize": "BUTTON_VIEW_MODEL_SIZE_XSMALL",
                          "customBackgroundColor": 4293910271,
                          "customFontColor": 4278190080
                        }
                      }
                    ]
                  }
                },
                "clientId": "COrCiLLN6JMDFWXBwgQd7MQpuQ"
              }
            }
            """;

    public static string RemoveChatItem() => """
            {
              "removeChatItemAction": {
                "targetItemId": "REMOVED_MSG_ID_01"
              }
            }
            """;

    public static string ReportModerationStateEmpty() => """
            {
              "liveChatReportModerationStateCommand": {}
            }
            """;

    /// <summary>
    /// Fresh poll from <c>showLiveChatActionPanelAction</c>.
    /// Real polls have NO <c>voteRatio</c>/<c>votePercentage</c> on choices when first opened.
    /// Data matches the shape observed in live captures (dump_showpanel_new.json).
    /// </summary>
    public static string ShowPanelActionNewPoll() => """
            {
              "clickTrackingParams": "CAEQl98BIhMIkq6pmejokwMVHwE6Ah3w5hZNygEEpuqq1w==",
              "showLiveChatActionPanelAction": {
                "panelToShow": {
                  "liveChatActionPanelRenderer": {
                    "contents": {
                      "pollRenderer": {
                        "choices": [
                          {
                            "text": { "runs": [{ "text": "LET IN" }] },
                            "selected": false,
                            "signinEndpoint": {
                              "clickTrackingParams": "CAIQiK0HIhMIkq6pmejokwMVHwE6Ah3w5hZNygEEpuqq1w==",
                              "commandMetadata": {
                                "webCommandMetadata": { "webPageType": "WEB_PAGE_TYPE_UNKNOWN", "rootVe": 83769 }
                              },
                              "signInEndpoint": { "nextEndpoint": { "clickTrackingParams": "CAIQiK0HIhMIkq6pmejokwMVHwE6Ah3w5hZNygEEpuqq1w==" } }
                            }
                          },
                          {
                            "text": { "runs": [{ "text": "OUT" }] },
                            "selected": false,
                            "signinEndpoint": {
                              "clickTrackingParams": "CAIQiK0HIhMIkq6pmejokwMVHwE6Ah3w5hZNygEEpuqq1w==",
                              "commandMetadata": {
                                "webCommandMetadata": { "webPageType": "WEB_PAGE_TYPE_UNKNOWN", "rootVe": 83769 }
                              },
                              "signInEndpoint": { "nextEndpoint": { "clickTrackingParams": "CAIQiK0HIhMIkq6pmejokwMVHwE6Ah3w5hZNygEEpuqq1w==" } }
                            }
                          }
                        ],
                        "trackingParams": "CAIQiK0HIhMIkq6pmejokwMVHwE6Ah3w5hZN",
                        "liveChatPollId": "ChwKGkNNdVFxNWpvNkpNREZaekh3Z1FkelJZTExR",
                        "header": {
                          "pollHeaderRenderer": {
                            "pollQuestion": {},
                            "thumbnail": {
                              "thumbnails": [
                                { "url": "https://yt4.ggpht.com/HKYI1ENbRIVyDgLVtpxOKyLAOEdOHWH__-JQu6Kj2dq0S9U-wTccKoZT0-4DBd21O0Cpo6NnlA=s32-c-k-c0x00ffffff-no-rj", "width": 32, "height": 32 },
                                { "url": "https://yt4.ggpht.com/HKYI1ENbRIVyDgLVtpxOKyLAOEdOHWH__-JQu6Kj2dq0S9U-wTccKoZT0-4DBd21O0Cpo6NnlA=s64-c-k-c0x00ffffff-no-rj", "width": 64, "height": 64 }
                              ]
                            },
                            "metadataText": {
                              "runs": [
                                { "text": "@holoen_raorapanthera" },
                                { "text": " \u2022 " },
                                { "text": "just now" },
                                { "text": " \u2022 " },
                                { "text": "0 votes" }
                              ]
                            },
                            "liveChatPollType": "LIVE_CHAT_POLL_TYPE_CREATOR"
                          }
                        }
                      }
                    },
                    "id": "ChwKGkNNdVFxNWpvNkpNREZaekh3Z1FkelJZTExR",
                    "targetId": "live-chat-action-panel"
                  }
                }
              }
            }
            """;

    /// <summary>
    /// Poll open action from @OuroKronii: "for the wood" — "wall" vs "floor", 0 votes.
    /// Real data from dump_poll_show.json (watch_20260415_060005.jsonl).
    /// Poll ID: ChwKGkNKTzk4NUNiNzVNREZYWlFUQWdkT2U0VjVR
    /// </summary>
    public static string ShowPollAction_OuroKronii_WallVsFloor() => """
            {
              "clickTrackingParams": "CAEQl98BIhMIgdmNkpvvkwMV9thJBx2pkxN6ygEE75Nu3Q==",
              "showLiveChatActionPanelAction": {
                "panelToShow": {
                  "liveChatActionPanelRenderer": {
                    "contents": {
                      "pollRenderer": {
                        "choices": [
                          {
                            "text": { "runs": [{ "text": "wall" }] },
                            "selected": false,
                            "signinEndpoint": {
                              "clickTrackingParams": "CAIQiK0HIhMIgdmNkpvvkwMV9thJBx2pkxN6ygEE75Nu3Q==",
                              "commandMetadata": { "webCommandMetadata": { "url": "https://accounts.google.com/ServiceLogin?service=youtube\u0026uilel=3\u0026passive=true\u0026continue=https%3A%2F%2Fwww.youtube.com%2Fsignin%3Faction_handle_signin%3Dtrue%26app%3Ddesktop%26hl%3Den\u0026hl=en", "webPageType": "WEB_PAGE_TYPE_UNKNOWN", "rootVe": 83769 } },
                              "signInEndpoint": { "nextEndpoint": { "clickTrackingParams": "CAIQiK0HIhMIgdmNkpvvkwMV9thJBx2pkxN6ygEE75Nu3Q==" } }
                            }
                          },
                          {
                            "text": { "runs": [{ "text": "floor" }] },
                            "selected": false,
                            "signinEndpoint": {
                              "clickTrackingParams": "CAIQiK0HIhMIgdmNkpvvkwMV9thJBx2pkxN6ygEE75Nu3Q==",
                              "commandMetadata": { "webCommandMetadata": { "url": "https://accounts.google.com/ServiceLogin?service=youtube\u0026uilel=3\u0026passive=true\u0026continue=https%3A%2F%2Fwww.youtube.com%2Fsignin%3Faction_handle_signin%3Dtrue%26app%3Ddesktop%26hl%3Den\u0026hl=en", "webPageType": "WEB_PAGE_TYPE_UNKNOWN", "rootVe": 83769 } },
                              "signInEndpoint": { "nextEndpoint": { "clickTrackingParams": "CAIQiK0HIhMIgdmNkpvvkwMV9thJBx2pkxN6ygEE75Nu3Q==" } }
                            }
                          }
                        ],
                        "trackingParams": "CAIQiK0HIhMIgdmNkpvvkwMV9thJBx2pkxN6",
                        "liveChatPollId": "ChwKGkNKTzk4NUNiNzVNREZYWlFUQWdkT2U0VjVR",
                        "header": {
                          "pollHeaderRenderer": {
                            "pollQuestion": { "runs": [{ "text": "for the wood" }] },
                            "thumbnail": {
                              "thumbnails": [
                                { "url": "https://yt4.ggpht.com/XxF6c2VtpdbRdLcldz5jp05FQY_JTfOXeVd8osfAZsxODIanpt0ymcn_6nitwydHNGek46cfZ04=s32-c-k-c0x00ffffff-no-rj", "width": 32, "height": 32 },
                                { "url": "https://yt4.ggpht.com/XxF6c2VtpdbRdLcldz5jp05FQY_JTfOXeVd8osfAZsxODIanpt0ymcn_6nitwydHNGek46cfZ04=s64-c-k-c0x00ffffff-no-rj", "width": 64, "height": 64 }
                              ]
                            },
                            "metadataText": {
                              "runs": [
                                { "text": "@OuroKronii" },
                                { "text": " \u2022 " },
                                { "text": "just now" },
                                { "text": " \u2022 " },
                                { "text": "0 votes" }
                              ]
                            },
                            "liveChatPollType": "LIVE_CHAT_POLL_TYPE_CREATOR",
                            "contextMenuButton": {
                              "buttonRenderer": {
                                "icon": { "iconType": "MORE_VERT" },
                                "accessibility": { "label": "Chat actions" },
                                "trackingParams": "CAMQ8FsiEwiB2Y2Sm--TAxX22EkHHamTE3o=",
                                "accessibilityData": { "accessibilityData": { "label": "Chat actions" } },
                                "targetId": "live-chat-action-panel-poll-context-menu",
                                "command": {
                                  "clickTrackingParams": "CAMQ8FsiEwiB2Y2Sm--TAxX22EkHHamTE3rKAQTvk27d",
                                  "commandMetadata": { "webCommandMetadata": { "ignoreNavigation": true } },
                                  "liveChatItemContextMenuEndpoint": { "params": "Q2g0S0hBb2FRMHBQT1RnMVEySTNOVTFFUmxoYVVWUkJaMlJQWlRSV05WRWFLU29uQ2hoVlEyMWljemhVTmsxWGNWVklVREYwU1ZGMlUyZExjbWNTQzNvMFFqZHJWSEJUWWxkaklBRW9CRElhQ2hoVlEyMWljemhVTmsxWGNWVklVREYwU1ZGMlUyZExjbWM0QTBnQVVCVSUzRA==" }
                                }
                              }
                            }
                          }
                        }
                      }
                    },
                    "id": "ChwKGkNKTzk4NUNiNzVNREZYWlFUQWdkT2U0VjVR",
                    "targetId": "live-chat-action-panel"
                  }
                }
              }
            }
            """;

    /// <summary>
    /// First updateLiveChatPollAction for the @OuroKronii "for the wood" poll: both choices at 0 votes.
    /// Real data from dump_poll_updates.json entry 0 (watch_20260415_060005.jsonl).
    /// </summary>
    public static string UpdatePollAction_OuroKronii_ZeroVotes() => """
            {
              "updateLiveChatPollAction": {
                "pollToUpdate": {
                  "pollRenderer": {
                    "choices": [
                      {
                        "text": { "runs": [{ "text": "wall" }] },
                        "selected": false,
                        "voteRatio": 0,
                        "votePercentage": { "simpleText": "0%" },
                        "signinEndpoint": { "commandMetadata": { "webCommandMetadata": { "url": "https://accounts.google.com/ServiceLogin?service=youtube\u0026uilel=3\u0026passive=true\u0026continue=https%3A%2F%2Fwww.youtube.com%2Fsignin%3Faction_handle_signin%3Dtrue%26app%3Ddesktop%26hl%3Den\u0026hl=en", "webPageType": "WEB_PAGE_TYPE_UNKNOWN", "rootVe": 83769 } }, "signInEndpoint": { "nextEndpoint": {} } }
                      },
                      {
                        "text": { "runs": [{ "text": "floor" }] },
                        "selected": false,
                        "voteRatio": 0,
                        "votePercentage": { "simpleText": "0%" },
                        "signinEndpoint": { "commandMetadata": { "webCommandMetadata": { "url": "https://accounts.google.com/ServiceLogin?service=youtube\u0026uilel=3\u0026passive=true\u0026continue=https%3A%2F%2Fwww.youtube.com%2Fsignin%3Faction_handle_signin%3Dtrue%26app%3Ddesktop%26hl%3Den\u0026hl=en", "webPageType": "WEB_PAGE_TYPE_UNKNOWN", "rootVe": 83769 } }, "signInEndpoint": { "nextEndpoint": {} } }
                      }
                    ],
                    "liveChatPollId": "ChwKGkNKTzk4NUNiNzVNREZYWlFUQWdkT2U0VjVR",
                    "header": {
                      "pollHeaderRenderer": {
                        "pollQuestion": { "runs": [{ "text": "for the wood" }] },
                        "thumbnail": {
                          "thumbnails": [
                            { "url": "https://yt4.ggpht.com/XxF6c2VtpdbRdLcldz5jp05FQY_JTfOXeVd8osfAZsxODIanpt0ymcn_6nitwydHNGek46cfZ04=s32-c-k-c0x00ffffff-no-rj", "width": 32, "height": 32 },
                            { "url": "https://yt4.ggpht.com/XxF6c2VtpdbRdLcldz5jp05FQY_JTfOXeVd8osfAZsxODIanpt0ymcn_6nitwydHNGek46cfZ04=s64-c-k-c0x00ffffff-no-rj", "width": 64, "height": 64 }
                          ]
                        },
                        "metadataText": {
                          "runs": [
                            { "text": "@OuroKronii" },
                            { "text": " \u2022 " },
                            { "text": "just now" },
                            { "text": " \u2022 " },
                            { "text": "0 votes" }
                          ]
                        },
                        "liveChatPollType": "LIVE_CHAT_POLL_TYPE_CREATOR"
                      }
                    }
                  }
                }
              }
            }
            """;

    /// <summary>
    /// Mid-poll updateLiveChatPollAction for @OuroKronii: wall 45% vs floor 55%, 1301 votes, "10 min ago".
    /// Real data from dump_poll_updates.json ~entry 600 (watch_20260415_060005.jsonl).
    /// </summary>
    public static string UpdatePollAction_OuroKronii_MidPoll_Wall45_Floor55() => """
            {
              "updateLiveChatPollAction": {
                "pollToUpdate": {
                  "pollRenderer": {
                    "choices": [
                      {
                        "text": { "runs": [{ "text": "wall" }] },
                        "selected": false,
                        "voteRatio": 0.45475459098815918,
                        "votePercentage": { "simpleText": "45%" },
                        "signinEndpoint": { "commandMetadata": { "webCommandMetadata": { "url": "https://accounts.google.com/ServiceLogin?service=youtube\u0026uilel=3\u0026passive=true\u0026continue=https%3A%2F%2Fwww.youtube.com%2Fsignin%3Faction_handle_signin%3Dtrue%26app%3Ddesktop%26hl%3Den\u0026hl=en", "webPageType": "WEB_PAGE_TYPE_UNKNOWN", "rootVe": 83769 } }, "signInEndpoint": { "nextEndpoint": {} } }
                      },
                      {
                        "text": { "runs": [{ "text": "floor" }] },
                        "selected": false,
                        "voteRatio": 0.54524540901184082,
                        "votePercentage": { "simpleText": "55%" },
                        "signinEndpoint": { "commandMetadata": { "webCommandMetadata": { "url": "https://accounts.google.com/ServiceLogin?service=youtube\u0026uilel=3\u0026passive=true\u0026continue=https%3A%2F%2Fwww.youtube.com%2Fsignin%3Faction_handle_signin%3Dtrue%26app%3Ddesktop%26hl%3Den\u0026hl=en", "webPageType": "WEB_PAGE_TYPE_UNKNOWN", "rootVe": 83769 } }, "signInEndpoint": { "nextEndpoint": {} } }
                      }
                    ],
                    "liveChatPollId": "ChwKGkNKTzk4NUNiNzVNREZYWlFUQWdkT2U0VjVR",
                    "header": {
                      "pollHeaderRenderer": {
                        "pollQuestion": { "runs": [{ "text": "for the wood" }] },
                        "thumbnail": {
                          "thumbnails": [
                            { "url": "https://yt4.ggpht.com/XxF6c2VtpdbRdLcldz5jp05FQY_JTfOXeVd8osfAZsxODIanpt0ymcn_6nitwydHNGek46cfZ04=s32-c-k-c0x00ffffff-no-rj", "width": 32, "height": 32 },
                            { "url": "https://yt4.ggpht.com/XxF6c2VtpdbRdLcldz5jp05FQY_JTfOXeVd8osfAZsxODIanpt0ymcn_6nitwydHNGek46cfZ04=s64-c-k-c0x00ffffff-no-rj", "width": 64, "height": 64 }
                          ]
                        },
                        "metadataText": {
                          "runs": [
                            { "text": "@OuroKronii" },
                            { "text": " \u2022 " },
                            { "text": "10\u00A0min ago" },
                            { "text": " \u2022 " },
                            { "text": "1,301 votes" }
                          ]
                        },
                        "liveChatPollType": "LIVE_CHAT_POLL_TYPE_CREATOR"
                      }
                    }
                  }
                }
              }
            }
            """;

    /// <summary>
    /// Final updateLiveChatPollAction for @OuroKronii: wall 47% vs floor 53%, 1972 votes, "20 min ago".
    /// Real data from dump_poll_updates.json last entry (watch_20260415_060005.jsonl).
    /// </summary>
    public static string UpdatePollAction_OuroKronii_FinalResult_Wall47_Floor53() => """
            {
              "updateLiveChatPollAction": {
                "pollToUpdate": {
                  "pollRenderer": {
                    "choices": [
                      {
                        "text": { "runs": [{ "text": "wall" }] },
                        "selected": false,
                        "voteRatio": 0.46805274486541748,
                        "votePercentage": { "simpleText": "47%" },
                        "signinEndpoint": { "commandMetadata": { "webCommandMetadata": { "url": "https://accounts.google.com/ServiceLogin?service=youtube\u0026uilel=3\u0026passive=true\u0026continue=https%3A%2F%2Fwww.youtube.com%2Fsignin%3Faction_handle_signin%3Dtrue%26app%3Ddesktop%26hl%3Den\u0026hl=en", "webPageType": "WEB_PAGE_TYPE_UNKNOWN", "rootVe": 83769 } }, "signInEndpoint": { "nextEndpoint": {} } }
                      },
                      {
                        "text": { "runs": [{ "text": "floor" }] },
                        "selected": false,
                        "voteRatio": 0.53194725513458252,
                        "votePercentage": { "simpleText": "53%" },
                        "signinEndpoint": { "commandMetadata": { "webCommandMetadata": { "url": "https://accounts.google.com/ServiceLogin?service=youtube\u0026uilel=3\u0026passive=true\u0026continue=https%3A%2F%2Fwww.youtube.com%2Fsignin%3Faction_handle_signin%3Dtrue%26app%3Ddesktop%26hl%3Den\u0026hl=en", "webPageType": "WEB_PAGE_TYPE_UNKNOWN", "rootVe": 83769 } }, "signInEndpoint": { "nextEndpoint": {} } }
                      }
                    ],
                    "liveChatPollId": "ChwKGkNKTzk4NUNiNzVNREZYWlFUQWdkT2U0VjVR",
                    "header": {
                      "pollHeaderRenderer": {
                        "pollQuestion": { "runs": [{ "text": "for the wood" }] },
                        "thumbnail": {
                          "thumbnails": [
                            { "url": "https://yt4.ggpht.com/XxF6c2VtpdbRdLcldz5jp05FQY_JTfOXeVd8osfAZsxODIanpt0ymcn_6nitwydHNGek46cfZ04=s32-c-k-c0x00ffffff-no-rj", "width": 32, "height": 32 },
                            { "url": "https://yt4.ggpht.com/XxF6c2VtpdbRdLcldz5jp05FQY_JTfOXeVd8osfAZsxODIanpt0ymcn_6nitwydHNGek46cfZ04=s64-c-k-c0x00ffffff-no-rj", "width": 64, "height": 64 }
                          ]
                        },
                        "metadataText": {
                          "runs": [
                            { "text": "@OuroKronii" },
                            { "text": " \u2022 " },
                            { "text": "20\u00A0min ago" },
                            { "text": " \u2022 " },
                            { "text": "1,972 votes" }
                          ]
                        },
                        "liveChatPollType": "LIVE_CHAT_POLL_TYPE_CREATOR"
                      }
                    }
                  }
                }
              }
            }
            """;

    /// <summary>
    /// closeLiveChatActionPanelAction for the @OuroKronii "for the wood" poll.
    /// Includes skipOnDismissCommand: true, observed when closing a creator poll panel.
    /// Real data from dump_poll_close.json (watch_20260415_060005.jsonl).
    /// </summary>
    public static string ClosePollPanel_OuroKronii() => """
            {
              "closeLiveChatActionPanelAction": {
                "targetPanelId": "ChwKGkNKTzk4NUNiNzVNREZYWlFUQWdkT2U0VjVR",
                "skipOnDismissCommand": true
              }
            }
            """;

    /// <summary>
    /// Poll result summary as a liveChatViewerEngagementMessageRenderer (POLL icon, bold question text).
    /// "for the wood" — floor 53%, wall 47% — Poll complete: 1.9K votes.
    /// Real data from dump_poll_engagement.json (watch_20260415_060005.jsonl).
    /// </summary>
    public static string ViewerEngagementPollResult_OuroKronii_WallVsFloor() => """
            {
              "addChatItemAction": {
                "item": {
                  "liveChatViewerEngagementMessageRenderer": {
                    "id": "ChwKGkNKelhuTldmNzVNREZaZHhUQWdkY0JZeDZR",
                    "icon": {
                      "iconType": "POLL"
                    },
                    "message": {
                      "runs": [
                        { "text": "for the wood", "bold": true },
                        { "text": "\n" },
                        { "text": "floor (53%)" },
                        { "text": "\n" },
                        { "text": "wall (47%)" },
                        { "text": "\n" },
                        { "text": "\n" },
                        { "text": "Poll complete: 1.9K votes" }
                      ]
                    },
                    "contextMenuEndpoint": {
                      "commandMetadata": { "webCommandMetadata": { "ignoreNavigation": true } },
                      "liveChatItemContextMenuEndpoint": { "params": "Q2g0S0hBb2FRMHA2V0c1T1YyWTNOVTFFUmxwa2VGUkJaMlJqUWxsNE5sRWFLU29uQ2hoVlEyMWljemhVTmsxWGNWVklVREYwU1ZGMlUyZExjbWNTQzNvMFFqZHJWSEJUWWxkaklBRW9CRElhQ2hoVlEyMWljemhVTmsxWGNWVklVREYwU1ZGMlUyZExjbWM0QWtnQVVCWSUzRA==" }
                    },
                    "contextMenuAccessibility": {
                      "accessibilityData": { "label": "Chat actions" }
                    }
                  }
                }
              }
            }
            """;

    public static string RemoveChatItemByAuthor() => """
            {
              "removeChatItemByAuthorAction": {
                "externalChannelId": "UC_BANNED_CHANNEL_01"
              }
            }
            """;

    public static string RemoveBanner() => """
            {
              "removeBannerForLiveChatCommand": {
                "targetActionId": "PINNED_ACTION_ID_01"
              }
            }
            """;

    public static string CloseLiveChatActionPanel() => """
            {
              "closeLiveChatActionPanelAction": {
                "targetPanelId": "POLL_ID_SHOW_01"
              }
            }
            """;

    public static string ModeChangeMessageRenderer()
    {
        long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000L;
        return $$"""
            {
              "addChatItemAction": {
                "item": {
                  "liveChatModeChangeMessageRenderer": {
                    "id": "MODE_CHANGE_TEST_01",
                    "timestampUsec": "{{ts}}",
                    "icon": { "iconType": "QUESTION_ANSWER" },
                    "text": {
                      "runs": [
                        { "text": "@Host", "bold": true },
                        { "text": " turned off subscribers-only mode", "bold": true }
                      ]
                    },
                    "subtext": {
                      "runs": [
                        { "text": "Anyone can send a message", "italics": true }
                      ]
                    }
                  }
                },
                "clientId": "TEST_CLIENT_ID_MODE_01"
              }
            }
            """;
    }

    public static string PlaceholderItemRenderer()
    {
        long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000L;
        return $$"""
            {
              "addChatItemAction": {
                "item": {
                  "liveChatPlaceholderItemRenderer": {
                    "id": "PLACEHOLDER_TEST_01",
                    "timestampUsec": "{{ts}}"
                  }
                },
                "clientId": "TEST_CLIENT_ID_PLACEHOLDER_01"
              }
            }
            """;
    }

    /// <summary>
    /// replaceChatItemAction with a full liveChatTextMessageRenderer replacement.
    /// Data from real live capture (dump_replace_membership.json, first entry).
    /// </summary>
    public static string ReplaceChatItemWithText() => """
            {
              "replaceChatItemAction": {
                "targetItemId": "ChwKGkNQcTJ5X0t3NlpNREZlZkJ3Z1FkUTI4RmJR",
                "replacementItem": {
                  "liveChatTextMessageRenderer": {
                    "message": {
                      "runs": [
                        { "text": "pagi bokobo" }
                      ]
                    },
                    "authorName": { "simpleText": "@asepjulian896" },
                    "authorPhoto": {
                      "thumbnails": [
                        { "url": "https://yt4.ggpht.com/ytc/AIdro_nVlngOB3p8jHEgg5A4A1VRs3m2pHGn2mrA5O1J3ck=s32-c-k-c0x00ffffff-no-rj", "width": 32, "height": 32 },
                        { "url": "https://yt4.ggpht.com/ytc/AIdro_nVlngOB3p8jHEgg5A4A1VRs3m2pHGn2mrA5O1J3ck=s64-c-k-c0x00ffffff-no-rj", "width": 64, "height": 64 }
                      ]
                    },
                    "id": "ChwKGkNQcTJ5X0t3NlpNREZlZkJ3Z1FkUTI4RmJR",
                    "timestampUsec": "1776033641718643",
                    "authorExternalChannelId": "UCFIehAvmitLzMf3KDWlW-sA"
                  }
                }
              }
            }
            """;

    /// <summary>
    /// replaceChatItemAction where the replacement is a liveChatPlaceholderItemRenderer.
    /// Replacement should produce a null ChatItem (placeholder maps to no output).
    /// </summary>
    public static string ReplaceChatItemWithPlaceholder()
    {
        long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000L;
        return $$"""
            {
              "replaceChatItemAction": {
                "targetItemId": "REPLACE_TARGET_PLACEHOLDER_01",
                "replacementItem": {
                  "liveChatPlaceholderItemRenderer": {
                    "id": "REPLACE_PLACEHOLDER_01",
                    "timestampUsec": "{{ts}}"
                  }
                }
              }
            }
            """;
    }

    /// <summary>
    /// updateLiveChatPollAction with 0% votes (poll just opened, first update).
    /// Data from real live capture (dump_updatepoll_new.json, first entry).
    /// </summary>
    public static string UpdatePollActionZeroVotes() => """
            {
              "updateLiveChatPollAction": {
                "pollToUpdate": {
                  "pollRenderer": {
                    "choices": [
                      {
                        "text": { "runs": [{ "text": "LET IN" }] },
                        "selected": false,
                        "voteRatio": 0,
                        "votePercentage": { "simpleText": "0%" },
                        "signinEndpoint": { "commandMetadata": { "webCommandMetadata": { "webPageType": "WEB_PAGE_TYPE_UNKNOWN", "rootVe": 83769 } }, "signInEndpoint": { "nextEndpoint": {} } }
                      },
                      {
                        "text": { "runs": [{ "text": "OUT" }] },
                        "selected": false,
                        "voteRatio": 0,
                        "votePercentage": { "simpleText": "0%" },
                        "signinEndpoint": { "commandMetadata": { "webCommandMetadata": { "webPageType": "WEB_PAGE_TYPE_UNKNOWN", "rootVe": 83769 } }, "signInEndpoint": { "nextEndpoint": {} } }
                      }
                    ],
                    "liveChatPollId": "ChwKGkNNdVFxNWpvNkpNREZaekh3Z1FkelJZTExR",
                    "header": {
                      "pollHeaderRenderer": {
                        "pollQuestion": {},
                        "thumbnail": {
                          "thumbnails": [
                            { "url": "https://yt4.ggpht.com/HKYI1ENbRIVyDgLVtpxOKyLAOEdOHWH__-JQu6Kj2dq0S9U-wTccKoZT0-4DBd21O0Cpo6NnlA=s32-c-k-c0x00ffffff-no-rj", "width": 32, "height": 32 },
                            { "url": "https://yt4.ggpht.com/HKYI1ENbRIVyDgLVtpxOKyLAOEdOHWH__-JQu6Kj2dq0S9U-wTccKoZT0-4DBd21O0Cpo6NnlA=s64-c-k-c0x00ffffff-no-rj", "width": 64, "height": 64 }
                          ]
                        },
                        "metadataText": {
                          "runs": [
                            { "text": "@holoen_raorapanthera" },
                            { "text": " \u2022 " },
                            { "text": "just now" },
                            { "text": " \u2022 " },
                            { "text": "0 votes" }
                          ]
                        },
                        "liveChatPollType": "LIVE_CHAT_POLL_TYPE_CREATOR"
                      }
                    }
                  }
                }
              }
            }
            """;

    /// <summary>
    /// giftMessageViewModel with authorAvatar (avatarViewModel.image.sources) and giftImage (sources).
    /// Real data from watch_20260422_203201.jsonl — @franciscosaranteheredia1890 sent Sparkles.
    /// Verifies that multi-source ToImage uses the last (highest-resolution) source.
    /// </summary>
    public static string GiftMessageViewModelWithAvatarAndGiftImage() => """
            {
              "clickTrackingParams": "CAEQl98BIhMI--y79qaClAMVqot8Bh3mCRhiygEEw1TgWw==",
              "addChatItemAction": {
                "item": {
                  "giftMessageViewModel": {
                    "text": {
                      "content": "sent Sparkles",
                      "styleRuns": [
                        {
                          "startIndex": 0,
                          "length": 13
                        }
                      ]
                    },
                    "authorName": {
                      "content": "@franciscosaranteheredia1890 ",
                      "styleRuns": [
                        {
                          "startIndex": 0,
                          "length": 29
                        }
                      ]
                    },
                    "id": "ChwKGkNLSEt5ZldtZ3BRREZVc0kxZ0FkTmRVNTBn",
                    "authorAvatar": {
                      "avatarViewModel": {
                        "image": {
                          "sources": [
                            {
                              "url": "https://yt4.ggpht.com/ytc/AIdro_kxKFy47u3Kv9yH8eQIPFcxR3iD4lub6s2Fxcsch3_Uy54=s32-c-k-c0x00ffffff-no-rj",
                              "width": 32,
                              "height": 32
                            },
                            {
                              "url": "https://yt4.ggpht.com/ytc/AIdro_kxKFy47u3Kv9yH8eQIPFcxR3iD4lub6s2Fxcsch3_Uy54=s64-c-k-c0x00ffffff-no-rj",
                              "width": 64,
                              "height": 64
                            }
                          ],
                          "processor": {
                            "borderImageProcessor": {
                              "circular": true
                            }
                          }
                        },
                        "avatarImageSize": "AVATAR_SIZE_XS"
                      }
                    },
                    "giftImage": {
                      "sources": [
                        {
                          "url": "//www.gstatic.com/youtube/img/pdg/gift/assets/sparkles_v2_320x320.png=w480-h480",
                          "width": 480,
                          "height": 480
                        },
                        {
                          "url": "//www.gstatic.com/youtube/img/pdg/gift/assets/sparkles_v2_320x320.png=w640-h640",
                          "width": 640,
                          "height": 640
                        }
                      ]
                    },
                    "giftImageA11yLabel": "@franciscosaranteheredia1890 sent a gift, Sparkles",
                    "rendererContext": {
                      "loggingContext": {
                        "loggingDirectives": {
                          "trackingParams": "CAIQ9p4PIhMI--y79qaClAMVqot8Bh3mCRhi",
                          "visibility": {
                            "types": "12"
                          }
                        }
                      }
                    }
                  }
                },
                "clientId": "CKHKyfWmgpQDFUsI1gAdNdU50g"
              }
            }
            """;

    public static string GiftMessageViewModelAction() => """
            {
              "addChatItemAction": {
                "item": {
                  "giftMessageViewModel": {
                    "text": {
                      "content": "sent Gold coin for 10 Jewels",
                      "styleRuns": [{ "startIndex": 0, "length": 28 }]
                    },
                    "authorName": {
                      "content": "@yaniescobar2170 ",
                      "styleRuns": [{ "startIndex": 0, "length": 17 }]
                    },
                    "image": {
                      "sources": [
                        {
                          "clientResource": {
                            "imageName": "GIFT",
                            "imageColor": 4294901760
                          }
                        }
                      ]
                    },
                    "id": "ChwKGkNPdXhoY09XX3BNREZWZ0kxZ0FkY0tzNlRR",
                    "imageA11yLabel": "Gifts",
                    "rendererContext": {
                      "loggingContext": {
                        "loggingDirectives": {
                          "trackingParams": "CLABEPaeDyITCNOI4ZSw_pMDFeFUQQIdNnQiFA==",
                          "visibility": { "types": "12" }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

    /// <summary>
    /// Creator Goal ticker chip action — minimal variant (no tracking params).
    /// Real data from watch_20260428_192449.jsonl, sample 1.
    /// Id: "ChwKGkNMYkttOWF4a3BRREZaMjRyZ1VkeXc4a2V3"
    /// EntityKey: "EgtPQXFoN0tWLXIzSSD6AygB"
    /// </summary>
    public static string CreatorGoalTickerChip() => """
            {
              "showCreatorGoalTickerChipCommand": {
                "creatorGoalTickerChip": {
                  "liveChatTickerCreatorGoalViewModel": {
                    "id": "ChwKGkNMYkttOWF4a3BRREZaMjRyZ1VkeXc4a2V3",
                    "initialTickerText": {
                      "content": "Goal",
                      "styleRuns": [ { "startIndex": 0, "length": 4 } ]
                    },
                    "tickerIcon": {
                      "sources": [ { "clientResource": { "imageName": "TARGET_ADD" } } ]
                    },
                    "creatorGoalEntityKey": "EgtPQXFoN0tWLXIzSSD6AygB",
                    "shouldShowCountIncrementAnimation": true,
                    "a11yLabel": "See Super Chat goal",
                    "onClickCommand": {
                      "innertubeCommand": {
                        "showEngagementPanelEndpoint": {
                          "engagementPanel": {
                            "engagementPanelSectionListRenderer": {
                              "header": {
                                "engagementPanelTitleHeaderRenderer": {
                                  "actionButton": {
                                    "buttonRenderer": {
                                      "icon": { "iconType": "QUESTION_CIRCLE" },
                                      "command": {
                                        "commandExecutorCommand": {
                                          "commands": [
                                            {
                                              "liveChatDialogEndpoint": {
                                                "content": {
                                                  "liveChatDialogRenderer": {
                                                    "title": {
                                                      "runs": [ { "text": "Super Chat Goal" } ]
                                                    },
                                                    "confirmButton": {
                                                      "buttonRenderer": {
                                                        "style": "STYLE_MONO_FILLED",
                                                        "size": "SIZE_DEFAULT",
                                                        "isDisabled": false,
                                                        "text": { "simpleText": "Got it" }
                                                      }
                                                    }
                                                  }
                                                }
                                              }
                                            },
                                            {
                                              "hideEngagementPanelEndpoint": {
                                                "identifier": {
                                                  "surface": "ENGAGEMENT_PANEL_SURFACE_LIVE_CHAT",
                                                  "tag": "creator_goal_progress_engagement_panel"
                                                }
                                              }
                                            }
                                          ]
                                        }
                                      }
                                    }
                                  }
                                }
                              },
                              "content": {
                                "sectionListRenderer": {
                                  "contents": [
                                    {
                                      "creatorGoalProgressFlowViewModel": {
                                        "creatorGoalEntityKey": "EgtPQXFoN0tWLXIzSSD6AygB",
                                        "progressFlowButton": {
                                          "buttonViewModel": {
                                            "onTap": {
                                              "innertubeCommand": {
                                                "commandMetadata": { "webCommandMetadata": { "ignoreNavigation": true } },
                                                "liveChatPurchaseMessageEndpoint": {
                                                  "params": "Q2lrcUp3b1lWVU4yUkhaQ1FVOTFTa2xJUzFkTE5UbGtkWFpOTUVwUkVndFBRWEZvTjB0V0xYSXpTUkFCSUFFNEFFSUNDQUUlM0Q="
                                                }
                                              }
                                            },
                                            "style": "BUTTON_VIEW_MODEL_STYLE_MONO",
                                            "type": "BUTTON_VIEW_MODEL_TYPE_FILLED",
                                            "titleFormatted": {
                                              "content": "Continue",
                                              "styleRuns": [ { "startIndex": 0, "length": 8 } ]
                                            }
                                          }
                                        },
                                        "progressCountA11yLabel": "Super Chat goal progress: $0 out of $1"
                                      }
                                    }
                                  ]
                                }
                              },
                              "identifier": {
                                "surface": "ENGAGEMENT_PANEL_SURFACE_LIVE_CHAT",
                                "tag": "creator_goal_progress_engagement_panel"
                              }
                            }
                          },
                          "identifier": {
                            "surface": "ENGAGEMENT_PANEL_SURFACE_LIVE_CHAT",
                            "tag": "creator_goal_progress_engagement_panel"
                          },
                          "engagementPanelPresentationConfigs": {
                            "engagementPanelPopupPresentationConfig": {
                              "popupType": "PANEL_POPUP_TYPE_DIALOG"
                            }
                          }
                        }
                      }
                    },
                    "loggingDirectives": {
                      "visibility": { "types": "12" }
                    }
                  }
                }
              }
            }
            """;

    /// <summary>
    /// Creator Goal ticker chip action — complex variant (with clickTrackingParams at every level).
    /// Real data from watch_20260428_192449.jsonl, sample 4.
    /// Id: "ChwKGkNLZTRpdXJVazVRREZSNjRyZ1VkXzVrM0Rn"
    /// EntityKey: "EgtPQXFoN0tWLXIzSSD6AygB"
    /// </summary>
    public static string CreatorGoalTickerChipWithTrackingParams() => """
            {
              "clickTrackingParams": "CAIQl98BIhMIoKmQ0NiTlAMVfcNJBx0XCjywygEEqLJbrg==",
              "showCreatorGoalTickerChipCommand": {
                "creatorGoalTickerChip": {
                  "liveChatTickerCreatorGoalViewModel": {
                    "id": "ChwKGkNLZTRpdXJVazVRREZSNjRyZ1VkXzVrM0Rn",
                    "initialTickerText": {
                      "content": "Goal",
                      "styleRuns": [ { "startIndex": 0, "length": 4 } ]
                    },
                    "tickerIcon": {
                      "sources": [ { "clientResource": { "imageName": "TARGET_ADD" } } ]
                    },
                    "creatorGoalEntityKey": "EgtPQXFoN0tWLXIzSSD6AygB",
                    "shouldShowCountIncrementAnimation": true,
                    "a11yLabel": "See Super Chat goal",
                    "onClickCommand": {
                      "innertubeCommand": {
                        "clickTrackingParams": "CAwQ7NANIhMIoKmQ0NiTlAMVfcNJBx0XCjywygEEqLJbrg==",
                        "showEngagementPanelEndpoint": {
                          "engagementPanel": {
                            "engagementPanelSectionListRenderer": {
                              "header": {
                                "engagementPanelTitleHeaderRenderer": {
                                  "actionButton": {
                                    "buttonRenderer": {
                                      "icon": { "iconType": "QUESTION_CIRCLE" },
                                      "trackingParams": "CBAQ8FsiEwigqZDQ2JOUAxV9w0kHHRcKPLA=",
                                      "command": {
                                        "clickTrackingParams": "CBAQ8FsiEwigqZDQ2JOUAxV9w0kHHRcKPLDKAQSosluu",
                                        "commandExecutorCommand": {
                                          "commands": [
                                            {
                                              "clickTrackingParams": "CBAQ8FsiEwigqZDQ2JOUAxV9w0kHHRcKPLDKAQSosluu",
                                              "liveChatDialogEndpoint": {
                                                "content": {
                                                  "liveChatDialogRenderer": {
                                                    "trackingParams": "CBEQzS8iEwigqZDQ2JOUAxV9w0kHHRcKPLA=",
                                                    "title": {
                                                      "runs": [ { "text": "Super Chat Goal" } ]
                                                    },
                                                    "dialogMessages": [
                                                      {
                                                        "runs": [
                                                          { "text": "Join the fun by participating in the goal! " },
                                                          {
                                                            "text": "Learn more\n",
                                                            "navigationEndpoint": {
                                                              "clickTrackingParams": "CBEQzS8iEwigqZDQ2JOUAxV9w0kHHRcKPLDKAQSosluu",
                                                              "commandMetadata": {
                                                                "webCommandMetadata": {
                                                                  "url": "https://support.google.com/youtube/answer/16475524",
                                                                  "webPageType": "WEB_PAGE_TYPE_UNKNOWN",
                                                                  "rootVe": 83769
                                                                }
                                                              },
                                                              "urlEndpoint": {
                                                                "url": "https://support.google.com/youtube/answer/16475524",
                                                                "target": "TARGET_NEW_WINDOW"
                                                              }
                                                            }
                                                          }
                                                        ]
                                                      },
                                                      {
                                                        "runs": [
                                                          { "text": "How to participate", "bold": true, "textColor": 4279440147 },
                                                          { "text": "\n" },
                                                          { "text": "1. Press \"Continue\"\n2. Purchase a Super Chat \n3. Watch the progress towards the goal\n4. Celebrate achieving it with the community!", "textColor": 4279440147 }
                                                        ]
                                                      }
                                                    ],
                                                    "confirmButton": {
                                                      "buttonRenderer": {
                                                        "style": "STYLE_MONO_FILLED",
                                                        "size": "SIZE_DEFAULT",
                                                        "isDisabled": false,
                                                        "text": { "simpleText": "Got it" },
                                                        "trackingParams": "CBIQ8FsiEwigqZDQ2JOUAxV9w0kHHRcKPLA=",
                                                        "accessibilityData": {
                                                          "accessibilityData": { "label": "Got it" }
                                                        }
                                                      }
                                                    }
                                                  }
                                                }
                                              }
                                            },
                                            {
                                              "clickTrackingParams": "CBAQ8FsiEwigqZDQ2JOUAxV9w0kHHRcKPLDKAQSosluu",
                                              "hideEngagementPanelEndpoint": {
                                                "identifier": {
                                                  "surface": "ENGAGEMENT_PANEL_SURFACE_LIVE_CHAT",
                                                  "tag": "creator_goal_progress_engagement_panel"
                                                }
                                              }
                                            }
                                          ]
                                        }
                                      }
                                    }
                                  },
                                  "trackingParams": "CA0Q040EIhMIoKmQ0NiTlAMVfcNJBx0XCjyw"
                                }
                              },
                              "content": {
                                "sectionListRenderer": {
                                  "contents": [
                                    {
                                      "creatorGoalProgressFlowViewModel": {
                                        "creatorGoalEntityKey": "EgtPQXFoN0tWLXIzSSD6AygB",
                                        "progressFlowButton": {
                                          "buttonViewModel": {
                                            "onTap": {
                                              "innertubeCommand": {
                                                "clickTrackingParams": "CA8Q8FsiEwigqZDQ2JOUAxV9w0kHHRcKPLDKAQSosluu",
                                                "commandMetadata": { "webCommandMetadata": { "ignoreNavigation": true } },
                                                "liveChatPurchaseMessageEndpoint": {
                                                  "params": "Q2lrcUp3b1lWVU4yUkhaQ1FVOTFTa2xJUzFkTE5UbGtkWFpOTUVwUkVndFBRWEZvTjB0V0xYSXpTUkFCSUFFNEFFSUNDQUUlM0Q="
                                                }
                                              }
                                            },
                                            "style": "BUTTON_VIEW_MODEL_STYLE_MONO",
                                            "trackingParams": "CA8Q8FsiEwigqZDQ2JOUAxV9w0kHHRcKPLA=",
                                            "type": "BUTTON_VIEW_MODEL_TYPE_FILLED",
                                            "titleFormatted": {
                                              "content": "Continue",
                                              "styleRuns": [ { "startIndex": 0, "length": 8 } ]
                                            }
                                          }
                                        },
                                        "progressCountA11yLabel": "Super Chat goal progress: $0 out of $1"
                                      }
                                    }
                                  ],
                                  "trackingParams": "CA4Qui8iEwigqZDQ2JOUAxV9w0kHHRcKPLA="
                                }
                              },
                              "identifier": {
                                "surface": "ENGAGEMENT_PANEL_SURFACE_LIVE_CHAT",
                                "tag": "creator_goal_progress_engagement_panel"
                              }
                            }
                          },
                          "identifier": {
                            "surface": "ENGAGEMENT_PANEL_SURFACE_LIVE_CHAT",
                            "tag": "creator_goal_progress_engagement_panel"
                          },
                          "engagementPanelPresentationConfigs": {
                            "engagementPanelPopupPresentationConfig": {
                              "popupType": "PANEL_POPUP_TYPE_DIALOG"
                            }
                          }
                        }
                      }
                    },
                    "loggingDirectives": {
                      "trackingParams": "CAwQ7NANIhMIoKmQ0NiTlAMVfcNJBx0XCjyw",
                      "visibility": { "types": "12" }
                    }
                  }
                }
              }
            }
            """;
}
