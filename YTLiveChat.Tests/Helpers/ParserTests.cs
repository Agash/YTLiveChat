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
        Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
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
        Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
        Assert.AreEqual("Congratulations ", ((TextPart)chatItem.Message[0]).Text);
        Assert.IsInstanceOfType<EmojiPart>(chatItem.Message[1]);
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
        Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
        Assert.AreEqual("Check this out: ", ((TextPart)chatItem.Message[0]).Text);
        Assert.IsInstanceOfType<EmojiPart>(chatItem.Message[1]);
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
        Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
        Assert.AreEqual("Text part 1, ", ((TextPart)chatItem.Message[0]).Text);
        Assert.IsInstanceOfType<EmojiPart>(chatItem.Message[1]);
        EmojiPart emojiPart = (EmojiPart)chatItem.Message[1];
        Assert.AreEqual("👍", emojiPart.EmojiText);
        Assert.AreEqual(":thumbs_up:", emojiPart.Alt);
        Assert.IsFalse(emojiPart.IsCustomEmoji);
        Assert.IsInstanceOfType<TextPart>(chatItem.Message[2]);
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
        Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
        Assert.AreEqual("Привет мир", ((TextPart)chatItem.Message[0]).Text);
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
            "Member (6 months)",
            chatItem.MembershipDetails.LevelName,
            "Level name from headerSubtext incorrect."
        );
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
            0,
            chatItem.Message.Length,
            "New member announcements typically have no user message body."
        );
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
            "Member (2 years)",
            chatItem.MembershipDetails.LevelName,
            "LevelName from badge tooltip incorrect."
        );
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
        Assert.IsInstanceOfType<TextPart>(chatItem.Message[0], "Message part should be TextPart.");
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
        Assert.AreEqual("Member (6 months)", chatItem.MembershipDetails.LevelName);
        Assert.AreEqual("The Fam", chatItem.MembershipDetails.HeaderSubtext);
        Assert.AreEqual("Member for 9 months", chatItem.MembershipDetails.HeaderPrimaryText);
        Assert.AreEqual(9, chatItem.MembershipDetails.MilestoneMonths);
        Assert.IsNotNull(chatItem.Author.Badge);
        Assert.AreEqual("Member (6 months)", chatItem.Author.Badge.Label);
        Assert.IsTrue(chatItem.IsMembership);
        Assert.AreEqual(1, chatItem.Message.Length);
        Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
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
            "LIVE_ID_PAGE_TEST",
            "API_KEY_PAGE_TEST",
            "CLIENT_VERSION_PAGE_TEST",
            "CONTINUATION_PAGE_TEST"
        );
        Models.FetchOptions options = Parser.GetOptionsFromLivePage(html);

        Assert.AreEqual("LIVE_ID_PAGE_TEST", options.LiveId);
        Assert.AreEqual("API_KEY_PAGE_TEST", options.ApiKey);
        Assert.AreEqual("CLIENT_VERSION_PAGE_TEST", options.ClientVersion);
        Assert.AreEqual("CONTINUATION_PAGE_TEST", options.Continuation);
    }

    [TestMethod]
    public void GetOptionsFromLivePage_FinishedStreamHtml_ThrowsException()
    {
        string html = UtilityTestData.GetFinishedStreamPageHtml("FINISHED_ID_PAGE_TEST");
        Exception ex = Assert.ThrowsException<Exception>(() => Parser.GetOptionsFromLivePage(html));
        Assert.IsTrue(ex.Message.Contains("is finished live"));
        Assert.IsTrue(ex.Message.Contains("FINISHED_ID_PAGE_TEST"));
    }

    [TestMethod]
    public void GetOptionsFromLivePage_MissingApiKey_ThrowsException()
    {
        string html = UtilityTestData
            .GetSampleLivePageHtml("LIVE_ID_NO_KEY", " ", "CLIENT_V", "CONT")
            .Replace("\"INNERTUBE_API_KEY\": \" \",", "");
        Exception ex = Assert.ThrowsException<Exception>(() => Parser.GetOptionsFromLivePage(html));
        Assert.IsTrue(ex.Message.Contains("API Key (INNERTUBE_API_KEY) not found"));
    }

    [TestMethod]
    public void GetOptionsFromLivePage_MissingClientVersion_ThrowsException()
    {
        string html = UtilityTestData
            .GetSampleLivePageHtml("LIVE_ID_NO_CV", "API_K", " ", "CONT")
            .Replace("\"INNERTUBE_CONTEXT_CLIENT_VERSION\": \" \",", "");
        Exception ex = Assert.ThrowsException<Exception>(() => Parser.GetOptionsFromLivePage(html));
        Assert.IsTrue(
            ex.Message.Contains("Client Version (INNERTUBE_CONTEXT_CLIENT_VERSION) not found")
        );
    }

    [TestMethod]
    public void GetOptionsFromLivePage_MissingContinuation_ThrowsException()
    {
        string html = UtilityTestData
            .GetSampleLivePageHtml("LIVE_ID_NO_CONT", "API_K", "CLIENT_V", " ")
            .Replace(
                "\"reloadContinuationData\": { \"continuation\": \" \" }",
                "\"reloadContinuationData\": { }"
            );
        Exception ex = Assert.ThrowsException<Exception>(() => Parser.GetOptionsFromLivePage(html));
        Assert.IsTrue(ex.Message.Contains("Initial Continuation token not found"));
    }

    [TestMethod]
    public void GetOptionsFromLivePage_MissingLiveIdCanonical_ThrowsException()
    {
        string html = UtilityTestData
            .GetSampleLivePageHtml(" ", "API_K", "CLIENT_V", "CONT")
            .Replace("<link rel=\"canonical\" href=\"https://www.youtube.com/watch?v= \">", "");
        Exception ex = Assert.ThrowsException<Exception>(() => Parser.GetOptionsFromLivePage(html));
        Assert.IsTrue(ex.Message.Contains("Live Stream canonical link not found"));
    }
}
