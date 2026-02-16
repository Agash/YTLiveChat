namespace YTLiveChat.Tests.TestData;

internal static class RawActionLog9To15TestData
{
    public static string UpdateLiveChatPollActionFromLog() =>
        """
        {
          "updateLiveChatPollAction": {
            "pollToUpdate": {
              "pollRenderer": {
                "choices": [
                  {
                    "text": {
                      "runs": [
                        {
                          "text": "yes"
                        }
                      ]
                    },
                    "selected": false,
                    "voteRatio": 0.908861517906189,
                    "votePercentage": {
                      "simpleText": "91%"
                    },
                    "signinEndpoint": {
                      "commandMetadata": {
                        "webCommandMetadata": {
                          "url": "https://accounts.google.com/ServiceLogin?service=youtube&uilel=3&passive=true&continue=https%3A%2F%2Fwww.youtube.com%2Fsignin%3Faction_handle_signin%3Dtrue%26app%3Ddesktop%26hl%3Den&hl=en",
                          "webPageType": "WEB_PAGE_TYPE_UNKNOWN",
                          "rootVe": 83769
                        }
                      },
                      "signInEndpoint": {
                        "nextEndpoint": {}
                      }
                    }
                  },
                  {
                    "text": {
                      "runs": [
                        {
                          "text": "okayy"
                        }
                      ]
                    },
                    "selected": false,
                    "voteRatio": 0.091138511896133423,
                    "votePercentage": {
                      "simpleText": "9%"
                    },
                    "signinEndpoint": {
                      "commandMetadata": {
                        "webCommandMetadata": {
                          "url": "https://accounts.google.com/ServiceLogin?service=youtube&uilel=3&passive=true&continue=https%3A%2F%2Fwww.youtube.com%2Fsignin%3Faction_handle_signin%3Dtrue%26app%3Ddesktop%26hl%3Den&hl=en",
                          "webPageType": "WEB_PAGE_TYPE_UNKNOWN",
                          "rootVe": 83769
                        }
                      },
                      "signInEndpoint": {
                        "nextEndpoint": {}
                      }
                    }
                  }
                ],
                "liveChatPollId": "ChwKGkNLTFM2dXlZM1pJREZSZkR3Z1FkY1FFbUlR",
                "header": {
                  "pollHeaderRenderer": {
                    "pollQuestion": {
                      "runs": [
                        {
                          "text": "like & turn on notif"
                        }
                      ]
                    },
                    "thumbnail": {
                      "thumbnails": [
                        {
                          "url": "https://yt4.ggpht.com/XzRjez5LMtb3BTHBM2Q-hx7XNlzqrhoO5Yn_e7-hcbUlkeELGa2jjUxJ8x807suZTtv4_e-1Zp4=s32-c-k-c0x00ffffff-no-rj",
                          "width": 32,
                          "height": 32
                        },
                        {
                          "url": "https://yt4.ggpht.com/XzRjez5LMtb3BTHBM2Q-hx7XNlzqrhoO5Yn_e7-hcbUlkeELGa2jjUxJ8x807suZTtv4_e-1Zp4=s64-c-k-c0x00ffffff-no-rj",
                          "width": 64,
                          "height": 64
                        }
                      ]
                    },
                    "metadataText": {
                      "runs": [
                        {
                          "text": "@HarrisCaine"
                        },
                        {
                          "text": " • "
                        },
                        {
                          "text": "12h ago"
                        },
                        {
                          "text": " • "
                        },
                        {
                          "text": "13,869 votes"
                        }
                      ]
                    },
                    "liveChatPollType": "LIVE_CHAT_POLL_TYPE_CREATOR",
                    "contextMenuButton": {
                      "buttonRenderer": {
                        "icon": {
                          "iconType": "MORE_VERT"
                        },
                        "accessibility": {
                          "label": "Chat actions"
                        },
                        "accessibilityData": {
                          "accessibilityData": {
                            "label": "Chat actions"
                          }
                        },
                        "targetId": "live-chat-action-panel-poll-context-menu",
                        "command": {
                          "commandMetadata": {
                            "webCommandMetadata": {
                              "ignoreNavigation": true
                            }
                          },
                          "liveChatItemContextMenuEndpoint": {
                            "params": "Q2g0S0hBb2FRMHRNVXpaMWVWa3pXa2xFUmxKbVJIZG5VV1JqVVVWdFNWRWFLU29uQ2hoVlEzUkROMjlzVDJ4a2EzTllOR1pqYkY4NFdFdFZSa0VTQzJkMmNVY3lXbFEzYkhOTklBRW9CRElhQ2hoVlEzUkROMjlzVDJ4a2EzTllOR1pqYkY4NFdFdFZSa0U0QTBnQVVCVSUzRA=="
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;

    public static string ShowLiveChatActionPanelActionFromLog() =>
        """
        {
          "clickTrackingParams": "CAEQl98BIhMI9tyD7sLekgMVnJj0Bx1ECTIgygEE94FxRQ==",
          "showLiveChatActionPanelAction": {
            "panelToShow": {
              "liveChatActionPanelRenderer": {
                "contents": {
                  "pollRenderer": {
                    "choices": [
                      {
                        "text": {
                          "runs": [
                            {
                              "text": "yes"
                            }
                          ]
                        },
                        "selected": false,
                        "signinEndpoint": {
                          "clickTrackingParams": "CCsQiK0HIhMI9tyD7sLekgMVnJj0Bx1ECTIgygEE94FxRQ==",
                          "commandMetadata": {
                            "webCommandMetadata": {
                              "url": "https://accounts.google.com/ServiceLogin?service=youtube&uilel=3&passive=true&continue=https%3A%2F%2Fwww.youtube.com%2Fsignin%3Faction_handle_signin%3Dtrue%26app%3Ddesktop%26hl%3Den&hl=en",
                              "webPageType": "WEB_PAGE_TYPE_UNKNOWN",
                              "rootVe": 83769
                            }
                          },
                          "signInEndpoint": {
                            "nextEndpoint": {
                              "clickTrackingParams": "CCsQiK0HIhMI9tyD7sLekgMVnJj0Bx1ECTIgygEE94FxRQ=="
                            }
                          }
                        }
                      },
                      {
                        "text": {
                          "runs": [
                            {
                              "text": "okayy"
                            }
                          ]
                        },
                        "selected": false,
                        "signinEndpoint": {
                          "clickTrackingParams": "CCsQiK0HIhMI9tyD7sLekgMVnJj0Bx1ECTIgygEE94FxRQ==",
                          "commandMetadata": {
                            "webCommandMetadata": {
                              "url": "https://accounts.google.com/ServiceLogin?service=youtube&uilel=3&passive=true&continue=https%3A%2F%2Fwww.youtube.com%2Fsignin%3Faction_handle_signin%3Dtrue%26app%3Ddesktop%26hl%3Den&hl=en",
                              "webPageType": "WEB_PAGE_TYPE_UNKNOWN",
                              "rootVe": 83769
                            }
                          },
                          "signInEndpoint": {
                            "nextEndpoint": {
                              "clickTrackingParams": "CCsQiK0HIhMI9tyD7sLekgMVnJj0Bx1ECTIgygEE94FxRQ=="
                            }
                          }
                        }
                      }
                    ],
                    "trackingParams": "CCsQiK0HIhMI9tyD7sLekgMVnJj0Bx1ECTIg",
                    "liveChatPollId": "ChwKGkNLTFM2dXlZM1pJREZSZkR3Z1FkY1FFbUlR",
                    "header": {
                      "pollHeaderRenderer": {
                        "pollQuestion": {
                          "runs": [
                            {
                              "text": "like & turn on notif"
                            }
                          ]
                        },
                        "thumbnail": {
                          "thumbnails": [
                            {
                              "url": "https://yt4.ggpht.com/XzRjez5LMtb3BTHBM2Q-hx7XNlzqrhoO5Yn_e7-hcbUlkeELGa2jjUxJ8x807suZTtv4_e-1Zp4=s32-c-k-c0x00ffffff-no-rj",
                              "width": 32,
                              "height": 32
                            },
                            {
                              "url": "https://yt4.ggpht.com/XzRjez5LMtb3BTHBM2Q-hx7XNlzqrhoO5Yn_e7-hcbUlkeELGa2jjUxJ8x807suZTtv4_e-1Zp4=s64-c-k-c0x00ffffff-no-rj",
                              "width": 64,
                              "height": 64
                            }
                          ]
                        },
                        "metadataText": {
                          "runs": [
                            {
                              "text": "@HarrisCaine"
                            },
                            {
                              "text": " • "
                            },
                            {
                              "text": "12h ago"
                            },
                            {
                              "text": " • "
                            },
                            {
                              "text": "13,869 votes"
                            }
                          ]
                        },
                        "liveChatPollType": "LIVE_CHAT_POLL_TYPE_CREATOR",
                        "contextMenuButton": {
                          "buttonRenderer": {
                            "icon": {
                              "iconType": "MORE_VERT"
                            },
                            "accessibility": {
                              "label": "Chat actions"
                            },
                            "trackingParams": "CCwQ8FsiEwj23IPuwt6SAxWcmPQHHUQJMiA=",
                            "accessibilityData": {
                              "accessibilityData": {
                                "label": "Chat actions"
                              }
                            },
                            "targetId": "live-chat-action-panel-poll-context-menu",
                            "command": {
                              "clickTrackingParams": "CCwQ8FsiEwj23IPuwt6SAxWcmPQHHUQJMiDKAQT3gXFF",
                              "commandMetadata": {
                                "webCommandMetadata": {
                                  "ignoreNavigation": true
                                }
                              },
                              "liveChatItemContextMenuEndpoint": {
                                "params": "Q2g0S0hBb2FRMHRNVXpaMWVWa3pXa2xFUmxKbVJIZG5VV1JqVVVWdFNWRWFLU29uQ2hoVlEzUkROMjlzVDJ4a2EzTllOR1pqYkY4NFdFdFZSa0VTQzJkMmNVY3lXbFEzYkhOTklBRW9CRElhQ2hoVlEzUkROMjlzVDJ4a2EzTllOR1pqYkY4NFdFdFZSa0U0QTBnQVVCVSUzRA=="
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                },
                "id": "ChwKGkNLTFM2dXlZM1pJREZSZkR3Z1FkY1FFbUlR",
                "targetId": "live-chat-action-panel"
              }
            }
          }
        }
        """;

    public static string AddBannerToLiveChatCommandFromLog() =>
        """
        {
          "addBannerToLiveChatCommand": {
            "bannerRenderer": {
              "liveChatBannerRenderer": {
                "contents": {
                  "liveChatBannerRedirectRenderer": {
                    "bannerMessage": {
                      "runs": [
                        {
                          "text": "Don't miss out! People are going to watch something from ",
                          "fontFace": "FONT_FACE_ROBOTO_REGULAR"
                        },
                        {
                          "text": "@Lutra_rescute",
                          "bold": true,
                          "fontFace": "FONT_FACE_ROBOTO_REGULAR"
                        }
                      ]
                    },
                    "authorPhoto": {
                      "thumbnails": [
                        {
                          "url": "https://yt4.ggpht.com/x8YIHOt-bE9D3u-bGrQrL-KIp4docMySvKeaoHab_9dor5j3TjtbmhT04cJCybWu4_p65HYRaQ=s32-c-k-c0x00ffffff-no-rj",
                          "width": 32,
                          "height": 32
                        },
                        {
                          "url": "https://yt4.ggpht.com/x8YIHOt-bE9D3u-bGrQrL-KIp4docMySvKeaoHab_9dor5j3TjtbmhT04cJCybWu4_p65HYRaQ=s64-c-k-c0x00ffffff-no-rj",
                          "width": 64,
                          "height": 64
                        }
                      ]
                    },
                    "inlineActionButton": {
                      "buttonRenderer": {
                        "style": "STYLE_DEFAULT",
                        "size": "SIZE_DEFAULT",
                        "isDisabled": false,
                        "text": {
                          "runs": [
                            {
                              "text": "Go now"
                            }
                          ]
                        },
                        "command": {
                          "commandMetadata": {
                            "webCommandMetadata": {
                              "url": "/watch?v=DFyEjE4ijRo",
                              "webPageType": "WEB_PAGE_TYPE_WATCH",
                              "rootVe": 3832
                            }
                          },
                          "watchEndpoint": {
                            "videoId": "DFyEjE4ijRo"
                          }
                        }
                      }
                    },
                    "contextMenuButton": {
                      "buttonRenderer": {
                        "icon": {
                          "iconType": "MORE_VERT"
                        },
                        "accessibility": {
                          "label": "Chat actions"
                        },
                        "accessibilityData": {
                          "accessibilityData": {
                            "label": "Chat actions"
                          }
                        },
                        "command": {
                          "commandMetadata": {
                            "webCommandMetadata": {
                              "ignoreNavigation": true
                            }
                          },
                          "liveChatItemContextMenuEndpoint": {
                            "params": "Q2g0S0hBb2FRMHhQU0RGWlJFTXpjRWxFUmxkRVZXeEJhMlJmWVVGWGVFRWFLU29uQ2hoVlEzZDZjRmh0VjBGR1JWWkxTRE5XZW5kMlUyeFpYM2NTQzFsb1pIcE1URzFUZG5SQklBRW9CRElFRWdJSUJqZ0JTQUJRSlElM0QlM0Q="
                          }
                        }
                      }
                    }
                  }
                },
                "actionId": "ChwKGkNMT0gxWURDM3BJREZXRFVsQWtkX2FBV3hB",
                "targetId": "live-chat-banner",
                "isStackable": true,
                "bannerType": "LIVE_CHAT_BANNER_TYPE_CROSS_CHANNEL_REDIRECT"
              }
            }
          }
        }
        """;

    public static string TickerGiftPurchaseFromLog() =>
        """
        {
          "clickTrackingParams": "CAEQl98BIhMIgYfCt5LekgMVRYl8Bh03Pi2PygEElvFFkw==",
          "addLiveChatTickerItemAction": {
            "item": {
              "liveChatTickerSponsorItemRenderer": {
                "id": "ChwKGkNLZW9tUDJNM3BJREZTY19yUVlka21JUlpB",
                "detailText": {
                  "accessibility": {
                    "accessibilityData": {
                      "label": "@林宏儒-r3b sent 10 gift memberships"
                    }
                  },
                  "simpleText": "10"
                },
                "detailTextColor": 4294967295,
                "startBackgroundColor": 4279213400,
                "endBackgroundColor": 4278943811,
                "sponsorPhoto": {
                  "thumbnails": [
                    {
                      "url": "https://yt4.ggpht.com/ytc/AIdro_m31jBKOnAO9IBvo5vjiMUs2_Ynha-9ZpAMxNlomV82dhA=s32-c-k-c0x00ffffff-no-rj",
                      "width": 32,
                      "height": 32
                    },
                    {
                      "url": "https://yt4.ggpht.com/ytc/AIdro_m31jBKOnAO9IBvo5vjiMUs2_Ynha-9ZpAMxNlomV82dhA=s64-c-k-c0x00ffffff-no-rj",
                      "width": 64,
                      "height": 64
                    }
                  ]
                },
                "durationSec": 353,
                "showItemEndpoint": {
                  "clickTrackingParams": "CCAQ6ocJIhMIgYfCt5LekgMVRYl8Bh03Pi2PygEElvFFkw==",
                  "commandMetadata": {
                    "webCommandMetadata": {
                      "ignoreNavigation": true
                    }
                  },
                  "showLiveChatItemEndpoint": {
                    "renderer": {
                      "liveChatSponsorshipsGiftPurchaseAnnouncementRenderer": {
                        "authorExternalChannelId": "UCz3K6hbsVpgmaJ1TqlxeXhA",
                        "header": {
                          "liveChatSponsorshipsHeaderRenderer": {
                            "authorName": {
                              "simpleText": "@林宏儒-r3b"
                            },
                            "authorPhoto": {
                              "thumbnails": [
                                {
                                  "url": "https://yt4.ggpht.com/ytc/AIdro_m31jBKOnAO9IBvo5vjiMUs2_Ynha-9ZpAMxNlomV82dhA=s32-c-k-c0x00ffffff-no-rj",
                                  "width": 32,
                                  "height": 32
                                },
                                {
                                  "url": "https://yt4.ggpht.com/ytc/AIdro_m31jBKOnAO9IBvo5vjiMUs2_Ynha-9ZpAMxNlomV82dhA=s64-c-k-c0x00ffffff-no-rj",
                                  "width": 64,
                                  "height": 64
                                }
                              ]
                            },
                            "primaryText": {
                              "runs": [
                                {
                                  "text": "Sent ",
                                  "bold": true
                                },
                                {
                                  "text": "10",
                                  "bold": true
                                },
                                {
                                  "text": " ",
                                  "bold": true
                                },
                                {
                                  "text": "AZKi Channel",
                                  "bold": true
                                },
                                {
                                  "text": " gift memberships",
                                  "bold": true
                                }
                              ]
                            },
                            "authorBadges": [
                              {
                                "liveChatAuthorBadgeRenderer": {
                                  "customThumbnail": {
                                    "thumbnails": [
                                      {
                                        "url": "https://yt3.ggpht.com/zCsQBvBQfTJOIMdyTir2SFyOSxFDYbGq8EGsrZVzAy4mC10PjFU_2f7FCP6BFm_0TJa_FnQBCg=s16-c-k",
                                        "width": 16,
                                        "height": 16
                                      },
                                      {
                                        "url": "https://yt3.ggpht.com/zCsQBvBQfTJOIMdyTir2SFyOSxFDYbGq8EGsrZVzAy4mC10PjFU_2f7FCP6BFm_0TJa_FnQBCg=s32-c-k",
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
                            "contextMenuEndpoint": {
                              "clickTrackingParams": "CCIQ3MMKIhMIgYfCt5LekgMVRYl8Bh03Pi2PygEElvFFkw==",
                              "commandMetadata": {
                                "webCommandMetadata": {
                                  "ignoreNavigation": true
                                }
                              },
                              "liveChatItemContextMenuEndpoint": {
                                "params": "Q2g0S0hBb2FRMHRsYjIxUU1rMHpjRWxFUmxOalgzSlJXV1JyYlVsU1drRWFLU29uQ2hoVlF6QlVXR1ZmVEZsYU5ITmpZVmN5V0UxNWFUVmZhM2NTQzNkU2FHTlVWMjlOYm5wRklBRW9CRElhQ2hoVlEzb3pTelpvWW5OV2NHZHRZVW94VkhGc2VHVllhRUU0QWtnQVVDUSUzRA=="
                              }
                            },
                            "contextMenuAccessibility": {
                              "accessibilityData": {
                                "label": "Chat actions"
                              }
                            },
                            "image": {
                              "thumbnails": [
                                {
                                  "url": "https://www.gstatic.com/youtube/img/sponsorships/sponsorships_gift_purchase_announcement_artwork.png"
                                }
                              ]
                            }
                          }
                        }
                      }
                    },
                    "trackingParams": "CCEQjtEGIhMIgYfCt5LekgMVRYl8Bh03Pi2P"
                  }
                },
                "authorExternalChannelId": "UCz3K6hbsVpgmaJ1TqlxeXhA",
                "fullDurationSec": 1800,
                "trackingParams": "CCAQ6ocJIhMIgYfCt5LekgMVRYl8Bh03Pi2P",
                "detailIcon": {
                  "iconType": "STAR_CIRCLE_RIBBON"
                }
              }
            },
            "durationSec": "353"
          }
        }
        """;
}
