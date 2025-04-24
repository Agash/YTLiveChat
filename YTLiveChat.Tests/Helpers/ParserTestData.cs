namespace YTLiveChat.Tests.Helpers;

internal static class ParserTestData
{
    // --- HTML Samples ---
    public const string SampleLivePageHtml = """
        <!DOCTYPE html><html><head>
        <link rel="canonical" href="https://www.youtube.com/watch?v=EXISTING_LIVE_ID">
        <script>
        window.ytcfg.set({
            "INNERTUBE_API_KEY": "TEST_API_KEY",
            "INNERTUBE_CONTEXT": { "client": { "clientVersion": "2.20240101.01.00", "clientName": "WEB" }},
            "INITIAL_DATA": { "contents": { "twoColumnWatchNextResults": { "conversationBar": { "liveChatRenderer": { "continuations": [ { "reloadContinuationData": { "continuation": "INITIAL_CONTINUATION_TOKEN" }}]}}}}}
        });
        </script></head><body></body></html>
        """;

    public const string SampleLivePageHtmlFinished = """
        <!DOCTYPE html><html><head>
        <link rel="canonical" href="https://www.youtube.com/watch?v=FINISHED_LIVE_ID">
        <script>
        window.ytcfg.set({
            "INNERTUBE_API_KEY": "TEST_API_KEY_FINISHED",
            "INNERTUBE_CONTEXT": { "client": { "clientVersion": "2.20240101.01.00", "clientName": "WEB" }},
            "playerResponse": { "playabilityStatus": { "liveStreamability": { "liveStreamabilityRenderer": { "offlineSlate": {}, "pollDelayMs": "15000" } } }, "videoDetails": { "isLiveContent": true, "isLive": false, "isLiveDvrEnabled": false }, "ytInitialPlayerResponse": { "playabilityStatus": { "liveStreamability": { "liveStreamabilityRenderer": { "offlineSlate": { "liveStreamOfflineSlateRenderer": { "scheduledStartTime": "1678886400" } } } } } } },
            "ytInitialData": { "playerOverlays": { "playerOverlayRenderer": { "endScreen": { "watchNextEndScreenRenderer": { "results": [] } } } } }
        });
        // Also check for isReplay flag if present
        var moreData = { "isReplay": true };
        </script></head><body></body></html>
        """; // Note: Detecting finished streams is tricky; relying on 'isReplay' is simpler.

    public const string SampleLivePageHtmlMissingKey = """
        <!DOCTYPE html><html><head>
        <link rel="canonical" href="https://www.youtube.com/watch?v=MISSING_KEY_ID">
        <script>
        window.ytcfg.set({
            // "INNERTUBE_API_KEY": "MISSING",
            "INNERTUBE_CONTEXT": { "client": { "clientVersion": "2.20240101.01.00", "clientName": "WEB" }},
            "INITIAL_DATA": { "contents": { "twoColumnWatchNextResults": { "conversationBar": { "liveChatRenderer": { "continuations": [ { "reloadContinuationData": { "continuation": "INITIAL_CONTINUATION_TOKEN" }}]}}}}}
        });
        </script></head><body></body></html>
        """;


    // --- JSON Samples ---

    // Base structure for wrapping item renderers into a valid LiveChatResponse
    private static string WrapItemInResponse(string itemRendererJson, string continuation = "NEXT_CONTINUATION")
    {
        string continuationJson = continuation == null
            ? ""
            : $$"""
                ,"continuations": [ { "timedContinuationData": { "continuation": "{{continuation}}", "timeoutMs": 5000 } } ]
                """;

        return $$"""
        {
          "responseContext": { /* ... */ },
          "continuationContents": {
            "liveChatContinuation": {
              "actions": [
                {
                  "addChatItemAction": {
                    "item": {{itemRendererJson}}
                  }
                }
              ]{{continuationJson}}
            }
          }
        }
        """;
    }


    public static string TextMessageJson(string id = "text-id-1", string author = "TestUser", string channelId = "UC123", string message = "Hello world!")
    {
        return WrapItemInResponse($$"""
            {
              "liveChatTextMessageRenderer": {
                "id": "{{id}}",
                "timestampUsec": "{{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000}}",
                "authorName": { "simpleText": "{{author}}" },
                "authorExternalChannelId": "{{channelId}}",
                "authorPhoto": { "thumbnails": [ { "url": "https://yt3.ggpht.com/...", "width": 32, "height": 32 } ] },
                "message": {
                  "runs": [ { "text": "{{message}}" } ]
                }
                // Add badges etc. if needed for specific tests
              }
            }
            """);
    }

    public static string TextMessageWithEmojiJson(string id = "emoji-id-1", string author = "EmojiFan", string channelId = "UC456", string text1 = "Look: ", string emojiText = "😊", string text2 = "!")
    {
        // A simplified Emoji structure for testing
        string emojiRunJson = $$"""
            {
              "emoji": {
                "emojiId": "😊", // This is often complex ID for custom, or unicode char for standard
                "shortcuts": [ ":)" ], // Example
                "searchTerms": [ "smile", "happy" ],
                "image": { "thumbnails": [ { "url": "https://..." } ], "accessibility": { "accessibilityData": { "label": "Smiling Face" } } },
                "isCustomEmoji": false
              }
            }
            """;
        return WrapItemInResponse($$"""
            {
              "liveChatTextMessageRenderer": {
                "id": "{{id}}",
                "timestampUsec": "{{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000}}",
                "authorName": { "simpleText": "{{author}}" },
                "authorExternalChannelId": "{{channelId}}",
                "authorPhoto": { "thumbnails": [ { "url": "https://..." } ] },
                "message": {
                  "runs": [
                    { "text": "{{text1}}" },
                    {{emojiRunJson}},
                    { "text": "{{text2}}" }
                  ]
                }
              }
            }
            """);
    }

    public static string SuperchatJson(string id = "sc-id-1", string author = "Supporter", string channelId = "UC789", string amount = "$5.00", string? message = "Great stream!", long bodyColor = -15376192L /* Blue */, long headerColor = -14680065L /* Darker Blue */, long bodyTextColor = -1L /* White */, long headerTextColor = -1L /* White */)
    {
        string messageJson = message == null ? "null" : $$"""{ "runs": [ { "text": "{{message}}" } ] }""";
        return WrapItemInResponse($$"""
            {
              "liveChatPaidMessageRenderer": {
                "id": "{{id}}",
                "timestampUsec": "{{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000}}",
                "authorName": { "simpleText": "{{author}}" },
                "authorExternalChannelId": "{{channelId}}",
                "authorPhoto": { "thumbnails": [ { "url": "https://..." } ] },
                "purchaseAmountText": { "simpleText": "{{amount}}" },
                "message": {{messageJson}},
                "headerBackgroundColor": {{headerColor}},
                "headerTextColor": {{headerTextColor}},
                "bodyBackgroundColor": {{bodyColor}},
                "bodyTextColor": {{bodyTextColor}},
                "authorNameTextColor": -16777216 // Black example
              }
            }
            """);
    }

    public static string SuperStickerJson(string id = "sticker-id-1", string author = "StickerFan", string channelId = "UCA BC", string amount = "¥1,000", long bgColor = 4280154210L /* Greenish? - FF1E88E5 -> -14680065 */, string stickerUrl = "https://sticker...", string stickerAlt = "Cute Sticker")
    {
        // Adjust bgColor to match expected type (long)
        long actualBgColor = -11619841L; // Green FF4CAF50 example

        return WrapItemInResponse($$"""
            {
              "liveChatPaidStickerRenderer": {
                "id": "{{id}}",
                "timestampUsec": "{{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000}}",
                "authorName": { "simpleText": "{{author}}" },
                "authorExternalChannelId": "{{channelId}}",
                "authorPhoto": { "thumbnails": [ { "url": "https://..." } ] },
                "purchaseAmountText": { "simpleText": "{{amount}}" },
                "sticker": {
                   "thumbnails": [ { "url": "{{stickerUrl}}", "width": 80, "height": 80 } ],
                   "accessibility": { "accessibilityData": { "label": "{{stickerAlt}}" } }
                 },
                "moneyChipBackgroundColor": {{actualBgColor}}, // Use adjusted value
                "moneyChipTextColor": -1, // White
                "backgroundColor": {{actualBgColor}}, // Use adjusted value
                "authorNameTextColor": -16777216, // Black
                "stickerDisplayWidth": 80,
                "stickerDisplayHeight": 80
              }
            }
            """);
    }


    public static string NewMemberJson(string id = "member-id-1", string author = "Newbie", string channelId = "UCDEF", string level = "Supporter Tier")
    {
        // Simulate badge tooltip providing level name
        string badgeJson = $$"""
             {
               "liveChatAuthorBadgeRenderer": {
                 "customThumbnail": { "thumbnails": [ { "url": "https://badge..." } ] },
                 "tooltip": "{{level}}",
                 "accessibility": { "accessibilityData": { "label": "{{level}}" } }
               }
             }
             """;

        return WrapItemInResponse($$"""
             {
               "liveChatMembershipItemRenderer": {
                 "id": "{{id}}",
                 "timestampUsec": "{{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000}}",
                 "authorName": { "simpleText": "{{author}}" },
                 "authorExternalChannelId": "{{channelId}}",
                 "authorPhoto": { "thumbnails": [ { "url": "https://..." } ] },
                 "authorBadges": [ {{badgeJson}} ],
                 "headerPrimaryText": { "runs": [ { "text": "Welcome to {{level}} membership!" } ] }, // Example format
                 "headerSubtext": { "simpleText": "New member" }
                 // "message" might be null or present for new members
               }
             }
             """);
    }


    public static string MembershipMilestoneJson(string id = "milestone-id-1", string author = "Veteran", string channelId = "UCGHI", string level = "Gold Tier", int months = 12, string userComment = "Still loving it!")
    {
        string badgeJson = $$"""
             {
               "liveChatAuthorBadgeRenderer": {
                 "customThumbnail": { "thumbnails": [ { "url": "https://badge..." } ] },
                 "tooltip": "{{level}} ({{months}} months)", // Tooltip might include duration
                 "accessibility": { "accessibilityData": { "label": "{{level}} ({{months}} months)" } }
               }
             }
             """;
        string messageJson = userComment == null ? "null" : $$"""{ "runs": [ { "text": "{{userComment}}" } ] }""";

        return WrapItemInResponse($$"""
             {
               "liveChatMembershipItemRenderer": {
                 "id": "{{id}}",
                 "timestampUsec": "{{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000}}",
                 "authorName": { "simpleText": "{{author}}" },
                 "authorExternalChannelId": "{{channelId}}",
                 "authorPhoto": { "thumbnails": [ { "url": "https://..." } ] },
                 "authorBadges": [ {{badgeJson}} ],
                 "headerPrimaryText": { "runs": [ { "text": "Member for {{months}} months" } ] },
                 "headerSubtext": null, // Often null for milestones
                 "message": {{messageJson}} // User's comment
               }
             }
             """);
    }


    public static string GiftPurchaseJson(string id = "gift-purchase-id-1", string gifter = "GenerousGuy", string channelId = "UCJKL", string level = "Silver Tier", int count = 5)
    {
        // Gift Purchase Announcement: Author in header IS the gifter
        string badgeJson = $$"""
             {
               "liveChatAuthorBadgeRenderer": {
                 "customThumbnail": { "thumbnails": [ { "url": "https://badge..." } ] },
                 "tooltip": "{{level}}", // Gifter's level badge
                 "accessibility": { "accessibilityData": { "label": "{{level}}" } }
               }
             }
             """;
        string headerJson = $$"""
             {
               "liveChatSponsorshipsHeaderRenderer": {
                 "authorName": { "simpleText": "{{gifter}}" },
                 "authorPhoto": { "thumbnails": [ { "url": "https://..." } ] },
                 "primaryText": { "runs": [ { "text": "Gifted {{count}} {{level}} memberships" } ] },
                 "authorBadges": [ {{badgeJson}} ]
                 // No authorExternalChannelId in header typically
               }
             }
             """;

        return WrapItemInResponse($$"""
             {
                "liveChatSponsorshipsGiftPurchaseAnnouncementRenderer": {
                    "id": "{{id}}",
                    "timestampUsec": "{{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000}}",
                    "header": {{headerJson}}
                    // authorExternalChannelId might be here or derived from header context
                    // For testing, assume parser gets it from header context if needed or relies on ChatItem author population logic
                 }
             }
             """);
    }

    public static string GiftRedemptionJson(string id = "gift-redeem-id-1", string recipient = "LuckyUser", string channelId = "UCMNO", string level = "Bronze Tier", string gifter = "MysteryBenefactor")
    {
        // Gift Redemption: Author IS the recipient
        string badgeJson = $$"""
             {
               "liveChatAuthorBadgeRenderer": {
                 "customThumbnail": { "thumbnails": [ { "url": "https://badge..." } ] },
                 "tooltip": "{{level}}", // Recipient's new level badge
                 "accessibility": { "accessibilityData": { "label": "{{level}}" } }
               }
             }
             """;

        // Message often includes gifter name
        string messageJson = $$"""
         {
            "runs": [
                { "text": "Welcome! You received a gift membership from " },
                { "text": "{{gifter}}", "bold": true }, // Gifter name might be bolded
                { "text": "!" }
             ]
         }
         """;

        return WrapItemInResponse($$"""
             {
               "liveChatSponsorshipsGiftRedemptionAnnouncementRenderer": {
                 "id": "{{id}}",
                 "timestampUsec": "{{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000}}",
                 "authorName": { "simpleText": "{{recipient}}" },
                 "authorExternalChannelId": "{{channelId}}",
                 "authorPhoto": { "thumbnails": [ { "url": "https://..." } ] },
                 "authorBadges": [ {{badgeJson}} ],
                 "message": {{messageJson}}
               }
             }
             """);
    }

    public static string ModeratorMessageJson(string id = "mod-msg-1", string author = "ModUser", string channelId = "UCMod")
    {
        string badgeJson = """
            {
              "liveChatAuthorBadgeRenderer": {
                "icon": { "iconType": "MODERATOR" },
                "tooltip": "Moderator",
                "accessibility": { "accessibilityData": { "label": "Moderator" } }
              }
            }
            """;
        return WrapItemInResponse($$"""
            {
              "liveChatTextMessageRenderer": {
                "id": "{{id}}",
                "timestampUsec": "{{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000}}",
                "authorName": { "simpleText": "{{author}}" },
                "authorExternalChannelId": "{{channelId}}",
                "authorPhoto": { "thumbnails": [ { "url": "https://..." } ] },
                "authorBadges": [ {{badgeJson}} ],
                "message": { "runs": [ { "text": "Please keep chat respectful." } ] }
              }
            }
            """);
    }

    // Add more samples: Owner, Verified, Member messages, edge cases (empty message, missing fields etc.)

    public static string ResponseWithNoContinuation()
    {
        // Simulate a response where the stream might have ended
        return WrapItemInResponse(TextMessageJson(message: "Last message maybe?"), continuation: null);
    }
}