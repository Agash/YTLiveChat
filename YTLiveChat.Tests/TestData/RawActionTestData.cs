namespace YTLiveChat.Tests.TestData;

internal static class RawActionTestData
{
    public static string TickerPaidMessageFromLog8() =>
        """
        {
          "clickTrackingParams": "CAEQl98BIhMIqZya5-fdkgMVettJBx0_CBUvygEE-ogb2A==",
          "addLiveChatTickerItemAction": {
            "item": {
              "liveChatTickerPaidMessageItemRenderer": {
                "id": "ChwKGkNMLWt4ZFRuM1pJREZiN0t3Z1Fkc3N3VFlR",
                "amountTextColor": 4278190080,
                "durationSec": 111,
                "authorPhoto": {
                  "thumbnails": [
                    {
                      "url": "https://yt4.ggpht.com/LKN2ze9hioSfGlKW4BfKGQzBZPO0G3fqkByS4jw9hOtYguPZMut0uIzNPYIw8075rynxq1TS=s64-c-k-c0x00ffffff-no-rj",
                      "width": 64,
                      "height": 64
                    }
                  ],
                  "accessibility": {
                    "accessibilityData": {
                      "label": "@すずむら337"
                    }
                  }
                },
                "showItemEndpoint": {
                  "showLiveChatItemEndpoint": {
                    "renderer": {
                      "liveChatPaidMessageRenderer": {
                        "id": "ChwKGkNMLWt4ZFRuM1pJREZiN0t3Z1Fkc3N3VFlR",
                        "timestampUsec": "1771238008473570",
                        "authorName": {
                          "simpleText": "@すずむら337"
                        },
                        "authorPhoto": {
                          "thumbnails": [
                            {
                              "url": "https://yt4.ggpht.com/LKN2ze9hioSfGlKW4BfKGQzBZPO0G3fqkByS4jw9hOtYguPZMut0uIzNPYIw8075rynxq1TS=s64-c-k-c0x00ffffff-no-rj",
                              "width": 64,
                              "height": 64
                            }
                          ]
                        },
                        "purchaseAmountText": {
                          "simpleText": "¥500"
                        },
                        "message": {
                          "runs": [
                            {
                              "text": "超キュート……"
                            },
                            {
                              "emoji": {
                                "emojiId": "🫶",
                                "shortcuts": [
                                  ":heart_hands:"
                                ],
                                "searchTerms": [
                                  "heart",
                                  "hands"
                                ],
                                "image": {
                                  "thumbnails": [
                                    {
                                      "url": "https://fonts.gstatic.com/s/e/notoemoji/15.1/1faf6/72.png"
                                    }
                                  ],
                                  "accessibility": {
                                    "accessibilityData": {
                                      "label": "🫶"
                                    }
                                  }
                                }
                              }
                            },
                            {
                              "text": "可愛すぎて感動……"
                            }
                          ]
                        },
                        "headerBackgroundColor": 4278239141,
                        "headerTextColor": 4278190080,
                        "bodyBackgroundColor": 4280150454,
                        "bodyTextColor": 4278190080,
                        "authorExternalChannelId": "UCDP7EFhA42ekKzaG0Mj7N8w",
                        "authorNameTextColor": 2315255808,
                        "authorBadges": [
                          {
                            "liveChatAuthorBadgeRenderer": {
                              "customThumbnail": {
                                "thumbnails": [
                                  {
                                    "url": "https://yt3.ggpht.com/EmgO-upNVOR4TzDh1SeS7dIGjNl1ILdGVSxT77_lf4PHVAzB92Z5E-Msg7fSYUGyP-XoJKB5qg=s16-c-k",
                                    "width": 16,
                                    "height": 16
                                  }
                                ]
                              },
                              "tooltip": "Member (6 months)",
                              "accessibility": {
                                "accessibilityData": {
                                  "label": "Member (6 months)"
                                }
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
            "durationSec": "111"
          }
        }
        """;

    /// <summary>
    /// Ticker paid-message with an <c>authorUsername</c> field on the outer renderer, providing
    /// the channel handle as <c>@路面そーだ</c>. Real data from watch_20260411_045605.jsonl —
    /// @路面そーだ, ¥5,630, Member (1 year), InugamiKorone stream.
    /// Verifies that <see cref="YTLiveChat.Contracts.Models.Author.ChannelHandle"/> is populated.
    /// </summary>
    public static string TickerPaidMessageWithAuthorUsername() =>
        """
        {
          "clickTrackingParams": "CAEQl98BIhMIypCM7czokwMVl2V6BR2dCSJ6ygEEI650eQ==",
          "addLiveChatTickerItemAction": {
            "item": {
              "liveChatTickerPaidMessageItemRenderer": {
                "id": "ChwKGkNLcTVudG5NNkpNREZVTEJ3Z1FkVEUwMEVn",
                "amountTextColor": 4294967295,
                "startBackgroundColor": 4293467747,
                "endBackgroundColor": 4290910299,
                "authorPhoto": {
                  "thumbnails": [
                    {
                      "url": "https://yt4.ggpht.com/NVez22B4wWs5VVuQ91Rhumn6fg46Kccom4Lg1-pwXhwAtSfkqde6fLS1QqYJq9DGw6dbpJzu=s32-c-k-c0x00ffffff-no-rj",
                      "width": 32,
                      "height": 32
                    },
                    {
                      "url": "https://yt4.ggpht.com/NVez22B4wWs5VVuQ91Rhumn6fg46Kccom4Lg1-pwXhwAtSfkqde6fLS1QqYJq9DGw6dbpJzu=s64-c-k-c0x00ffffff-no-rj",
                      "width": 64,
                      "height": 64
                    }
                  ],
                  "accessibility": {
                    "accessibilityData": {
                      "label": "@路面そーだ"
                    }
                  }
                },
                "durationSec": 1774,
                "showItemEndpoint": {
                  "clickTrackingParams": "COwCELDIBCITCMqQjO3M6JMDFZdlegUdnQkiesoBBCOudHk=",
                  "commandMetadata": {
                    "webCommandMetadata": {
                      "ignoreNavigation": true
                    }
                  },
                  "showLiveChatItemEndpoint": {
                    "renderer": {
                      "liveChatPaidMessageRenderer": {
                        "id": "ChwKGkNLcTVudG5NNkpNREZVTEJ3Z1FkVEUwMEVn",
                        "timestampUsec": "1776006759909883",
                        "authorName": {
                          "simpleText": "@路面そーだ"
                        },
                        "authorPhoto": {
                          "thumbnails": [
                            {
                              "url": "https://yt4.ggpht.com/NVez22B4wWs5VVuQ91Rhumn6fg46Kccom4Lg1-pwXhwAtSfkqde6fLS1QqYJq9DGw6dbpJzu=s32-c-k-c0x00ffffff-no-rj",
                              "width": 32,
                              "height": 32
                            },
                            {
                              "url": "https://yt4.ggpht.com/NVez22B4wWs5VVuQ91Rhumn6fg46Kccom4Lg1-pwXhwAtSfkqde6fLS1QqYJq9DGw6dbpJzu=s64-c-k-c0x00ffffff-no-rj",
                              "width": 64,
                              "height": 64
                            }
                          ]
                        },
                        "purchaseAmountText": {
                          "simpleText": "¥5,630"
                        },
                        "message": {
                          "runs": [
                            {
                              "text": "ころさん7thおめでと～"
                            }
                          ]
                        },
                        "headerBackgroundColor": 4287349200,
                        "headerTextColor": 4294967295,
                        "bodyBackgroundColor": 4293467747,
                        "bodyTextColor": 4278190080,
                        "authorExternalChannelId": "UCXEPOXmgUU6EZ3eqjxvbQ_A",
                        "authorNameTextColor": 2315255808,
                        "authorBadges": [
                          {
                            "liveChatAuthorBadgeRenderer": {
                              "customThumbnail": {
                                "thumbnails": [
                                  {
                                    "url": "https://yt3.ggpht.com/oy3BYNMH3mfxkpZFbbf4Y0FkES5eT4HctXftzK_5nNen2eidnd_wJ_RiKSjTMVUeBSDs9QlTew=s16-c-k",
                                    "width": 16,
                                    "height": 16
                                  },
                                  {
                                    "url": "https://yt3.ggpht.com/oy3BYNMH3mfxkpZFbbf4Y0FkES5eT4HctXftzK_5nNen2eidnd_wJ_RiKSjTMVUeBSDs9QlTew=s32-c-k",
                                    "width": 32,
                                    "height": 32
                                  }
                                ]
                              },
                              "tooltip": "Member (1 year)",
                              "accessibility": {
                                "accessibilityData": {
                                  "label": "Member (1 year)"
                                }
                              }
                            }
                          }
                        ],
                        "isV2Style": true
                      }
                    },
                    "trackingParams": "CO0CEI7RBiITCMqQjO3M6JMDFZdlegUdnQkieg=="
                  }
                },
                "authorExternalChannelId": "UCXEPOXmgUU6EZ3eqjxvbQ_A",
                "fullDurationSec": 1800,
                "trackingParams": "COwCELDIBCITCMqQjO3M6JMDFZdlegUdnQkieg==",
                "authorUsername": {
                  "simpleText": "@路面そーだ"
                }
              }
            },
            "durationSec": "1774"
          }
        }
        """;

    public static string TickerSponsorItemFromLog8() =>
        """
        {
          "clickTrackingParams": "CAEQl98BIhMIqZya5-fdkgMVettJBx0_CBUvygEE-ogb2A==",
          "addLiveChatTickerItemAction": {
            "item": {
              "liveChatTickerSponsorItemRenderer": {
                "id": "Ci8KLUNOU0p6SlhmeTVJREZXdjFsQWtkcjdnMTN3LUxveU1lc0lELTM1NDI0NzU5Nw%3D%3D",
                "detailText": {
                  "simpleText": "7 mths"
                },
                "showItemEndpoint": {
                  "showLiveChatItemEndpoint": {
                    "renderer": {
                      "liveChatMembershipItemRenderer": {
                        "id": "Ci8KLUNOU0p6SlhmeTVJREZXdjFsQWtkcjdnMTN3LUxveU1lc0lELTM1NDI0NzU5Nw%3D%3D",
                        "timestampUsec": "1771237989676166",
                        "authorExternalChannelId": "UCxBL52-3z0aGnmW0S3Z4xrQ",
                        "headerPrimaryText": {
                          "runs": [
                            {
                              "text": "Member for "
                            },
                            {
                              "text": "7"
                            },
                            {
                              "text": " months"
                            }
                          ]
                        },
                        "headerSubtext": {
                          "simpleText": "すずふぁーむ"
                        },
                        "message": {
                          "runs": [
                            {
                              "text": "可愛すぎて止まった心臓動き出した"
                            }
                          ]
                        },
                        "authorName": {
                          "simpleText": "@mutukisegawa3526"
                        },
                        "authorPhoto": {
                          "thumbnails": [
                            {
                              "url": "https://yt4.ggpht.com/ytc/AIdro_le2zBNV6ZmwElx8oZwOy7blz_AjxzboPWuR5xv2qSCzFc=s64-c-k-c0x00ffffff-no-rj",
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
                                    "url": "https://yt3.ggpht.com/EmgO-upNVOR4TzDh1SeS7dIGjNl1ILdGVSxT77_lf4PHVAzB92Z5E-Msg7fSYUGyP-XoJKB5qg=s16-c-k",
                                    "width": 16,
                                    "height": 16
                                  }
                                ]
                              },
                              "tooltip": "Member (6 months)",
                              "accessibility": {
                                "accessibilityData": {
                                  "label": "Member (6 months)"
                                }
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
            "durationSec": "92"
          }
        }
        """;
}
