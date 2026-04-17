using System.Text.Json;

using YTLiveChat.Contracts.Models;
using YTLiveChat.Helpers;
using YTLiveChat.Models.Response;
using YTLiveChat.Tests.TestData;

namespace YTLiveChat.Tests.Helpers;

[TestClass]
public class ParserTests
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private ChatItem? ParseRendererContentToChatItem(
        string rendererContentJson,
        string rendererType
    )
    {
        string addChatItemActionItemJson = $"{{ \"{rendererType}\": {rendererContentJson} }}";
        string actionJson =
            $$"""{ "addChatItemAction": { "item": {{addChatItemActionItemJson}}, "clientId": "TEST_CLIENT_ID_FOR_PARSER_TEST" } }""";
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            actionJson,
            s_jsonOptions
        );
        Assert.IsNotNull(action, $"Failed to deserialize Action from: {actionJson}");
        Assert.IsNotNull(
            action.AddChatItemAction,
            $"AddChatItemAction is null in Action from: {actionJson}"
        );
        Assert.IsNotNull(
            action.AddChatItemAction.Item,
            $"AddChatItemAction.Item is null in Action from: {actionJson}"
        );
        return Parser.ToChatItem(action);
    }

    private static string LoadWebSnapshot(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "WebSnapshots", fileName);
        Assert.IsTrue(File.Exists(path), $"Snapshot file not found: {path}");
        return WebSnapshotTestData.LoadWebSnapshot(fileName);
    }

    [TestMethod]
    public void ToChatItem_SimpleTextMessage1_ParsesCorrectly()
    {
        string rendererContentJson = TextMessageTestData.SimpleTextMessage1();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatTextMessageRenderer"
        );

        Assert.IsNotNull(chatItem, "ChatItem should not be null.");
        Assert.AreEqual("MSG_ID_SIMPLE_01", chatItem.Id);
        Assert.AreEqual("TestUser1", chatItem.Author.Name);
        Assert.AreEqual("UC_CHANNEL_ID_01", chatItem.Author.ChannelId);
        Assert.AreEqual(1, chatItem.Message.Length);
        _ = Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
        Assert.AreEqual("Hello World", ((TextPart)chatItem.Message[0]).Text);
        Assert.IsNull(chatItem.Author.Badge);
        Assert.IsNull(chatItem.Superchat);
        Assert.IsNull(chatItem.MembershipDetails);
        Assert.IsFalse(chatItem.IsOwner);
        Assert.IsFalse(chatItem.IsModerator);
        Assert.IsFalse(chatItem.IsVerified);
        Assert.IsTrue(
            (DateTimeOffset.UtcNow - chatItem.Timestamp).TotalSeconds < 60,
            "Timestamp seems too old or in the future."
        );
    }

    [TestMethod]
    public void ToChatItem_TextMessageWithStandardEmoji_ParsesCorrectly()
    {
        string rendererContentJson = TextMessageTestData.TextMessageWithStandardEmoji();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatTextMessageRenderer"
        );

        Assert.IsNotNull(chatItem, "ChatItem should not be null.");
        Assert.AreEqual("MSG_ID_STD_EMOJI_01", chatItem.Id);
        Assert.AreEqual("EmojiFan", chatItem.Author.Name);
        Assert.AreEqual(2, chatItem.Message.Length);
        _ = Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
        Assert.AreEqual("Congratulations ", ((TextPart)chatItem.Message[0]).Text);
        _ = Assert.IsInstanceOfType<EmojiPart>(chatItem.Message[1]);
        EmojiPart emojiPart = (EmojiPart)chatItem.Message[1];
        Assert.AreEqual("🥳", emojiPart.EmojiText);
        Assert.AreEqual(":partying_face:", emojiPart.Alt);
        Assert.AreEqual("https://fonts.gstatic.com/s/e/notoemoji/15.1/1f973/72.png", emojiPart.Url);
        Assert.IsFalse(emojiPart.IsCustomEmoji);
    }

    [TestMethod]
    public void ToChatItem_TextMessageWithCustomEmoji_ParsesCorrectly()
    {
        string rendererContentJson = TextMessageTestData.TextMessageWithCustomEmoji();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatTextMessageRenderer"
        );

        Assert.IsNotNull(chatItem, "ChatItem should not be null.");
        Assert.AreEqual("MSG_ID_CUSTOM_EMOJI_01", chatItem.Id);
        Assert.AreEqual("ChannelSupporter", chatItem.Author.Name);
        Assert.AreEqual(2, chatItem.Message.Length);
        _ = Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
        Assert.AreEqual("Check this out: ", ((TextPart)chatItem.Message[0]).Text);
        _ = Assert.IsInstanceOfType<EmojiPart>(chatItem.Message[1]);
        EmojiPart emojiPart = (EmojiPart)chatItem.Message[1];
        Assert.IsTrue(emojiPart.IsCustomEmoji);
        Assert.AreEqual(":customcat:", emojiPart.EmojiText);
        Assert.AreEqual(":customcat:", emojiPart.Alt);
        Assert.AreEqual("https://yt3.ggpht.com/placeholder/custom_cat_s48.png", emojiPart.Url);
    }

    [TestMethod]
    public void ToChatItem_MultiPartTextMessageWithMixedContent_ParsesCorrectly()
    {
        string rendererContentJson = TextMessageTestData.MultiPartTextMessageWithMixedContent();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatTextMessageRenderer"
        );

        Assert.IsNotNull(chatItem, "ChatItem should not be null.");
        Assert.AreEqual("MSG_ID_MIXED_01", chatItem.Id);
        Assert.AreEqual("MixedUser", chatItem.Author.Name);
        Assert.AreEqual(3, chatItem.Message.Length);
        _ = Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
        Assert.AreEqual("Text part 1, ", ((TextPart)chatItem.Message[0]).Text);
        _ = Assert.IsInstanceOfType<EmojiPart>(chatItem.Message[1]);
        EmojiPart emojiPart = (EmojiPart)chatItem.Message[1];
        Assert.AreEqual("👍", emojiPart.EmojiText);
        Assert.AreEqual(":thumbs_up:", emojiPart.Alt);
        Assert.IsFalse(emojiPart.IsCustomEmoji);
        _ = Assert.IsInstanceOfType<TextPart>(chatItem.Message[2]);
        Assert.AreEqual(" then more text.", ((TextPart)chatItem.Message[2]).Text);
    }

    [TestMethod]
    public void ToChatItem_TextMessageWithNonLatinCharacters_ParsesCorrectly()
    {
        string rendererContentJson = TextMessageTestData.TextMessageWithNonLatinCharacters();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatTextMessageRenderer"
        );

        Assert.IsNotNull(chatItem, "ChatItem should not be null.");
        Assert.AreEqual("MSG_ID_NON_LATIN_01", chatItem.Id);
        Assert.AreEqual("Пользователь1", chatItem.Author.Name);
        Assert.AreEqual(1, chatItem.Message.Length);
        _ = Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
        Assert.AreEqual("Привет мир", ((TextPart)chatItem.Message[0]).Text);
    }

    [TestMethod]
    public void ToChatItem_TextMessageWithViewerLeaderboardRank_ParsesCorrectly()
    {
        string rendererContentJson = TextMessageTestData.TextMessageWithViewerLeaderboardRank();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatTextMessageRenderer"
        );

        Assert.IsNotNull(chatItem, "ChatItem should not be null.");
        Assert.AreEqual("MSG_ID_RANK_01", chatItem.Id);
        Assert.AreEqual("RankedUser", chatItem.Author.Name);
        Assert.AreEqual(1, chatItem.Message.Length);
        _ = Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
        Assert.AreEqual("ranked message sample", ((TextPart)chatItem.Message[0]).Text);
        Assert.AreEqual(2, chatItem.ViewerLeaderboardRank);
    }

    [TestMethod]
    public void ToChatItem_SuperChatMessagePaidMessage1_ParsesCorrectly()
    {
        string rendererContentJson = SuperChatTestData.SuperChatMessagePaidMessage1();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatPaidMessageRenderer"
        );

        Assert.IsNotNull(chatItem, "ChatItem should not be null for SuperChatMessagePaidMessage1.");
        Assert.AreEqual("SC_ID_PAID_MSG_01", chatItem.Id);
        Assert.AreEqual("SuperFanPaidMsg1", chatItem.Author.Name);
        Assert.AreEqual("UC_CHANNEL_ID_SF_PM_01", chatItem.Author.ChannelId);
        Assert.IsNotNull(chatItem.Superchat, "Superchat details should not be null.");
        Assert.AreEqual("$10.00", chatItem.Superchat.AmountString);
        Assert.AreEqual(10.00m, chatItem.Superchat.AmountValue);
        Assert.AreEqual("USD", chatItem.Superchat.Currency);

        // TODO: Need to correctly find examples from YouTube, these are arbitrary values for now.
        //Assert.AreEqual("FFCC00", chatItem.Superchat.HeaderBackgroundColor);
        //Assert.AreEqual("DE000000", chatItem.Superchat.HeaderTextColor);
        //Assert.AreEqual("FFE658", chatItem.Superchat.BodyBackgroundColor);
        //Assert.AreEqual("DE000000", chatItem.Superchat.BodyTextColor);
        //Assert.AreEqual("8A000000", chatItem.Superchat.AuthorNameTextColor);

        Assert.AreEqual(1, chatItem.Message.Length);
        Assert.AreEqual("Great stream! Keep it up!", ((TextPart)chatItem.Message[0]).Text);
    }

    [TestMethod]
    public void ToChatItem_SuperChatMessageFromLatestLog1_ParsesCorrectly()
    {
        string rendererContentJson = SuperChatTestData.SuperChatMessageFromLatestLog();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatPaidMessageRenderer"
        );

        Assert.IsNotNull(
            chatItem,
            "ChatItem should not be null for SuperChatMessageFromLatestLog1."
        );
        Assert.AreEqual("SC_ID_LATEST_01", chatItem.Id);
        Assert.AreEqual("ArmbarAssassinPlaceholder", chatItem.Author.Name);
        Assert.IsNotNull(chatItem.Superchat, "Superchat details should not be null.");
        Assert.AreEqual("$10.00", chatItem.Superchat.AmountString);
        Assert.AreEqual(10.00m, chatItem.Superchat.AmountValue);
        Assert.AreEqual("USD", chatItem.Superchat.Currency);

        // TODO: Need to correctly find examples from YouTube, these are arbitrary values for now.
        //Assert.AreEqual("FFCC00", chatItem.Superchat.HeaderBackgroundColor);
        //Assert.AreEqual("000000", chatItem.Superchat.HeaderTextColor);
        //Assert.AreEqual("FFE658", chatItem.Superchat.BodyBackgroundColor);
        //Assert.AreEqual("000000", chatItem.Superchat.BodyTextColor);
        //Assert.AreEqual("000000", chatItem.Superchat.AuthorNameTextColor);

        Assert.AreEqual(1, chatItem.Message.Length);
        Assert.AreEqual(
            "Rich and Kylie Subathon make it happen captain. At 3 rip shots brotha",
            ((TextPart)chatItem.Message[0]).Text
        );
    }

    [TestMethod]
    public void ToChatItem_SuperChatMessagePaidMessage2_DifferentAmountAndCurrency_ParsesCorrectly()
    {
        string rendererContentJson =
            SuperChatTestData.SuperChatMessagePaidMessage2_DifferentAmountAndCurrency();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatPaidMessageRenderer"
        );

        Assert.IsNotNull(chatItem, "ChatItem should not be null for SuperChatMessage2.");
        Assert.AreEqual("SC_ID_PLACEHOLDER_02", chatItem.Id);
        Assert.AreEqual("EuroSupporter", chatItem.Author.Name);
        Assert.IsNotNull(chatItem.Superchat, "Superchat details should not be null.");
        Assert.AreEqual("€5.00", chatItem.Superchat.AmountString);
        Assert.AreEqual(5.00m, chatItem.Superchat.AmountValue);
        Assert.AreEqual("EUR", chatItem.Superchat.Currency);

        // TODO: Need to correctly find examples from YouTube, these are arbitrary values for now.
        //Assert.AreEqual("00FFAB", chatItem.Superchat.HeaderBackgroundColor);
        //Assert.AreEqual("000000", chatItem.Superchat.HeaderTextColor);
        //Assert.AreEqual("1E88B6", chatItem.Superchat.BodyBackgroundColor);
        //Assert.AreEqual("000000", chatItem.Superchat.BodyTextColor);
        //Assert.AreEqual("000000", chatItem.Superchat.AuthorNameTextColor);
    }

    [TestMethod]
    public void ToChatItem_SuperChatMessage_MultipleCurrencyFormats_ParsesCorrectly()
    {
        (string rendererJson, string expectedId, decimal expectedAmount, string expectedCurrency)[] cases =
        [
            (
                SuperChatTestData.SuperChatMessageAudPrefixSymbol(),
                "SC_ID_CURRENCY_AUD_01",
                10.00m,
                "AUD"
            ),
            (
                SuperChatTestData.SuperChatMessageHkdPrefixSymbol(),
                "SC_ID_CURRENCY_HKD_01",
                25.00m,
                "HKD"
            ),
            (
                SuperChatTestData.SuperChatMessagePlnCode(),
                "SC_ID_CURRENCY_PLN_01",
                10.00m,
                "PLN"
            ),
            (
                SuperChatTestData.SuperChatMessageArsCodePrefix(),
                "SC_ID_CURRENCY_ARS_01",
                2500m,
                "ARS"
            ),
            (
                SuperChatTestData.SuperChatMessageVndSymbol(),
                "SC_ID_CURRENCY_VND_01",
                20000m,
                "VND"
            ),
        ];

        foreach ((string rendererJson, string expectedId, decimal expectedAmount, string expectedCurrency) in cases)
        {
            ChatItem? chatItem = ParseRendererContentToChatItem(
                rendererJson,
                "liveChatPaidMessageRenderer"
            );

            Assert.IsNotNull(chatItem, $"ChatItem should not be null for {expectedId}.");
            Assert.AreEqual(expectedId, chatItem.Id, $"Unexpected id for {expectedId}.");
            Assert.IsNotNull(chatItem.Superchat, $"Superchat should not be null for {expectedId}.");
            Assert.AreEqual(
                expectedAmount,
                chatItem.Superchat.AmountValue,
                $"Unexpected amount for {expectedId}."
            );
            Assert.AreEqual(
                expectedCurrency,
                chatItem.Superchat.Currency,
                $"Unexpected currency for {expectedId}."
            );
        }
    }

    [TestMethod]
    public void ToChatItem_NewMemberChickenMcNugget_ParsesCorrectly()
    {
        string rendererContentJson = MembershipTestData.NewMemberChickenMcNugget();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatMembershipItemRenderer"
        );

        Assert.IsNotNull(chatItem, "ChatItem should not be null for NewMemberChickenMcNugget.");
        Assert.AreEqual("NEW_MEMBER_CHICKEN_ID", chatItem.Id);
        Assert.AreEqual("ChickenMcNuggetPlaceholder", chatItem.Author.Name);
        Assert.AreEqual("UC6pydynNYoEGABzcY_zuolw_placeholder", chatItem.Author.ChannelId);
        Assert.IsNotNull(chatItem.MembershipDetails, "MembershipDetails should not be null.");
        Assert.AreEqual(
            MembershipEventType.New,
            chatItem.MembershipDetails.EventType,
            "Event type should be New."
        );

        Assert.AreEqual(
            "The Plusers",
            chatItem.MembershipDetails.LevelName,
            "Level name from headerSubtext should represent tier."
        );
        Assert.AreEqual("Member (6 months)", chatItem.MembershipDetails.MembershipBadgeLabel);
        Assert.IsNotNull(
            chatItem.MembershipDetails.HeaderSubtext,
            "HeaderSubtext should not be null."
        );
        Assert.IsTrue(
            chatItem.MembershipDetails.HeaderSubtext.Contains("Welcome to The Plusers!"),
            "HeaderSubtext mismatch."
        );
        Assert.IsNotNull(chatItem.Author.Badge, "Author badge should not be null.");
        Assert.AreEqual("Member (6 months)", chatItem.Author.Badge.Label, "Badge label mismatch.");
        Assert.IsTrue(chatItem.IsMembership, "IsMembership flag should be true.");
        Assert.AreEqual(
            3,
            chatItem.Message.Length,
            "New member announcements should preserve welcome message runs."
        );
        Assert.AreEqual("Welcome to ", ((TextPart)chatItem.Message[0]).Text);
        Assert.AreEqual("The Plusers", ((TextPart)chatItem.Message[1]).Text);
        Assert.AreEqual("!", ((TextPart)chatItem.Message[2]).Text);
    }

    [TestMethod]
    public void ToChatItem_NewMemberWithExclamationTier_RatBoss_ParsesCorrectly()
    {
        string rendererContentJson = MembershipTestData.NewMemberWithExclamationTier_RatBoss();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatMembershipItemRenderer"
        );

        Assert.IsNotNull(chatItem);
        Assert.IsNotNull(chatItem.MembershipDetails);
        Assert.AreEqual(MembershipEventType.New, chatItem.MembershipDetails.EventType);
        Assert.AreEqual("Rat Boss!", chatItem.MembershipDetails.LevelName, "Tier name should preserve '!'.");
        Assert.AreEqual("Welcome to Rat Boss!!", chatItem.MembershipDetails.HeaderSubtext);
    }

    // ── Tier-upgrade tests (real InnerTube capture) ────────────────────────────
    // Real event captured 2026-04-15: @rembray upgraded to "Cardinal Archer".
    // See https://github.com/Agash/YTLiveChat/issues/42 for the tracking issue.

    /// <summary>
    /// Regression guard: the "Upgraded membership to" prefix must NOT trigger the New-member
    /// detection path. Verified with a real InnerTube capture.
    /// </summary>
    [TestMethod]
    public void ToChatItem_RealUpgrade_CardinalArcher_DoesNotClassifyAsNew()
    {
        string rendererContentJson = MembershipTestData.RealUpgrade_Runs_CardinalArcher();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatMembershipItemRenderer"
        );

        Assert.IsNotNull(chatItem);
        Assert.IsNotNull(chatItem.MembershipDetails);
        Assert.AreNotEqual(
            MembershipEventType.New,
            chatItem.MembershipDetails.EventType,
            "An 'Upgraded membership to' payload must NOT be classified as New."
        );
        Assert.IsTrue(chatItem.IsMembership, "IsMembership should be true for upgrade events.");
    }

    /// <summary>
    /// Happy path: real capture of runs shape ["Upgraded membership to ", "Cardinal Archer", "!"]
    /// is classified as Upgraded and tier name "Cardinal Archer" is extracted from the second run.
    /// </summary>
    [TestMethod]
    public void ToChatItem_RealUpgrade_CardinalArcher_ParsesUpgradedEventAndTierName()
    {
        string rendererContentJson = MembershipTestData.RealUpgrade_Runs_CardinalArcher();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatMembershipItemRenderer"
        );

        Assert.IsNotNull(chatItem);
        Assert.IsNotNull(chatItem.MembershipDetails);
        Assert.AreEqual(
            MembershipEventType.Upgraded,
            chatItem.MembershipDetails.EventType,
            "EventType should be Upgraded for 'Upgraded membership to' headerSubtext."
        );
        Assert.AreEqual(
            "Cardinal Archer",
            chatItem.MembershipDetails.LevelName,
            "Tier name should be extracted from the second run."
        );
        Assert.AreEqual("Upgraded membership to Cardinal Archer!", chatItem.MembershipDetails.HeaderSubtext);
        Assert.AreEqual("@rembray", chatItem.Author.Name);
        Assert.AreEqual("UCdtey2zoNQ9HVgdK9oEA_RA", chatItem.Author.ChannelId);
        Assert.IsTrue(chatItem.IsMembership);
    }

    [TestMethod]
    public void ToChatItem_NewMemberFromLatestLog_WithNewMemberBadge_ParsesCorrectly()
    {
        string rendererContentJson = MembershipTestData.NewMemberFromLatestLogWithNewMemberBadge();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatMembershipItemRenderer"
        );

        Assert.IsNotNull(chatItem, "ChatItem should not be null for latest new member schema.");
        Assert.AreEqual("ChwKGkNMUEltT1BRM1pJREZTX0J3Z1FkNUljbU1n", chatItem.Id);
        Assert.AreEqual("@米喬-h9l", chatItem.Author.Name);
        Assert.AreEqual("UCZff3zMXqYWgH-tL1OHBU1g", chatItem.Author.ChannelId);
        Assert.IsNotNull(chatItem.MembershipDetails, "MembershipDetails should not be null.");
        Assert.AreEqual(MembershipEventType.New, chatItem.MembershipDetails.EventType);
        Assert.AreEqual(
            "the Ukiverse",
            chatItem.MembershipDetails.LevelName,
            "Level should be parsed from welcome subtext when badge is generic."
        );
        Assert.AreEqual("New member", chatItem.MembershipDetails.MembershipBadgeLabel);
        Assert.AreEqual(
            "Welcome to the Ukiverse!",
            chatItem.MembershipDetails.HeaderSubtext,
            "HeaderSubtext should be composed from runs."
        );
        Assert.IsNotNull(chatItem.Author.Badge, "Author badge should be parsed.");
        Assert.AreEqual("New member", chatItem.Author.Badge.Label);
        Assert.IsTrue(chatItem.IsMembership);
        Assert.AreEqual(3, chatItem.Message.Length);
        Assert.AreEqual("Welcome to ", ((TextPart)chatItem.Message[0]).Text);
        Assert.AreEqual("the Ukiverse", ((TextPart)chatItem.Message[1]).Text);
        Assert.AreEqual("!", ((TextPart)chatItem.Message[2]).Text);
    }

    [TestMethod]
    public void ToChatItem_NewMemberLocalizedThreeRuns_ParsesTierFromSecondRun()
    {
        string rendererContentJson = MembershipTestData.NewMemberLocalizedThreeRuns();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatMembershipItemRenderer"
        );

        Assert.IsNotNull(chatItem, "ChatItem should not be null for localized membership schema.");
        Assert.IsNotNull(chatItem.MembershipDetails, "MembershipDetails should not be null.");
        Assert.AreEqual(MembershipEventType.New, chatItem.MembershipDetails.EventType);
        Assert.AreEqual("ミトメイトぷち", chatItem.MembershipDetails.LevelName);
        Assert.AreEqual("New member", chatItem.MembershipDetails.MembershipBadgeLabel);
        Assert.AreEqual("ようこそ ミトメイトぷち！", chatItem.MembershipDetails.HeaderSubtext);
    }

    [TestMethod]
    public void ToChatItem_NewMemberFromLog6_WithTenureBadge_ParsesTierFromWelcomeRuns()
    {
        string rendererContentJson = MembershipTestData.NewMemberFromLog6WithMemberTenureBadge();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatMembershipItemRenderer"
        );

        Assert.IsNotNull(chatItem);
        Assert.IsNotNull(chatItem.MembershipDetails);
        Assert.AreEqual(MembershipEventType.New, chatItem.MembershipDetails.EventType);
        Assert.AreEqual("ヘルエスタ王国民シップ", chatItem.MembershipDetails.LevelName);
        Assert.AreEqual("Member (1 year)", chatItem.MembershipDetails.MembershipBadgeLabel);
        Assert.AreEqual("Welcome to ヘルエスタ王国民シップ!", chatItem.MembershipDetails.HeaderSubtext);
        Assert.IsNotNull(chatItem.Author.Badge);
        Assert.AreEqual("Member (1 year)", chatItem.Author.Badge.Label);
    }

    [TestMethod]
    public void ToChatItem_MembershipMilestone27Months_ParsesCorrectly()
    {
        string rendererContentJson = MembershipTestData.MembershipMilestone27Months();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatMembershipItemRenderer"
        );

        Assert.IsNotNull(chatItem, "ChatItem should not be null for MembershipMilestone27Months.");
        Assert.AreEqual("MILESTONE_ID_27M", chatItem.Id);
        Assert.AreEqual("MilestoneUser27", chatItem.Author.Name);
        Assert.AreEqual("UC_CHANNEL_ID_MILESTONE_27M", chatItem.Author.ChannelId);
        Assert.IsNotNull(chatItem.MembershipDetails, "MembershipDetails should not be null.");
        Assert.AreEqual(MembershipEventType.Milestone, chatItem.MembershipDetails.EventType);
        Assert.AreEqual(
            "The Fam",
            chatItem.MembershipDetails.LevelName,
            "Milestone tier/level name should be parsed from HeaderSubtext."
        );
        Assert.AreEqual("Member (2 years)", chatItem.MembershipDetails.MembershipBadgeLabel);
        Assert.AreEqual(
            "The Fam",
            chatItem.MembershipDetails.HeaderSubtext,
            "HeaderSubtext incorrect."
        );
        Assert.AreEqual(
            "Member for 27 months",
            chatItem.MembershipDetails.HeaderPrimaryText,
            "HeaderPrimaryText incorrect."
        );
        Assert.AreEqual(
            27,
            chatItem.MembershipDetails.MilestoneMonths,
            "MilestoneMonths incorrect."
        );
        Assert.IsNotNull(chatItem.Author.Badge, "Author badge should not be null.");
        Assert.AreEqual(
            "Member (2 years)",
            chatItem.Author.Badge.Label,
            "Author badge label incorrect."
        );
        Assert.IsNotNull(
            chatItem.Author.Badge.Thumbnail,
            "Author badge thumbnail should not be null."
        );
        Assert.AreEqual(
            "https://yt3.ggpht.com/placeholder/badge_2yr_s32.png",
            chatItem.Author.Badge.Thumbnail.Url,
            "Author badge thumbnail URL incorrect."
        );
        Assert.IsTrue(chatItem.IsMembership, "IsMembership flag should be true.");
        Assert.AreEqual(
            1,
            chatItem.Message.Length,
            "Should have one message run for the user's comment."
        );
        _ = Assert.IsInstanceOfType<TextPart>(chatItem.Message[0], "Message part should be TextPart.");
        Assert.AreEqual(
            "YOOOOOO hope all is a well my man",
            ((TextPart)chatItem.Message[0]).Text,
            "User comment text incorrect."
        );
    }

    [TestMethod]
    public void ToChatItem_MembershipMilestone9Months_ParsesCorrectly()
    {
        string rendererContentJson = MembershipTestData.MembershipMilestone9Months();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatMembershipItemRenderer"
        );

        Assert.IsNotNull(chatItem, "ChatItem should not be null for MembershipMilestone9Months.");
        Assert.AreEqual("MILESTONE_ID_9M", chatItem.Id);
        Assert.AreEqual("MilestoneUser9", chatItem.Author.Name);
        Assert.AreEqual("UC_CHANNEL_ID_MILESTONE_9M", chatItem.Author.ChannelId);
        Assert.IsNotNull(chatItem.MembershipDetails);
        Assert.AreEqual(MembershipEventType.Milestone, chatItem.MembershipDetails.EventType);
        Assert.AreEqual("The Fam", chatItem.MembershipDetails.LevelName);
        Assert.AreEqual("Member (6 months)", chatItem.MembershipDetails.MembershipBadgeLabel);
        Assert.AreEqual("The Fam", chatItem.MembershipDetails.HeaderSubtext);
        Assert.AreEqual("Member for 9 months", chatItem.MembershipDetails.HeaderPrimaryText);
        Assert.AreEqual(9, chatItem.MembershipDetails.MilestoneMonths);
        Assert.IsNotNull(chatItem.Author.Badge);
        Assert.AreEqual("Member (6 months)", chatItem.Author.Badge.Label);
        Assert.IsTrue(chatItem.IsMembership);
        Assert.AreEqual(1, chatItem.Message.Length);
        _ = Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
        Assert.AreEqual("missed a bit, what's going on?", ((TextPart)chatItem.Message[0]).Text);
    }

    [TestMethod]
    public void ToChatItem_GiftPurchase_1_Gift_Kelly_ParsesCorrectly()
    {
        string rendererContentJson = MembershipTestData.GiftPurchase_1_Gift_Kelly();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatSponsorshipsGiftPurchaseAnnouncementRenderer"
        );

        Assert.IsNotNull(chatItem, "ChatItem should not be null for GiftPurchase_1_Gift_Kelly.");
        Assert.AreEqual("GIFT_PURCHASE_ID_KELLY", chatItem.Id);
        Assert.AreEqual("KellyTheGifter", chatItem.Author.Name);
        Assert.AreEqual("UC_CHANNEL_ID_GIFTER_KELLY", chatItem.Author.ChannelId);
        Assert.IsNotNull(chatItem.MembershipDetails, "MembershipDetails should not be null.");
        Assert.AreEqual(MembershipEventType.GiftPurchase, chatItem.MembershipDetails.EventType);

        // TODO: Defaults to "Member" because we look for the gift purchaser Member Level which doesn't get populated, we should change this to the gifted membership levels.
        // Assert.AreEqual("RaidAway+", chatItem.MembershipDetails.LevelName);

        Assert.AreEqual(1, chatItem.MembershipDetails.GiftCount);
        Assert.AreEqual("KellyTheGifter", chatItem.MembershipDetails.GifterUsername);
        Assert.IsTrue(chatItem.IsMembership, "IsMembership for gifter should be true.");
        Assert.IsNull(
            chatItem.Author.Badge,
            "Gifter Kelly should not have a badge in this specific example."
        );
    }

    [TestMethod]
    public void ToChatItem_GiftPurchase_20_Gifts_WhittBud_Mod_ParsesCorrectly()
    {
        string rendererContentJson = MembershipTestData.GiftPurchase_20_Gifts_WhittBud_Mod();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatSponsorshipsGiftPurchaseAnnouncementRenderer"
        );

        Assert.IsNotNull(
            chatItem,
            "ChatItem should not be null for GiftPurchase_20_Gifts_WhittBud_Mod."
        );
        Assert.AreEqual("GIFT_PURCHASE_ID_WHITT", chatItem.Id);
        Assert.AreEqual("WhittTheModGifter", chatItem.Author.Name);
        Assert.IsTrue(chatItem.IsModerator, "Gifter WhittBud should be marked as moderator.");
        Assert.IsNotNull(chatItem.Author.Badge, "Gifter WhittBud's member badge should be parsed.");
        Assert.AreEqual("Member (2 months)", chatItem.Author.Badge.Label); // Based on test data
        Assert.IsNotNull(chatItem.MembershipDetails);
        Assert.AreEqual(MembershipEventType.GiftPurchase, chatItem.MembershipDetails.EventType);

        // TODO: Defaults to "Member" because we look for the gift purchaser Member Level which doesn't get populated, we should change this to the gifted membership levels.
        // Assert.AreEqual("RaidAway+", chatItem.MembershipDetails.LevelName);

        Assert.AreEqual(20, chatItem.MembershipDetails.GiftCount);
        Assert.AreEqual("WhittTheModGifter", chatItem.MembershipDetails.GifterUsername);
        Assert.IsTrue(chatItem.IsMembership);
    }

    [TestMethod]
    public void ToChatItem_GiftPurchase_5_Gifts_JanaBeh_ParsesCorrectly()
    {
        string rendererContentJson = MembershipTestData.GiftPurchase_5_Gifts_JanaBeh();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatSponsorshipsGiftPurchaseAnnouncementRenderer"
        );

        Assert.IsNotNull(chatItem, "ChatItem should not be null for GiftPurchase_5_Gifts_JanaBeh.");
        Assert.AreEqual("GIFT_PURCHASE_ID_JANA", chatItem.Id);
        Assert.AreEqual("JanaTheGifter", chatItem.Author.Name);
        Assert.AreEqual("UC_CHANNEL_ID_GIFTER_JANA", chatItem.Author.ChannelId);
        Assert.IsNotNull(chatItem.MembershipDetails, "MembershipDetails should not be null.");
        Assert.AreEqual(MembershipEventType.GiftPurchase, chatItem.MembershipDetails.EventType);

        // TODO: Defaults to "Member" because we look for the gift purchaser Member Level which doesn't get populated, we should change this to the gifted membership levels.
        // Assert.AreEqual("RaidAway+", chatItem.MembershipDetails.LevelName);

        Assert.AreEqual(5, chatItem.MembershipDetails.GiftCount);
        Assert.AreEqual("JanaTheGifter", chatItem.MembershipDetails.GifterUsername);
        Assert.IsTrue(chatItem.IsMembership);
        Assert.IsNull(chatItem.Author.Badge);
    }

    [TestMethod]
    public void ToChatItem_GiftPurchase_50_Gifts_Derpickie_Mod_ParsesCorrectly()
    {
        string rendererContentJson = MembershipTestData.GiftPurchase_50_Gifts_Derpickie_Mod();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatSponsorshipsGiftPurchaseAnnouncementRenderer"
        );

        Assert.IsNotNull(chatItem);
        Assert.AreEqual("GIFT_PURCHASE_ID_DERPICKIE", chatItem.Id);
        Assert.AreEqual("DerpickieTheModGifter", chatItem.Author.Name);
        Assert.IsTrue(chatItem.IsModerator);
        Assert.IsNotNull(chatItem.Author.Badge);
        Assert.AreEqual("Member (6 months)", chatItem.Author.Badge.Label);
        Assert.IsNotNull(chatItem.MembershipDetails);
        Assert.AreEqual(MembershipEventType.GiftPurchase, chatItem.MembershipDetails.EventType);

        // TODO: Defaults to "Member" because we look for the gift purchaser Member Level which doesn't get populated, we should change this to the gifted membership levels.
        // Assert.AreEqual("RaidAway+", chatItem.MembershipDetails.LevelName);

        Assert.AreEqual(50, chatItem.MembershipDetails.GiftCount);
        Assert.IsTrue(chatItem.IsMembership);
    }

    [TestMethod]
    public void ToChatItem_GiftRedemption_FromKelly_ToHikari_ParsesCorrectly()
    {
        string rendererContentJson = MembershipTestData.GiftRedemption_FromKelly_ToHikari();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatSponsorshipsGiftRedemptionAnnouncementRenderer"
        );

        Assert.IsNotNull(
            chatItem,
            "ChatItem should not be null for GiftRedemption_FromKelly_ToHikari."
        );
        Assert.AreEqual("GIFT_REDEMPTION_ID_HIKARI", chatItem.Id);
        Assert.AreEqual("HikariTheRecipient", chatItem.Author.Name);
        Assert.AreEqual("UC_CHANNEL_ID_RECIPIENT_HIKARI", chatItem.Author.ChannelId);
        Assert.IsNotNull(chatItem.MembershipDetails, "MembershipDetails should not be null.");
        Assert.AreEqual(MembershipEventType.GiftRedemption, chatItem.MembershipDetails.EventType);
        Assert.AreEqual("Member", chatItem.MembershipDetails.LevelName);
        Assert.AreEqual("Kelly Lewis", chatItem.MembershipDetails.GifterUsername);
        Assert.AreEqual("HikariTheRecipient", chatItem.MembershipDetails.RecipientUsername);
        Assert.IsTrue(chatItem.IsMembership);

        // TODO: This doesn't get populated in the message part, data should be in HeaderSubtext or HeaderPrimaryText
        //Assert.AreEqual(2, chatItem.Message.Length);
        //Assert.AreEqual(
        //    "received a gift membership by Kelly Lewis",
        //    Parser.ToSimpleString(chatItem.Message)
        //);
    }

    [TestMethod]
    public void ToChatItem_GiftRedemption_FromJana_ToJackalope_ParsesCorrectly()
    {
        string rendererContentJson = MembershipTestData.GiftRedemption_FromJana_ToJackalope();
        ChatItem? chatItem = ParseRendererContentToChatItem(
            rendererContentJson,
            "liveChatSponsorshipsGiftRedemptionAnnouncementRenderer"
        );

        Assert.IsNotNull(
            chatItem,
            "ChatItem should not be null for GiftRedemption_FromJana_ToJackalope."
        );
        Assert.AreEqual("GIFT_REDEMPTION_ID_JACKALOPE", chatItem.Id);
        Assert.AreEqual("JackalopeTheRecipient", chatItem.Author.Name);
        Assert.IsNotNull(chatItem.MembershipDetails, "MembershipDetails should not be null.");
        Assert.AreEqual(MembershipEventType.GiftRedemption, chatItem.MembershipDetails.EventType);
        Assert.AreEqual("Member", chatItem.MembershipDetails.LevelName);
        Assert.AreEqual("Jana Beh", chatItem.MembershipDetails.GifterUsername);
        Assert.IsTrue(chatItem.IsMembership);
    }

    [TestMethod]
    public void ToChatItem_TickerPaidMessageFromLog8_ParsesAsTickerSuperchat()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            RawActionTestData.TickerPaidMessageFromLog8(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        ChatItem? chatItem = action.ToChatItem();
        Assert.IsNotNull(chatItem);
        Assert.IsTrue(chatItem.IsTicker);
        Assert.AreEqual("ChwKGkNMLWt4ZFRuM1pJREZiN0t3Z1Fkc3N3VFlR", chatItem.Id);
        Assert.AreEqual("@すずむら337", chatItem.Author.Name);
        Assert.IsNotNull(chatItem.Superchat);
        Assert.AreEqual("¥500", chatItem.Superchat.AmountString);
        Assert.AreEqual(500M, chatItem.Superchat.AmountValue);
        Assert.AreEqual("JPY", chatItem.Superchat.Currency);
        Assert.AreEqual(3, chatItem.Message.Length);
    }

    [TestMethod]
    public void ToChatItem_TickerSponsorFromLog8_ParsesAsTickerMembership()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            RawActionTestData.TickerSponsorItemFromLog8(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        ChatItem? chatItem = action.ToChatItem();
        Assert.IsNotNull(chatItem);
        Assert.IsTrue(chatItem.IsTicker);
        Assert.IsNotNull(chatItem.MembershipDetails);
        Assert.AreEqual(MembershipEventType.Milestone, chatItem.MembershipDetails.EventType);
        Assert.AreEqual(7, chatItem.MembershipDetails.MilestoneMonths);
        Assert.AreEqual("Member (6 months)", chatItem.MembershipDetails.MembershipBadgeLabel);
        Assert.AreEqual("@mutukisegawa3526", chatItem.Author.Name);
        Assert.AreEqual("可愛すぎて止まった心臓動き出した", ((TextPart)chatItem.Message[0]).Text);
    }

    [TestMethod]
    public void ToChatItem_TickerGiftPurchaseFromLog9To15_ParsesAsTickerMembershipGiftPurchase()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            RawActionLog9To15TestData.TickerGiftPurchaseFromLog(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        ChatItem? chatItem = action.ToChatItem();
        Assert.IsNotNull(chatItem);
        Assert.IsTrue(chatItem.IsTicker);
        Assert.IsNotNull(chatItem.MembershipDetails);
        Assert.AreEqual(MembershipEventType.GiftPurchase, chatItem.MembershipDetails.EventType);
        Assert.AreEqual(10, chatItem.MembershipDetails.GiftCount);
        Assert.AreEqual("@林宏儒-r3b", chatItem.Author.Name);
        Assert.AreEqual("@林宏儒-r3b", chatItem.MembershipDetails.GifterUsername);
    }

    [TestMethod]
    public void ToChatItem_NewMembershipWelcomeFromLog11_ParsesNewEventAndTierFromHeaderRuns()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            RawActionLog9To15TestData.NewMembershipWelcomeFromLog11(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        ChatItem? chatItem = action.ToChatItem();
        Assert.IsNotNull(chatItem);
        Assert.IsNotNull(chatItem.MembershipDetails);
        Assert.AreEqual(MembershipEventType.New, chatItem.MembershipDetails.EventType);
        Assert.AreEqual("開拓者組合", chatItem.MembershipDetails.LevelName);
        Assert.AreEqual("Member (2 months)", chatItem.MembershipDetails.MembershipBadgeLabel);
        Assert.AreEqual(3, chatItem.Message.Length);
    }

    // --- ParseLiveChatResponse Tests ---
    [TestMethod]
    public void ParseLiveChatResponse_SingleItem_ParsesCorrectly()
    {
        string rendererContent = TextMessageTestData.SimpleTextMessage1();
        string itemObjectJson = $$"""{ "liveChatTextMessageRenderer": {{rendererContent}} }""";
        string fullResponseJson = UtilityTestData.WrapItemsInLiveChatResponse(
            [itemObjectJson],
            "NEXT_CONT_SINGLE_PARSE"
        );

        LiveChatResponse? liveChatResponse = JsonSerializer.Deserialize<LiveChatResponse>(
            fullResponseJson,
            s_jsonOptions
        );
        Assert.IsNotNull(liveChatResponse, "LiveChatResponse deserialization failed.");

        (List<ChatItem> items, string? continuation) = Parser.ParseLiveChatResponse(
            liveChatResponse
        );

        Assert.AreEqual(1, items.Count, "Should have parsed one item.");
        Assert.AreEqual("NEXT_CONT_SINGLE_PARSE", continuation);
        Assert.IsNotNull(items[0], "Parsed ChatItem should not be null.");
        Assert.AreEqual("MSG_ID_SIMPLE_01", items[0].Id);
    }

    [TestMethod]
    public void ParseLiveChatResponse_MultipleItems_ParsesCorrectly()
    {
        string renderer1Content = TextMessageTestData.SimpleTextMessage1();
        string item1Json = $$"""{ "liveChatTextMessageRenderer": {{renderer1Content}} }""";

        string renderer2Content = TextMessageTestData.TextMessageWithStandardEmoji();
        string item2Json = $$"""{ "liveChatTextMessageRenderer": {{renderer2Content}} }""";

        string[] itemObjectJsons = [item1Json, item2Json];
        string fullResponseJson = UtilityTestData.WrapItemsInLiveChatResponse(
            itemObjectJsons,
            "NEXT_CONT_MULTI_PARSE"
        );

        LiveChatResponse? liveChatResponse = JsonSerializer.Deserialize<LiveChatResponse>(
            fullResponseJson,
            s_jsonOptions
        );
        Assert.IsNotNull(liveChatResponse, "LiveChatResponse deserialization failed.");

        (List<ChatItem> items, string? continuation) = Parser.ParseLiveChatResponse(
            liveChatResponse
        );

        Assert.AreEqual(2, items.Count, "Should have parsed two items.");
        Assert.AreEqual("NEXT_CONT_MULTI_PARSE", continuation);
        Assert.IsNotNull(items[0], "First parsed ChatItem should not be null.");
        Assert.AreEqual("MSG_ID_SIMPLE_01", items[0].Id);
        Assert.IsNotNull(items[1], "Second parsed ChatItem should not be null.");
        Assert.AreEqual("MSG_ID_STD_EMOJI_01", items[1].Id);
    }

    [TestMethod]
    public void ParseLiveChatResponse_NoContinuation_ReturnsNullContinuation()
    {
        string rendererContent = TextMessageTestData.SimpleTextMessage1();
        string itemObjectJson = $$"""{ "liveChatTextMessageRenderer": {{rendererContent}} }""";
        string fullResponseJson = UtilityTestData.StreamEndedResponse(itemObjectJson);

        LiveChatResponse? liveChatResponse = JsonSerializer.Deserialize<LiveChatResponse>(
            fullResponseJson,
            s_jsonOptions
        );
        Assert.IsNotNull(liveChatResponse, "LiveChatResponse deserialization failed.");

        (List<ChatItem> items, string? continuation) = Parser.ParseLiveChatResponse(
            liveChatResponse
        );

        Assert.AreEqual(1, items.Count, "Should have parsed one item.");
        Assert.IsNull(continuation);
        Assert.IsNotNull(items[0], "Parsed ChatItem should not be null.");
    }

    [TestMethod]
    public void ParseLiveChatResponse_EmptyActions_ReturnsEmptyListAndContinuation()
    {
        string fullResponseJson = UtilityTestData.WrapItemsInLiveChatResponse(
            [],
            "EMPTY_ACTIONS_CONT"
        );
        LiveChatResponse? liveChatResponse = JsonSerializer.Deserialize<LiveChatResponse>(
            fullResponseJson,
            s_jsonOptions
        );
        Assert.IsNotNull(liveChatResponse);

        (List<ChatItem> items, string? continuation) = Parser.ParseLiveChatResponse(
            liveChatResponse
        );

        Assert.AreEqual(0, items.Count);
        Assert.AreEqual("EMPTY_ACTIONS_CONT", continuation);
    }

    [TestMethod]
    public void ParseLiveChatResponse_ViewerEngagementAction_IgnoredAndContinuationPreserved()
    {
        string fullResponseJson = UtilityTestData.WrapActionsInLiveChatResponse(
            [ActionTestData.ViewerEngagementSubscribersOnly()],
            "CONT_VIEWER_ENGAGEMENT"
        );
        LiveChatResponse? liveChatResponse = JsonSerializer.Deserialize<LiveChatResponse>(
            fullResponseJson,
            s_jsonOptions
        );
        Assert.IsNotNull(liveChatResponse);

        (List<ChatItem> items, string? continuation) = Parser.ParseLiveChatResponse(
            liveChatResponse
        );

        Assert.AreEqual(0, items.Count, "Viewer engagement message should not map to ChatItem.");
        Assert.AreEqual("CONT_VIEWER_ENGAGEMENT", continuation);
    }

    [TestMethod]
    public void ParseLiveChatResponse_PollActionsAndBanner_IgnoredAndContinuationPreserved()
    {
        string fullResponseJson = UtilityTestData.WrapActionsInLiveChatResponse(
            [
                RawActionLog9To15TestData.ShowLiveChatActionPanelActionFromLog(),
                RawActionLog9To15TestData.UpdateLiveChatPollActionFromLog(),
                RawActionLog9To15TestData.AddBannerToLiveChatCommandFromLog(),
            ],
            "CONT_POLL_AND_BANNER"
        );
        LiveChatResponse? liveChatResponse = JsonSerializer.Deserialize<LiveChatResponse>(
            fullResponseJson,
            s_jsonOptions
        );
        Assert.IsNotNull(liveChatResponse);

        (List<ChatItem> items, string? continuation) = Parser.ParseLiveChatResponse(
            liveChatResponse
        );

        Assert.AreEqual(
            0,
            items.Count,
            "Poll update/action-panel and banner command should not map to ChatItem."
        );
        Assert.AreEqual("CONT_POLL_AND_BANNER", continuation);
    }

    [TestMethod]
    public void ParseLiveChatResponse_MixedKnownAndUnknownActions_ParsesOnlyKnownChatItems()
    {
        string textItemJson =
            $$"""{ "liveChatTextMessageRenderer": {{TextMessageTestData.SimpleTextMessage1()}} }""";

        string addTextActionJson = $$"""
            {
              "addChatItemAction": {
                "item": {{textItemJson}},
                "clientId": "CLIENT_ID_MIXED_ACTIONS_01"
              }
            }
            """;

        string fullResponseJson = UtilityTestData.WrapActionsInLiveChatResponse(
            [
                ActionTestData.AddBannerPinnedMessage(),
                addTextActionJson,
                ActionTestData.RemoveChatItem(),
                ActionTestData.ViewerEngagementSubscribersOnly(),
                ActionTestData.ModeChangeMessageRenderer(),
                ActionTestData.PlaceholderItemRenderer(),
                ActionTestData.ReportModerationStateEmpty(),
            ],
            "CONT_MIXED_ACTIONS"
        );

        LiveChatResponse? liveChatResponse = JsonSerializer.Deserialize<LiveChatResponse>(
            fullResponseJson,
            s_jsonOptions
        );
        Assert.IsNotNull(liveChatResponse);

        (List<ChatItem> items, string? continuation) = Parser.ParseLiveChatResponse(
            liveChatResponse
        );

        Assert.AreEqual(1, items.Count, "Only the text message action should map to ChatItem.");
        Assert.AreEqual("MSG_ID_SIMPLE_01", items[0].Id);
        Assert.AreEqual("CONT_MIXED_ACTIONS", continuation);
    }

    [TestMethod]
    public void ParseLiveChatResponse_NullResponse_ReturnsEmptyListAndNullContinuation()
    {
        (List<ChatItem> items, string? continuation) = Parser.ParseLiveChatResponse(null);
        Assert.AreEqual(0, items.Count);
        Assert.IsNull(continuation);
    }

    // --- GetOptionsFromLivePage Tests ---
    [TestMethod]
    public void GetOptionsFromLivePage_ValidHtml_ReturnsOptions()
    {
        string html = UtilityTestData.GetSampleLivePageHtml(
            "LIVETEST001",
            "API_KEY_PAGE_TEST",
            "CLIENT_VERSION_PAGE_TEST",
            "CONTINUATION_PAGE_TEST"
        );
        Models.FetchOptions options = Parser.GetOptionsFromLivePage(html);

        Assert.AreEqual("LIVETEST001", options.LiveId);
        Assert.AreEqual("API_KEY_PAGE_TEST", options.ApiKey);
        Assert.AreEqual("CLIENT_VERSION_PAGE_TEST", options.ClientVersion);
        Assert.AreEqual("CONTINUATION_PAGE_TEST", options.Continuation);
    }

    [TestMethod]
    public void GetOptionsFromLivePage_FinishedStreamHtml_ThrowsException()
    {
        string html = UtilityTestData.GetFinishedStreamPageHtml("FINISHED001");
        Exception ex = Assert.Throws<Exception>(() => Parser.GetOptionsFromLivePage(html));
        Assert.IsTrue(ex.Message.Contains("is finished live"));
        Assert.IsTrue(ex.Message.Contains("FINISHED001"));
    }

    [TestMethod]
    public void GetOptionsFromLivePage_MissingApiKey_ThrowsException()
    {
        string html = UtilityTestData
            .GetSampleLivePageHtml("NOKEYLIVE01", " ", "CLIENT_V", "CONT")
            .Replace("\"INNERTUBE_API_KEY\": \" \",", "");
        Exception ex = Assert.Throws<Exception>(() => Parser.GetOptionsFromLivePage(html));
        Assert.IsTrue(ex.Message.Contains("API Key (INNERTUBE_API_KEY) not found"));
    }

    [TestMethod]
    public void GetOptionsFromLivePage_MissingClientVersion_ThrowsException()
    {
        string html = UtilityTestData
            .GetSampleLivePageHtml("NOCVLIVE001", "API_K", " ", "CONT")
            .Replace("\"INNERTUBE_CONTEXT_CLIENT_VERSION\": \" \",", "");
        Exception ex = Assert.Throws<Exception>(() => Parser.GetOptionsFromLivePage(html));
        Assert.IsTrue(
            ex.Message.Contains("Client Version (INNERTUBE_CONTEXT_CLIENT_VERSION) not found")
        );
    }

    [TestMethod]
    public void GetOptionsFromLivePage_MissingContinuation_ThrowsException()
    {
        string html = UtilityTestData
            .GetSampleLivePageHtml("NOCONTLV001", "API_K", "CLIENT_V", " ")
            .Replace(
                "\"reloadContinuationData\": { \"continuation\": \" \" }",
                "\"reloadContinuationData\": { }"
            );
        Exception ex = Assert.Throws<Exception>(() => Parser.GetOptionsFromLivePage(html));
        Assert.IsTrue(ex.Message.Contains("Initial Continuation token not found"));
    }

    [TestMethod]
    public void GetOptionsFromLivePage_MissingLiveIdCanonical_ThrowsException()
    {
        string html = UtilityTestData
            .GetSampleLivePageHtml(" ", "API_K", "CLIENT_V", "CONT")
            .Replace("<link rel=\"canonical\" href=\"https://www.youtube.com/watch?v= \">", "");
        Exception ex = Assert.Throws<Exception>(() => Parser.GetOptionsFromLivePage(html));
        Assert.IsTrue(ex.Message.Contains("Live Stream ID not found"));
    }

    [TestMethod]
    public void GetOptionsFromLivePage_MissingCanonical_UsesCanonicalBaseUrlFallback()
    {
        string liveId = "AbCdEfGhI12";
        string html =
            """
            <html><head>
            <script>
            var cfg = {
              "INNERTUBE_API_KEY":"API_KEY_X",
              "INNERTUBE_CONTEXT_CLIENT_VERSION":"CLIENT_VERSION_X",
              "canonicalBaseUrl":"\/watch?v=AbCdEfGhI12",
              "continuation":"CONT_X"
            };
            </script>
            </head><body></body></html>
            """;

        Models.FetchOptions options = Parser.GetOptionsFromLivePage(html);
        Assert.AreEqual(liveId, options.LiveId);
        Assert.AreEqual("API_KEY_X", options.ApiKey);
        Assert.AreEqual("CLIENT_VERSION_X", options.ClientVersion);
        Assert.AreEqual("CONT_X", options.Continuation);
    }

    [TestMethod]
    public void GetOptionsFromLivePage_MissingCanonical_UsesChatTopicFallback()
    {
        string liveId = "ZyXwVuTsRq9";
        string html =
            """
            <html><head>
            <script>
            var cfg = {
              "INNERTUBE_API_KEY":"API_KEY_Y",
              "INNERTUBE_CONTEXT_CLIENT_VERSION":"CLIENT_VERSION_Y",
              "topic":"chat~ZyXwVuTsRq9",
              "continuation":"CONT_Y"
            };
            </script>
            </head><body></body></html>
            """;

        Models.FetchOptions options = Parser.GetOptionsFromLivePage(html);
        Assert.AreEqual(liveId, options.LiveId);
        Assert.AreEqual("API_KEY_Y", options.ApiKey);
        Assert.AreEqual("CLIENT_VERSION_Y", options.ClientVersion);
        Assert.AreEqual("CONT_Y", options.Continuation);
    }

    [TestMethod]
    public void GetOptionsFromLivePage_HakosSnapshot_ParsesExpectedValues()
    {
        string html = WebSnapshotTestData.HakosLivePageSnapshot();
        Models.FetchOptions options = Parser.GetOptionsFromLivePage(html);

        Assert.AreEqual("oPOBYMu2zk8", options.LiveId);
        Assert.AreEqual("AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8", options.ApiKey);
        Assert.AreEqual("2.20260213.01.00", options.ClientVersion);
        Assert.AreEqual(
            "0ofMyAOAARpeQ2lrcUp3b1lWVU5uYlZCdWVDMUZSV1ZQY2xwVFp6VlVhWGMzV2xKUkVndHZVRTlDV1UxMU1ucHJPQm",
            options.Continuation
        );
    }

    [TestMethod]
    public void IsActivelyBroadcastingLivePage_HakosSnapshot_ReturnsFalse()
    {
        bool isActive = Parser.IsActivelyBroadcastingLivePage(
            WebSnapshotTestData.HakosLivePageSnapshot()
        );
        Assert.IsFalse(isActive);
    }

    [TestMethod]
    public void IsActivelyBroadcastingLivePage_BijouSnapshot_ReturnsTrue()
    {
        bool isActive = Parser.IsActivelyBroadcastingLivePage(
            WebSnapshotTestData.BijouLivePageSnapshot()
        );
        Assert.IsTrue(isActive);
    }

    [TestMethod]
    public void ExtractStreamCandidatesFromStreamsPage_ReturnsLiveAndUpcomingSignals()
    {
        string html =
            """
            <html><body><script>
            {"videoRenderer":{"videoId":"AAAAAAAAAAA","thumbnailOverlayTimeStatusRenderer":{"style":"LIVE"},"shortViewCountText":{"simpleText":"1.2K watching"}}}
            {"videoRenderer":{"videoId":"BBBBBBBBBBB","thumbnailOverlayTimeStatusRenderer":{"style":"UPCOMING"},"upcomingEventData":{"startTime":"1771405200"},"shortViewCountText":{"simpleText":"29 waiting"}}}
            </script></body></html>
            """;

        IReadOnlyList<StreamPageCandidate> candidates = Parser.ExtractStreamCandidatesFromStreamsPage(
            html
        );

        Assert.AreEqual(2, candidates.Count);
        Assert.AreEqual("AAAAAAAAAAA", candidates[0].LiveId);
        Assert.IsTrue(candidates[0].IsLive);
        Assert.AreEqual("BBBBBBBBBBB", candidates[1].LiveId);
        Assert.IsTrue(candidates[1].IsUpcoming);
        Assert.AreEqual(1771405200L, candidates[1].UpcomingStartTime);
    }

    [TestMethod]
    public void ExtractStreamCandidatesFromStreamsPage_DeduplicatesByVideoId()
    {
        string html =
            """
            <html><body><script>
            {"videoRenderer":{"videoId":"CCCCCCCCCCC","thumbnailOverlayTimeStatusRenderer":{"style":"UPCOMING"},"upcomingEventData":{"startTime":"1771405200"}}}
            {"videoRenderer":{"videoId":"CCCCCCCCCCC","thumbnailOverlayTimeStatusRenderer":{"style":"LIVE"},"shortViewCountText":{"simpleText":"500 watching"}}}
            </script></body></html>
            """;

        IReadOnlyList<StreamPageCandidate> candidates = Parser.ExtractStreamCandidatesFromStreamsPage(
            html
        );

        Assert.AreEqual(1, candidates.Count);
        Assert.AreEqual("CCCCCCCCCCC", candidates[0].LiveId);
        Assert.IsTrue(candidates[0].IsLive);
        Assert.IsTrue(candidates[0].IsUpcoming);
    }

    [TestMethod]
    public void ExtractStreamCandidatesFromStreamsPage_RealSnapshots_ParsesLiveAndUpcoming()
    {
        IReadOnlyList<StreamPageCandidate> candidates = Parser.ExtractStreamCandidatesFromStreamsPage(
            WebSnapshotTestData.StreamsPageSnapshotFragments()
        );

        Assert.IsTrue(candidates.Any(c => c.LiveId == "17PFTNoO_RE" && c.IsLive));
        Assert.IsTrue(
            candidates.Any(c =>
                c.LiveId == "oPOBYMu2zk8"
                && c.IsUpcoming
                && c.UpcomingStartTime == 1771322400L
            )
        );
        Assert.IsTrue(
            candidates.Any(c =>
                c.LiveId == "hlDFczhR2mo"
                && c.IsUpcoming
                && c.UpcomingStartTime == 1788748200L
            )
        );
        Assert.IsTrue(
            candidates.Any(c =>
                c.LiveId == "197OEpjj8RI"
                && c.IsUpcoming
                && c.UpcomingStartTime == 1819720800L
            )
        );
    }

    [TestMethod]
    public void ExtractStreamCandidatesFromStreamsPage_FullLunaStreamsSnapshot_ParsesLiveAndUpcoming()
    {
        string html = LoadWebSnapshot("HimemoriLuna.streams.2026-02-17.html");
        IReadOnlyList<StreamPageCandidate> candidates = Parser.ExtractStreamCandidatesFromStreamsPage(
            html
        );

        Assert.IsTrue(candidates.Count > 0);
        Assert.IsTrue(candidates.Any(c => c.LiveId == "qT5OTDvJK1Q" && c.IsLive));
        Assert.IsTrue(
            candidates.Any(c =>
                c.LiveId == "wPXfKeWU2YE" && c.IsUpcoming && c.UpcomingStartTime == 1791644100L
            )
        );
    }

    [TestMethod]
    public void GetOptionsFromLivePage_FullLunaLiveSnapshot_ParsesExpectedValues()
    {
        string html = LoadWebSnapshot("HimemoriLuna.live.2026-02-17.html");
        Models.FetchOptions options = Parser.GetOptionsFromLivePage(html);

        Assert.AreEqual("qT5OTDvJK1Q", options.LiveId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(options.ApiKey));
        Assert.IsFalse(string.IsNullOrWhiteSpace(options.ClientVersion));
        Assert.IsFalse(string.IsNullOrWhiteSpace(options.Continuation));
    }

    [TestMethod]
    public void IsActivelyBroadcastingLivePage_FullLunaLiveSnapshot_ReturnsTrue()
    {
        string html = LoadWebSnapshot("HimemoriLuna.live.2026-02-17.html");
        bool isActive = Parser.IsActivelyBroadcastingLivePage(html);
        Assert.IsTrue(isActive);
    }

    [TestMethod]
    public void ExtractStreamCandidatesFromStreamsPage_FullAkiStreamsSnapshot_ParsesFreeChatAndUpcoming()
    {
        string html = LoadWebSnapshot("AkiRosenthal.streams.2026-02-17.html");
        IReadOnlyList<StreamPageCandidate> candidates = Parser.ExtractStreamCandidatesFromStreamsPage(
            html
        );

        Assert.IsTrue(candidates.Any(c =>
            c.LiveId == "VoWHIX4tp5k" && c.IsUpcoming && c.UpcomingStartTime == 1798123500L
        ));
        Assert.IsTrue(candidates.Any(c =>
            c.LiveId == "qS50yDHZOx4" && c.IsUpcoming && c.UpcomingStartTime == 1771332300L
        ));
        Assert.IsFalse(candidates.Any(c => c.IsLive));
    }

    [TestMethod]
    public void IsActivelyBroadcastingLivePage_FullAkiLiveSnapshot_ReturnsFalse()
    {
        string html = LoadWebSnapshot("AkiRosenthal.live.2026-02-17.html");
        bool isActive = Parser.IsActivelyBroadcastingLivePage(html);
        Assert.IsFalse(isActive);
    }

    [TestMethod]
    public void ExtractStreamCandidatesFromStreamsPage_FullIofiStreamsSnapshot_ParsesSingleUpcoming()
    {
        string html = LoadWebSnapshot("AiraniIofifteen.streams.2026-02-17.html");
        IReadOnlyList<StreamPageCandidate> candidates = Parser.ExtractStreamCandidatesFromStreamsPage(
            html
        );

        Assert.IsTrue(candidates.Any(c =>
            c.LiveId == "c2lb7tb1SEA" && c.IsUpcoming && c.UpcomingStartTime == 1771333200L
        ));
        Assert.IsFalse(candidates.Any(c => c.IsLive));
    }

    [TestMethod]
    public void IsActivelyBroadcastingLivePage_FullIofiLiveSnapshot_ReturnsFalse()
    {
        string html = LoadWebSnapshot("AiraniIofifteen.live.2026-02-17.html");
        bool isActive = Parser.IsActivelyBroadcastingLivePage(html);
        Assert.IsFalse(isActive);
    }

    [TestMethod]
    public void ExtractStreamCandidatesFromStreamsPage_AkiCurrentSnapshot_FindsLiveMembersOnlyAndUpcomingFreeChat()
    {
        string html = LoadWebSnapshot("AkiRosenthal.streams.2026-02-17.current.html");
        IReadOnlyList<StreamPageCandidate> candidates = Parser.ExtractStreamCandidatesFromStreamsPage(
            html
        );

        Assert.IsTrue(candidates.Any(c => c.LiveId == "qS50yDHZOx4" && c.IsLive));
        Assert.IsTrue(
            candidates.Any(c =>
                c.LiveId == "VoWHIX4tp5k" && c.IsUpcoming && c.UpcomingStartTime == 1798123500L
            )
        );
    }

    [TestMethod]
    public void DetectInaccessibleLiveReason_MembersOnlyWatchSnapshot_ReturnsMembersOnly()
    {
        string html = LoadWebSnapshot("AkiRosenthal.memberlive.qS50yDHZOx4.2026-02-17.html");
        string? reason = Parser.DetectInaccessibleLiveReason(html);
        Assert.AreEqual("members-only", reason);
    }

    // ── Poll parsing ──────────────────────────────────────────────────────────

    [TestMethod]
    public void ToPollItem_UpdatePollActionWithVotes_ReturnsPollItemWithVoteData()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.UpdatePollActionWithVotes(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        PollItem? poll = Parser.ToPollItem(action);

        Assert.IsNotNull(poll, "ToPollItem should return a non-null PollItem.");
        Assert.AreEqual("POLL_ID_UPDATE_01", poll.PollId);
        Assert.IsFalse(poll.IsNew, "An updateLiveChatPollAction should not be marked IsNew.");
        Assert.AreEqual("@StreamerHandle", poll.CreatorHandle);
        Assert.AreEqual(1234, poll.TotalVotes);
        Assert.AreEqual(2, poll.Choices.Count);
        Assert.AreEqual("Option A", string.Concat(poll.Choices[0].Text.OfType<TextPart>().Select(p => p.Text)));
        Assert.AreEqual(0.28, poll.Choices[0].VoteRatio, 0.001);
        Assert.AreEqual("Option B", string.Concat(poll.Choices[1].Text.OfType<TextPart>().Select(p => p.Text)));
        Assert.AreEqual(0.72, poll.Choices[1].VoteRatio, 0.001);
    }

    [TestMethod]
    public void ToPollItem_ShowPanelActionNewPoll_ReturnsPollItemIsNew()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.ShowPanelActionNewPoll(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        PollItem? poll = Parser.ToPollItem(action);

        Assert.IsNotNull(poll, "ToPollItem should return a non-null PollItem.");
        Assert.AreEqual("ChwKGkNNdVFxNWpvNkpNREZaekh3Z1FkelJZTExR", poll.PollId);
        Assert.IsTrue(poll.IsNew, "A showLiveChatActionPanelAction should be marked IsNew.");
        Assert.AreEqual("@holoen_raorapanthera", poll.CreatorHandle);
        Assert.AreEqual(0, poll.TotalVotes);
        Assert.AreEqual(2, poll.Choices.Count);
        Assert.AreEqual("LET IN", string.Concat(poll.Choices[0].Text.OfType<TextPart>().Select(p => p.Text)));
        Assert.AreEqual("OUT", string.Concat(poll.Choices[1].Text.OfType<TextPart>().Select(p => p.Text)));
        // Fresh polls have no voteRatio (default 0.0 since the field is not present in JSON)
        Assert.AreEqual(0.0, poll.Choices[0].VoteRatio);
        Assert.AreEqual(0.0, poll.Choices[1].VoteRatio);
    }

    [TestMethod]
    public void ToPollItem_UpdatePollActionZeroVotes_ReturnsPollItemNotNew()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.UpdatePollActionZeroVotes(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        PollItem? poll = Parser.ToPollItem(action);

        Assert.IsNotNull(poll, "ToPollItem should return a non-null PollItem.");
        Assert.AreEqual("ChwKGkNNdVFxNWpvNkpNREZaekh3Z1FkelJZTExR", poll.PollId);
        Assert.IsFalse(poll.IsNew, "An updateLiveChatPollAction should not be marked IsNew.");
        Assert.AreEqual("@holoen_raorapanthera", poll.CreatorHandle);
        Assert.AreEqual(0, poll.TotalVotes);
        Assert.AreEqual(2, poll.Choices.Count);
        Assert.AreEqual("LET IN", string.Concat(poll.Choices[0].Text.OfType<TextPart>().Select(p => p.Text)));
        Assert.AreEqual(0.0, poll.Choices[0].VoteRatio, 0.001);
        Assert.AreEqual("OUT", string.Concat(poll.Choices[1].Text.OfType<TextPart>().Select(p => p.Text)));
        Assert.AreEqual(0.0, poll.Choices[1].VoteRatio, 0.001);
    }

    [TestMethod]
    public void ToPollItem_ShowPollAction_OuroKronii_IsNewWithQuestionAndZeroVotes()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.ShowPollAction_OuroKronii_WallVsFloor(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        PollItem? poll = Parser.ToPollItem(action);

        Assert.IsNotNull(poll);
        Assert.AreEqual("ChwKGkNKTzk4NUNiNzVNREZYWlFUQWdkT2U0VjVR", poll.PollId);
        Assert.IsTrue(poll.IsNew, "showLiveChatActionPanelAction should be marked IsNew.");
        Assert.IsNotNull(poll.Question);
        Assert.AreEqual("for the wood", string.Concat(poll.Question.OfType<TextPart>().Select(p => p.Text)));
        Assert.AreEqual("@OuroKronii", poll.CreatorHandle);
        Assert.AreEqual(0, poll.TotalVotes);
        Assert.AreEqual(2, poll.Choices.Count);
        Assert.AreEqual("wall", string.Concat(poll.Choices[0].Text.OfType<TextPart>().Select(p => p.Text)));
        Assert.AreEqual("floor", string.Concat(poll.Choices[1].Text.OfType<TextPart>().Select(p => p.Text)));
    }

    [TestMethod]
    public void ToPollItem_UpdatePollAction_OuroKronii_ZeroVotes_AllChoicesAtZeroRatio()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.UpdatePollAction_OuroKronii_ZeroVotes(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        PollItem? poll = Parser.ToPollItem(action);

        Assert.IsNotNull(poll);
        Assert.AreEqual("ChwKGkNKTzk4NUNiNzVNREZYWlFUQWdkT2U0VjVR", poll.PollId);
        Assert.IsFalse(poll.IsNew, "updateLiveChatPollAction should not be marked IsNew.");
        Assert.IsNotNull(poll.Question);
        Assert.AreEqual("for the wood", string.Concat(poll.Question.OfType<TextPart>().Select(p => p.Text)));
        Assert.AreEqual(0, poll.TotalVotes);
        Assert.AreEqual(0.0, poll.Choices[0].VoteRatio, 0.001);
        Assert.AreEqual(0.0, poll.Choices[1].VoteRatio, 0.001);
    }

    [TestMethod]
    public void ToPollItem_UpdatePollAction_OuroKronii_MidPoll_ParsesVoteRatiosAndTotalVotes()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.UpdatePollAction_OuroKronii_MidPoll_Wall45_Floor55(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        PollItem? poll = Parser.ToPollItem(action);

        Assert.IsNotNull(poll);
        Assert.AreEqual("ChwKGkNKTzk4NUNiNzVNREZYWlFUQWdkT2U0VjVR", poll.PollId);
        Assert.IsFalse(poll.IsNew);
        Assert.AreEqual(1301, poll.TotalVotes);
        Assert.AreEqual(0.4547, poll.Choices[0].VoteRatio, 0.001, "wall should be ~45%.");
        Assert.AreEqual(0.5452, poll.Choices[1].VoteRatio, 0.001, "floor should be ~55%.");
    }

    [TestMethod]
    public void ToPollItem_UpdatePollAction_OuroKronii_FinalResult_ParsesFinalVoteCounts()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.UpdatePollAction_OuroKronii_FinalResult_Wall47_Floor53(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        PollItem? poll = Parser.ToPollItem(action);

        Assert.IsNotNull(poll);
        Assert.AreEqual("ChwKGkNKTzk4NUNiNzVNREZYWlFUQWdkT2U0VjVR", poll.PollId);
        Assert.IsFalse(poll.IsNew);
        Assert.AreEqual(1972, poll.TotalVotes);
        Assert.AreEqual(0.4680, poll.Choices[0].VoteRatio, 0.001, "wall should be ~47%.");
        Assert.AreEqual(0.5319, poll.Choices[1].VoteRatio, 0.001, "floor should be ~53%.");
    }

    [TestMethod]
    public void ToPollItem_UnrelatedAction_ReturnsNull()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.RemoveChatItem(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);
        Assert.IsNull(Parser.ToPollItem(action));
    }

    // ── Message deletion ──────────────────────────────────────────────────────

    [TestMethod]
    public void ToDeletedItemId_RemoveChatItemAction_ReturnsTargetId()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.RemoveChatItem(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        string? targetId = Parser.ToDeletedItemId(action);

        Assert.AreEqual("REMOVED_MSG_ID_01", targetId);
    }

    [TestMethod]
    public void ToDeletedItemId_UnrelatedAction_ReturnsNull()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.AddBannerPinnedMessage(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);
        Assert.IsNull(Parser.ToDeletedItemId(action));
    }

    [TestMethod]
    public void ToDeletedByAuthorChannelId_RemoveChatItemByAuthorAction_ReturnsChannelId()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.RemoveChatItemByAuthor(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        string? channelId = Parser.ToDeletedByAuthorChannelId(action);

        Assert.AreEqual("UC_BANNED_CHANNEL_01", channelId);
    }

    // ── Banner parsing ────────────────────────────────────────────────────────

    [TestMethod]
    public void ToBannerItem_AddBannerPinnedMessage_ReturnsPinnedMessageBannerItem()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.AddBannerPinnedMessage(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        BannerItem? banner = Parser.ToBannerItem(action);

        Assert.IsNotNull(banner, "ToBannerItem should return a non-null BannerItem.");
        _ = Assert.IsInstanceOfType<PinnedMessageBannerItem>(banner, "Pinned message must be PinnedMessageBannerItem.");
        PinnedMessageBannerItem pinned = (PinnedMessageBannerItem)banner;

        Assert.AreEqual("PINNED_ACTION_ID_01", pinned.ActionId);
        Assert.AreEqual(BannerType.PinnedMessage, pinned.BannerType);
        Assert.AreEqual("Pinned by @Host", pinned.PinnedBy);
        Assert.AreEqual("@Host", pinned.Author.Name);
        Assert.AreEqual("UC_HOST_01", pinned.Author.ChannelId);
        Assert.AreEqual(1, pinned.Message.Length);
        _ = Assert.IsInstanceOfType<TextPart>(pinned.Message[0]);
        Assert.AreEqual("Pinned message body", ((TextPart)pinned.Message[0]).Text);
        Assert.AreEqual("PINNED_TEXT_ID_01", pinned.MessageId);
        // Timestamp from timestampUsec "1776004576422639"
        Assert.AreNotEqual(default, pinned.Timestamp);
        Assert.IsTrue(pinned.Timestamp.Year >= 2026, "Timestamp should be from 2026 or later.");
        // VERIFIED badge
        Assert.IsTrue(pinned.IsVerified, "Author should be marked verified.");
        Assert.IsFalse(pinned.IsModerator);
        Assert.IsFalse(pinned.IsOwner);
    }

    [TestMethod]
    public void ToBannerItem_RedirectBannerWithVideoId_ReturnsCrossChannelRedirectBannerItem()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.AddBannerRedirectCommand(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        BannerItem? banner = Parser.ToBannerItem(action);

        Assert.IsNotNull(banner, "ToBannerItem should return a non-null BannerItem for redirect.");
        _ = Assert.IsInstanceOfType<CrossChannelRedirectBannerItem>(banner, "Redirect must be CrossChannelRedirectBannerItem.");
        CrossChannelRedirectBannerItem redirect = (CrossChannelRedirectBannerItem)banner;

        Assert.AreEqual("ChwKGkNKLW1yNjd4NkpNREZhRE5GZ2tkVUFNWUNn", redirect.ActionId);
        Assert.AreEqual(BannerType.CrossChannelRedirect, redirect.BannerType);
        Assert.AreEqual(CrossChannelRedirectType.Redirect, redirect.RedirectType);
        Assert.AreEqual("@TakanashiKiara", redirect.RedirectChannelHandle);
        Assert.AreEqual("OcULALBAXRA", redirect.RedirectVideoId);
        Assert.IsNotNull(redirect.ChannelPhoto, "Redirect banner should have a ChannelPhoto.");
        Assert.IsTrue(redirect.BannerMessage.Length >= 2, "Redirect banner message should have multiple parts.");
    }

    [TestMethod]
    public void ToBannerItem_RedirectBannerLearnMore_ReturnsCrossChannelRedirectBannerItemWithNullVideoId()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.AddBannerRedirectLearnMore(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        BannerItem? banner = Parser.ToBannerItem(action);

        Assert.IsNotNull(banner, "ToBannerItem should return a non-null BannerItem for learn-more redirect.");
        _ = Assert.IsInstanceOfType<CrossChannelRedirectBannerItem>(banner, "Learn-more redirect must be CrossChannelRedirectBannerItem.");
        CrossChannelRedirectBannerItem redirect = (CrossChannelRedirectBannerItem)banner;

        Assert.AreEqual("ChwKGkNPNzM0NEdnNlpNREZUUFFsQWtkM25ZN3NR", redirect.ActionId);
        Assert.AreEqual(BannerType.CrossChannelRedirect, redirect.BannerType);
        Assert.AreEqual(CrossChannelRedirectType.Raid, redirect.RedirectType);
        Assert.AreEqual("@holoen_ceciliaimmergreen", redirect.RedirectChannelHandle);
        Assert.IsNull(redirect.RedirectVideoId, "Learn-more redirect should have no RedirectVideoId.");
        Assert.IsNotNull(redirect.ChannelPhoto, "Learn-more redirect banner should have a ChannelPhoto.");
    }

    [TestMethod]
    public void ToBannerItem_ChatSummaryBanner_ReturnsChatSummaryBannerItemWithStructuredParts()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.AddBannerChatSummary(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        BannerItem? banner = Parser.ToBannerItem(action);

        Assert.IsNotNull(banner);
        _ = Assert.IsInstanceOfType<ChatSummaryBannerItem>(banner);
        ChatSummaryBannerItem summary = (ChatSummaryBannerItem)banner;

        Assert.AreEqual("z4B7kTpSbWc_1776232797929176", summary.ActionId);
        Assert.AreEqual(BannerType.ChatSummary, summary.BannerType);
        Assert.AreEqual("z4B7kTpSbWc_1776232797929176", summary.SummaryId);

        // All 5 runs preserved as-is: [bold title, "\n", deemphasized disclaimer, "\n", body text].
        // Newline separators are kept so consumers can faithfully reproduce YouTube's layout.
        Assert.AreEqual(5, summary.Summary.Length, "All 5 chatSummary runs should be preserved.");

        // Part 0: bold title
        TextPart title = Assert.IsInstanceOfType<TextPart>(summary.Summary[0]);
        Assert.AreEqual("Chat summary", title.Text);
        Assert.IsTrue(title.Bold, "Title run should be bold.");
        Assert.IsFalse(title.IsDeemphasized);

        // Part 1: newline separator
        TextPart newline1 = Assert.IsInstanceOfType<TextPart>(summary.Summary[1]);
        Assert.AreEqual("\n", newline1.Text);

        // Part 2: deemphasized disclaimer
        TextPart disclaimer = Assert.IsInstanceOfType<TextPart>(summary.Summary[2]);
        StringAssert.Contains(disclaimer.Text, "Auto-generated");
        Assert.IsTrue(disclaimer.IsDeemphasized, "Disclaimer run should be deemphasized.");
        Assert.IsFalse(disclaimer.Bold);

        // Part 3: newline separator
        TextPart newline2 = Assert.IsInstanceOfType<TextPart>(summary.Summary[3]);
        Assert.AreEqual("\n", newline2.Text);

        // Part 4: body text — the actual AI-generated summary content
        TextPart body = Assert.IsInstanceOfType<TextPart>(summary.Summary[4]);
        StringAssert.Contains(body.Text, "happy birthday");
        Assert.IsFalse(body.Bold);
        Assert.IsFalse(body.IsDeemphasized);
    }

    [TestMethod]
    public void ToBannerItem_PinnedMessage_InugamiKorone_HasOwnerAndVerifiedBadgesWithEmojiMessage()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.AddBannerPinnedMessage_InugamiKorone(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        BannerItem? banner = Parser.ToBannerItem(action);

        Assert.IsNotNull(banner);
        _ = Assert.IsInstanceOfType<PinnedMessageBannerItem>(banner);
        PinnedMessageBannerItem pinned = (PinnedMessageBannerItem)banner;

        Assert.AreEqual("ChwKGkNMcTd1dlRYN1pNREZXcmV3Z1FkRndzSXJ3", pinned.ActionId);
        Assert.AreEqual(BannerType.PinnedMessage, pinned.BannerType);
        Assert.AreEqual("@InugamiKorone", pinned.Author.Name);
        Assert.AreEqual("Pinned by @InugamiKorone", pinned.PinnedBy);
        Assert.IsTrue(pinned.IsOwner, "Author should be flagged as OWNER.");
        Assert.IsTrue(pinned.IsVerified, "Author should be flagged as VERIFIED.");
        Assert.AreEqual(4, pinned.Message.Length, "Message should have 1 text + 3 emoji parts.");
        _ = Assert.IsInstanceOfType<TextPart>(pinned.Message[0], "First part should be a TextPart.");
        _ = Assert.IsInstanceOfType<EmojiPart>(pinned.Message[1], "Second part should be an EmojiPart.");
        _ = Assert.IsInstanceOfType<EmojiPart>(pinned.Message[2], "Third part should be an EmojiPart.");
        _ = Assert.IsInstanceOfType<EmojiPart>(pinned.Message[3], "Fourth part should be an EmojiPart.");
    }

    [TestMethod]
    public void ToBannerItem_RedirectLearnMore_KureijiOllie_ParsesHandleAndNullVideoId()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.AddBannerRedirectLearnMore_KureijiOllie(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        BannerItem? banner = Parser.ToBannerItem(action);

        Assert.IsNotNull(banner);
        _ = Assert.IsInstanceOfType<CrossChannelRedirectBannerItem>(banner);
        CrossChannelRedirectBannerItem redirect = (CrossChannelRedirectBannerItem)banner;

        Assert.AreEqual("ChwKGkNPdjN0b1g0N1pNREZmWENsQWtkaFVveU1B", redirect.ActionId);
        Assert.AreEqual(BannerType.CrossChannelRedirect, redirect.BannerType);
        Assert.AreEqual(CrossChannelRedirectType.Raid, redirect.RedirectType);
        Assert.AreEqual("@KureijiOllie", redirect.RedirectChannelHandle);
        Assert.IsNull(redirect.RedirectVideoId, "Learn-more variant should have no video ID.");
        Assert.IsNotNull(redirect.ChannelPhoto, "Should have a channel photo.");
        Assert.AreEqual(2, redirect.BannerMessage.Length, "Banner message should have 2 parts.");
    }

    [TestMethod]
    public void ToBannerItem_RedirectGoNow_UsadaPekora_ParsesRedirectTypeAndVideoId()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.AddBannerRedirectGoNow_UsadaPekora(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        BannerItem? banner = Parser.ToBannerItem(action);

        Assert.IsNotNull(banner);
        _ = Assert.IsInstanceOfType<CrossChannelRedirectBannerItem>(banner);
        CrossChannelRedirectBannerItem redirect = (CrossChannelRedirectBannerItem)banner;

        Assert.AreEqual("ChwKGkNMdk96OG55NzVNREZYYkNsQWtkNFVVRndB", redirect.ActionId);
        Assert.AreEqual(BannerType.CrossChannelRedirect, redirect.BannerType);
        Assert.AreEqual(CrossChannelRedirectType.Redirect, redirect.RedirectType, "watchEndpoint button should map to Redirect.");
        Assert.AreEqual("@usadapekora", redirect.RedirectChannelHandle);
        Assert.AreEqual("AFcfu7GuxVs", redirect.RedirectVideoId, "Go-now redirect should carry a video ID.");
        Assert.IsNotNull(redirect.ChannelPhoto, "Should have a channel photo.");
        Assert.AreEqual(2, redirect.BannerMessage.Length, "Banner message should have 2 parts (prefix text + bold handle).");
        TextPart? handlePart = redirect.BannerMessage.OfType<TextPart>().FirstOrDefault(p => p.Bold);
        Assert.IsNotNull(handlePart, "The @handle run should be marked bold.");
        Assert.AreEqual("@usadapekora", handlePart.Text);
    }

    [TestMethod]
    public void ToBannerItem_UnrelatedAction_ReturnsNull()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.RemoveChatItem(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);
        Assert.IsNull(Parser.ToBannerItem(action));
    }

    [TestMethod]
    public void ToRemovedBannerActionId_RemoveBannerCommand_ReturnsTargetActionId()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.RemoveBanner(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        string? removedId = Parser.ToRemovedBannerActionId(action);

        Assert.AreEqual("PINNED_ACTION_ID_01", removedId);
    }

    // ── Poll closed ───────────────────────────────────────────────────────────

    [TestMethod]
    public void ToClosedPollId_ClosePanelAction_ReturnsPollId()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.CloseLiveChatActionPanel(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        string? pollId = Parser.ToClosedPollId(action);

        Assert.AreEqual("POLL_ID_SHOW_01", pollId);
    }

    [TestMethod]
    public void ToClosedPollId_OuroKronii_WithSkipDismissCommand_ReturnsCorrectPollId()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.ClosePollPanel_OuroKronii(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        string? pollId = Parser.ToClosedPollId(action);

        Assert.AreEqual("ChwKGkNKTzk4NUNiNzVNREZYWlFUQWdkT2U0VjVR", pollId);
    }

    [TestMethod]
    public void ToClosedPollId_UnrelatedAction_ReturnsNull()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.RemoveChatItem(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);
        Assert.IsNull(Parser.ToClosedPollId(action));
    }

    // ── Chat item replacement ─────────────────────────────────────────────────

    [TestMethod]
    public void ToReplacedChatItem_WithTextReplacement_ReturnsTargetIdAndChatItem()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.ReplaceChatItemWithText(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        (string? targetId, ChatItem? replacement) = Parser.ToReplacedChatItem(action);

        Assert.AreEqual("ChwKGkNQcTJ5X0t3NlpNREZlZkJ3Z1FkUTI4RmJR", targetId);
        Assert.IsNotNull(replacement, "Replacement should not be null for a text message.");
        // The replacement id should match the inner renderer id
        Assert.AreEqual("ChwKGkNQcTJ5X0t3NlpNREZlZkJ3Z1FkUTI4RmJR", replacement.Id);
        Assert.AreEqual("@asepjulian896", replacement.Author.Name);
        Assert.AreEqual("UCFIehAvmitLzMf3KDWlW-sA", replacement.Author.ChannelId);
        Assert.AreEqual(1, replacement.Message.Length);
        _ = Assert.IsInstanceOfType<TextPart>(replacement.Message[0]);
        Assert.AreEqual("pagi bokobo", ((TextPart)replacement.Message[0]).Text);
        // Timestamp from timestampUsec "1776033641718643"
        Assert.AreNotEqual(default, replacement.Timestamp);
    }

    [TestMethod]
    public void ToReplacedChatItem_WithPlaceholderReplacement_ReturnsTargetIdAndNullItem()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.ReplaceChatItemWithPlaceholder(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        (string? targetId, ChatItem? replacement) = Parser.ToReplacedChatItem(action);

        Assert.AreEqual("REPLACE_TARGET_PLACEHOLDER_01", targetId);
        Assert.IsNull(replacement, "Replacement should be null for a placeholder renderer.");
    }

    [TestMethod]
    public void ToReplacedChatItem_UnrelatedAction_ReturnsNullTargetId()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.RemoveChatItem(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        (string? targetId, ChatItem? replacement) = Parser.ToReplacedChatItem(action);

        Assert.IsNull(targetId);
        Assert.IsNull(replacement);
    }

    // ── Viewer engagement messages ────────────────────────────────────────────

    [TestMethod]
    public void ToEngagementItem_SubscribersOnly5Min_ReturnsCorrectItem()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.ViewerEngagementSubscribersOnly(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        EngagementItem? item = Parser.ToEngagementItem(action);

        Assert.IsNotNull(item);
        Assert.AreEqual(
            "Ci0KK1NVQlNDUklCRVJTX09OTFlfVkVNMjAyNi8wNC8xMS0wNDo1NjowNC41Nzk%3D",
            item.Id
        );
        Assert.AreEqual(EngagementMessageType.SubscribersOnly, item.MessageType);
        // Timestamp from timestampUsec "1775908564579677"
        Assert.AreNotEqual(default, item.Timestamp);
        Assert.AreEqual(3, item.Message.Length);
        _ = Assert.IsInstanceOfType<TextPart>(item.Message[0]);
        Assert.AreEqual("5 minutes", ((TextPart)item.Message[1]).Text);
        Assert.AreEqual(
            "//support.google.com/youtube/?p=subs_only_chat_viewer&hl=en",
            item.LearnMoreUrl
        );
        StringAssert.Contains(((TextPart)item.Message[0]).Text, "Subscribers-only mode");
    }

    [TestMethod]
    public void ToEngagementItem_SubscribersOnly20Min_ReturnsCorrectItem()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.ViewerEngagementSubscribersOnly20Min(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        EngagementItem? item = Parser.ToEngagementItem(action);

        Assert.IsNotNull(item);
        Assert.AreEqual(
            "Ci0KK1NVQlNDUklCRVJTX09OTFlfVkVNMjAyNi8wNC8xMS0wNDo1NjowNC41NTM%3D",
            item.Id
        );
        Assert.AreEqual(EngagementMessageType.SubscribersOnly, item.MessageType);
        Assert.AreEqual(3, item.Message.Length);
        Assert.AreEqual("20 minutes", ((TextPart)item.Message[1]).Text);
    }

    [TestMethod]
    public void ToEngagementItem_CommunityGuidelines_ReturnsCorrectItem()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.ViewerEngagementCommunityGuidelines(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        EngagementItem? item = Parser.ToEngagementItem(action);

        Assert.IsNotNull(item);
        Assert.AreEqual(
            "CjEKL0NPTU1VTklUWV9HVUlERUxJTkVTX1ZFTTIwMjYvMDQvMTEtMDQ6NTY6MDQuNjEx",
            item.Id
        );
        Assert.AreEqual(EngagementMessageType.CommunityGuidelines, item.MessageType);
        Assert.AreEqual(1, item.Message.Length);
        StringAssert.Contains(((TextPart)item.Message[0]).Text, "community guidelines");
        Assert.AreEqual(
            "//support.google.com/youtube/answer/2853856?hl=en#safe",
            item.LearnMoreUrl
        );
    }

    [TestMethod]
    public void ToEngagementItem_PollResult_ReturnsCorrectItem()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.ViewerEngagementPollResult(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        EngagementItem? item = Parser.ToEngagementItem(action);

        Assert.IsNotNull(item);
        Assert.AreEqual("ChwKGkNNS082cVdsNXBNREZicGJUQWdkME1VN2x3", item.Id);
        Assert.AreEqual(EngagementMessageType.PollResult, item.MessageType);
        Assert.AreEqual(6, item.Message.Length);
        Assert.IsNull(item.LearnMoreUrl);
        string pollResultText = string.Concat(item.Message.OfType<TextPart>().Select(p => p.Text));
        StringAssert.Contains(pollResultText, "Poll complete");
        StringAssert.Contains(pollResultText, "DIG (70%)");
    }

    [TestMethod]
    public void ToEngagementItem_PollResult_OuroKronii_ParsesBoldQuestionAndResults()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.ViewerEngagementPollResult_OuroKronii_WallVsFloor(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        EngagementItem? item = Parser.ToEngagementItem(action);

        Assert.IsNotNull(item);
        Assert.AreEqual("ChwKGkNKelhuTldmNzVNREZaZHhUQWdkY0JZeDZR", item.Id);
        Assert.AreEqual(EngagementMessageType.PollResult, item.MessageType);
        Assert.AreEqual(8, item.Message.Length, "Bold question + newline + 2 result lines + 2 newlines + summary = 8 parts.");
        Assert.IsNull(item.LearnMoreUrl);
        // Verify bold run for the question text
        TextPart questionPart = Assert.IsInstanceOfType<TextPart>(item.Message[0]);
        Assert.IsTrue(questionPart.Bold, "Poll question run should be bold.");
        StringAssert.Contains(questionPart.Text, "for the wood");
        // Verify overall content via plain-text concatenation
        string pollText = string.Concat(item.Message.OfType<TextPart>().Select(p => p.Text));
        StringAssert.Contains(pollText, "Poll complete: 1.9K votes");
        StringAssert.Contains(pollText, "floor (53%)");
    }

    [TestMethod]
    public void ToEngagementItem_UnrelatedAction_ReturnsNull()
    {
        Models.Response.Action? action = JsonSerializer.Deserialize<Models.Response.Action>(
            ActionTestData.RemoveChatItem(),
            s_jsonOptions
        );
        Assert.IsNotNull(action);

        EngagementItem? item = Parser.ToEngagementItem(action);

        Assert.IsNull(item);
    }
}