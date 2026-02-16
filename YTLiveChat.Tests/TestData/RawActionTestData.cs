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
