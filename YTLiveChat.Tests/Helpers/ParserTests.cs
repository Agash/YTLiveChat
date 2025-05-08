using System.Text.Json;
using YTLiveChat.Contracts.Models;
using YTLiveChat.Helpers;
using YTLiveChat.Models;
using YTLiveChat.Models.Response;

namespace YTLiveChat.Tests.Helpers;

[TestClass]
public class ParserTests
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // Helper to deserialize the wrapped response
    private static LiveChatResponse? DeserializeWrappedResponse(string json) =>
        JsonSerializer.Deserialize<LiveChatResponse>(json, s_jsonOptions);

    // Helper to get the first ChatItem from a deserialized response
    private static ChatItem? GetFirstChatItem(LiveChatResponse? response)
    {
        (List<ChatItem> Items, _) = Parser.ParseLiveChatResponse(response);
        return Items.FirstOrDefault();
    }

    [TestMethod]
    public void GetOptionsFromLivePage_ValidHtml_ReturnsOptions()
    {
        // Arrange
        string html = ParserTestData.SampleLivePageHtml;
        FetchOptions expectedOptions = new()
        {
            LiveId = "EXISTING_LIVE_ID",
            ApiKey = "TEST_API_KEY",
            ClientVersion = "2.20240101.01.00",
            Continuation = "INITIAL_CONTINUATION_TOKEN",
        };

        // Act
        FetchOptions actualOptions = Parser.GetOptionsFromLivePage(html);

        // Assert
        Assert.AreEqual(expectedOptions.LiveId, actualOptions.LiveId);
        Assert.AreEqual(expectedOptions.ApiKey, actualOptions.ApiKey);
        Assert.AreEqual(expectedOptions.ClientVersion, actualOptions.ClientVersion);
        Assert.AreEqual(expectedOptions.Continuation, actualOptions.Continuation);
    }

    [TestMethod]
    public void GetOptionsFromLivePage_FinishedStreamHtml_ThrowsException()
    {
        // Arrange
        string html = ParserTestData.SampleLivePageHtmlFinished;

        // Act & Assert
        Exception ex = Assert.ThrowsException<Exception>(() => Parser.GetOptionsFromLivePage(html));
        Assert.IsTrue(ex.Message.Contains("is finished live"));
        Assert.IsTrue(ex.Message.Contains("FINISHED_LIVE_ID"));
    }

    [TestMethod]
    public void GetOptionsFromLivePage_MissingApiKey_ThrowsException()
    {
        // Arrange
        string html = ParserTestData.SampleLivePageHtmlMissingKey;

        // Act & Assert
        Exception ex = Assert.ThrowsException<Exception>(() => Parser.GetOptionsFromLivePage(html));
        Assert.IsTrue(ex.Message.Contains("API Key (INNERTUBE_API_KEY) not found"));
    }

    // Add tests for missing Continuation, ClientVersion, Canonical Link

    [TestMethod]
    public void ParseLiveChatResponse_TextMessage_ReturnsCorrectChatItem()
    {
        // Arrange
        string authorName = "TestAuthor";
        string messageText = "Simple message here.";
        string json = ParserTestData.TextMessageJson(author: authorName, message: messageText);
        LiveChatResponse? response = DeserializeWrappedResponse(json);

        // Act
        ChatItem? chatItem = GetFirstChatItem(response);

        // Assert
        Assert.IsNotNull(chatItem);
        Assert.AreEqual(authorName, chatItem.Author.Name);
        Assert.AreEqual(1, chatItem.Message.Length);
        Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
        Assert.AreEqual(messageText, ((TextPart)chatItem.Message[0]).Text);
        Assert.IsNull(chatItem.Superchat);
        Assert.IsNull(chatItem.MembershipDetails);
        Assert.IsFalse(chatItem.IsModerator);
        Assert.IsFalse(chatItem.IsOwner);
        Assert.IsFalse(chatItem.IsVerified);
        Assert.IsFalse(chatItem.IsMembership); // No membership badge in this test data
    }

    [TestMethod]
    public void ParseLiveChatResponse_TextMessageWithEmoji_ReturnsCorrectChatItem()
    {
        // Arrange
        string json = ParserTestData.TextMessageWithEmojiJson(
            text1: "Prefix ",
            emojiText: "😊",
            text2: " Suffix"
        );
        LiveChatResponse? response = DeserializeWrappedResponse(json);

        // Act
        ChatItem? chatItem = GetFirstChatItem(response);

        // Assert
        Assert.IsNotNull(chatItem);
        Assert.AreEqual(3, chatItem.Message.Length);
        Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
        Assert.AreEqual("Prefix ", ((TextPart)chatItem.Message[0]).Text);
        Assert.IsInstanceOfType<EmojiPart>(chatItem.Message[1]);
        Assert.AreEqual("😊", ((EmojiPart)chatItem.Message[1]).EmojiText); // Using EmojiId for standard emojis
        Assert.AreEqual("Smiling Face", ((EmojiPart)chatItem.Message[1]).Alt); // Alt derived from accessibility
        Assert.IsFalse(((EmojiPart)chatItem.Message[1]).IsCustomEmoji);
        Assert.IsInstanceOfType<TextPart>(chatItem.Message[2]);
        Assert.AreEqual(" Suffix", ((TextPart)chatItem.Message[2]).Text);
    }

    [TestMethod]
    public void ParseLiveChatResponse_Superchat_ReturnsCorrectChatItem()
    {
        // Arrange
        string amountStr = "$9.99";
        string message = "Awesome!";
        long bodyColorLong = -1102084L; // Orange FFEF6C00
        long headerColorLong = -1102084L; // Also Orange in this example
        long bodyTextLong = -16777216L; // Black
        long headerTextLong = -16777216L; // Black
        string expectedBodyColor = "EF6C00";
        string expectedHeaderColor = "EF6C00";
        string expectedBodyTextColor = "000000";
        string expectedHeaderTextColor = "000000";

        string json = ParserTestData.SuperchatJson(
            amount: amountStr,
            message: message,
            bodyColor: bodyColorLong,
            headerColor: headerColorLong,
            bodyTextColor: bodyTextLong,
            headerTextColor: headerTextLong
        );
        LiveChatResponse? response = DeserializeWrappedResponse(json);

        // Act
        ChatItem? chatItem = GetFirstChatItem(response);

        // Assert
        Assert.IsNotNull(chatItem);
        Assert.AreEqual(message, ((TextPart)chatItem.Message[0]).Text); // Superchat message is in main Message array
        Assert.IsNotNull(chatItem.Superchat);
        Assert.AreEqual(amountStr, chatItem.Superchat.AmountString);
        Assert.AreEqual(9.99M, chatItem.Superchat.AmountValue);
        Assert.AreEqual("USD", chatItem.Superchat.Currency); // Deduced from $
        Assert.AreEqual(expectedBodyColor, chatItem.Superchat.BodyBackgroundColor);
        Assert.AreEqual(expectedHeaderColor, chatItem.Superchat.HeaderBackgroundColor);
        Assert.AreEqual(expectedBodyTextColor, chatItem.Superchat.BodyTextColor);
        Assert.AreEqual(expectedHeaderTextColor, chatItem.Superchat.HeaderTextColor);
        Assert.IsNull(chatItem.Superchat.Sticker);
        Assert.IsNull(chatItem.MembershipDetails);
    }

    [TestMethod]
    public void ParseLiveChatResponse_SuperchatJPY_ReturnsCorrectChatItem()
    {
        // Arrange
        string amountStr = "￥1,500"; // Yen
        string message = "ありがとう！";

        string json = ParserTestData.SuperchatJson(amount: amountStr, message: message);
        LiveChatResponse? response = DeserializeWrappedResponse(json);

        // Act
        ChatItem? chatItem = GetFirstChatItem(response);

        // Assert
        Assert.IsNotNull(chatItem);
        Assert.IsNotNull(chatItem.Superchat);
        Assert.AreEqual(amountStr, chatItem.Superchat.AmountString);
        Assert.AreEqual(1500M, chatItem.Superchat.AmountValue); // Comma removed
        Assert.AreEqual("JPY", chatItem.Superchat.Currency); // Deduced from ¥
    }

    [TestMethod]
    public void ParseLiveChatResponse_SuperSticker_ReturnsCorrectChatItem()
    {
        // Arrange
        string amountStr = "€2.00";
        string stickerAlt = "Waving Fox";
        long bgColorLong = -11619841L; // Green FF4CAF50
        string expectedBgColor = "4CAF50";

        string json = ParserTestData.SuperStickerJson(
            amount: amountStr,
            stickerAlt: stickerAlt,
            bgColor: bgColorLong
        );
        LiveChatResponse? response = DeserializeWrappedResponse(json);

        // Act
        ChatItem? chatItem = GetFirstChatItem(response);

        // Assert
        Assert.IsNotNull(chatItem);
        Assert.AreEqual(0, chatItem.Message.Length); // Super Stickers usually have no separate message runs
        Assert.IsNotNull(chatItem.Superchat);
        Assert.AreEqual(amountStr, chatItem.Superchat.AmountString);
        Assert.AreEqual(2.00M, chatItem.Superchat.AmountValue);
        Assert.AreEqual("EUR", chatItem.Superchat.Currency); // Deduced from €
        Assert.AreEqual(expectedBgColor, chatItem.Superchat.BodyBackgroundColor); // Sticker BG color maps to Body BG
        Assert.IsNull(chatItem.Superchat.HeaderBackgroundColor);
        Assert.IsNull(chatItem.Superchat.HeaderTextColor);
        Assert.IsNull(chatItem.Superchat.BodyTextColor);
        Assert.IsNotNull(chatItem.Superchat.Sticker);
        Assert.AreEqual(stickerAlt, chatItem.Superchat.Sticker.Alt);
        Assert.IsFalse(string.IsNullOrEmpty(chatItem.Superchat.Sticker.Url));
        Assert.IsNull(chatItem.MembershipDetails);
    }

    [TestMethod]
    public void ParseLiveChatResponse_NewMember_ReturnsCorrectChatItem()
    {
        // Arrange
        string authorName = "NewMemberGuy";
        string levelName = "Awesome Tier";
        string json = ParserTestData.NewMemberJson(author: authorName, level: levelName);
        LiveChatResponse? response = DeserializeWrappedResponse(json);

        // Act
        ChatItem? chatItem = GetFirstChatItem(response);

        // Assert
        Assert.IsNotNull(chatItem);
        Assert.AreEqual(authorName, chatItem.Author.Name);
        Assert.IsNotNull(chatItem.Author.Badge);
        Assert.AreEqual(levelName, chatItem.Author.Badge.Label); // Check badge label
        Assert.IsNotNull(chatItem.MembershipDetails);
        Assert.AreEqual(MembershipEventType.New, chatItem.MembershipDetails.EventType);
        Assert.AreEqual(levelName, chatItem.MembershipDetails.LevelName);
        Assert.AreEqual("New member", chatItem.MembershipDetails.HeaderSubtext);
        Assert.IsNotNull(chatItem.MembershipDetails.HeaderPrimaryText);
        Assert.IsTrue(chatItem.MembershipDetails.HeaderPrimaryText.Contains(levelName));
        Assert.IsNull(chatItem.MembershipDetails.MilestoneMonths);
        Assert.IsNull(chatItem.MembershipDetails.GiftCount);
        Assert.IsNull(chatItem.MembershipDetails.GifterUsername);
        Assert.IsTrue(chatItem.IsMembership);
        Assert.IsNull(chatItem.Superchat);
    }

    [TestMethod]
    public void ParseLiveChatResponse_MembershipMilestone_ReturnsCorrectChatItem()
    {
        // Arrange
        string authorName = "LoyalFan";
        string levelName = "Patron";
        int months = 6;
        string comment = "Still here!";
        string json = ParserTestData.MembershipMilestoneJson(
            author: authorName,
            level: levelName,
            months: months,
            userComment: comment
        );
        LiveChatResponse? response = DeserializeWrappedResponse(json);

        // Act
        ChatItem? chatItem = GetFirstChatItem(response);

        // Assert
        Assert.IsNotNull(chatItem);
        Assert.AreEqual(authorName, chatItem.Author.Name);
        Assert.IsTrue(chatItem.Author.Badge?.Label?.Contains(levelName) ?? false);
        Assert.AreEqual(1, chatItem.Message.Length); // User comment is the message
        Assert.AreEqual(comment, ((TextPart)chatItem.Message[0]).Text);
        Assert.IsNotNull(chatItem.MembershipDetails);
        Assert.AreEqual(MembershipEventType.Milestone, chatItem.MembershipDetails.EventType);
        Assert.AreEqual(levelName, chatItem.MembershipDetails.LevelName); // Level name from badge tooltip parsing
        Assert.AreEqual(months, chatItem.MembershipDetails.MilestoneMonths);
        Assert.IsNotNull(chatItem.MembershipDetails.HeaderPrimaryText);
        Assert.IsTrue(chatItem.MembershipDetails.HeaderPrimaryText.Contains($"{months} months"));
        Assert.IsNull(chatItem.MembershipDetails.GiftCount);
        Assert.IsNull(chatItem.MembershipDetails.GifterUsername);
        Assert.IsTrue(chatItem.IsMembership);
        Assert.IsNull(chatItem.Superchat);
    }

    [TestMethod]
    public void ParseLiveChatResponse_GiftPurchase_ReturnsCorrectChatItem()
    {
        // Arrange
        string gifterName = "SantaClaus";
        string levelName = "Elf Tier";
        int count = 10;
        string json = ParserTestData.GiftPurchaseJson(
            gifter: gifterName,
            level: levelName,
            count: count
        );
        LiveChatResponse? response = DeserializeWrappedResponse(json);

        // Act
        ChatItem? chatItem = GetFirstChatItem(response);

        // Assert
        Assert.IsNotNull(chatItem);
        // IMPORTANT: Author of the ChatItem *IS* the gifter in this case
        Assert.AreEqual(gifterName, chatItem.Author.Name);
        Assert.IsTrue(chatItem.Author.Badge?.Label?.Contains(levelName) ?? false); // Gifter's badge
        Assert.AreEqual(0, chatItem.Message.Length); // Typically no message body
        Assert.IsNotNull(chatItem.MembershipDetails);
        Assert.AreEqual(MembershipEventType.GiftPurchase, chatItem.MembershipDetails.EventType);
        Assert.AreEqual(levelName, chatItem.MembershipDetails.LevelName);
        Assert.AreEqual(count, chatItem.MembershipDetails.GiftCount);
        Assert.AreEqual(gifterName, chatItem.MembershipDetails.GifterUsername); // Gifter name explicitly stored
        Assert.IsNull(chatItem.MembershipDetails.MilestoneMonths);
        Assert.IsTrue(chatItem.IsMembership); // Gifter has membership badge
        Assert.IsNull(chatItem.Superchat);
    }

    [TestMethod]
    public void ParseLiveChatResponse_GiftRedemption_ReturnsCorrectChatItem()
    {
        // Arrange
        string recipientName = "LuckyWinner";
        string levelName = "Prize Tier";
        string gifterName = "GenerousDonor";
        string json = ParserTestData.GiftRedemptionJson(
            recipient: recipientName,
            level: levelName,
            gifter: gifterName
        );
        LiveChatResponse? response = DeserializeWrappedResponse(json);

        // Act
        ChatItem? chatItem = GetFirstChatItem(response);

        // Assert
        Assert.IsNotNull(chatItem);
        // IMPORTANT: Author of the ChatItem *IS* the recipient
        Assert.AreEqual(recipientName, chatItem.Author.Name);
        Assert.IsTrue(chatItem.Author.Badge?.Label?.Contains(levelName) ?? false); // Recipient's new badge
        Assert.IsTrue(chatItem.Message.Length > 0); // Contains welcome message potentially with gifter name
        // Let's check if the parsed message contains the gifter name
        string fullMessage = string.Concat(chatItem.Message.Select(p => p.ToString()));
        Assert.IsTrue(fullMessage.Contains(gifterName));

        Assert.IsNotNull(chatItem.MembershipDetails);
        Assert.AreEqual(MembershipEventType.GiftRedemption, chatItem.MembershipDetails.EventType);
        Assert.AreEqual(levelName, chatItem.MembershipDetails.LevelName);
        Assert.AreEqual(recipientName, chatItem.MembershipDetails.RecipientUsername); // Recipient stored explicitly
        Assert.AreEqual(gifterName, chatItem.MembershipDetails.GifterUsername); // Gifter name parsed from message
        Assert.IsNull(chatItem.MembershipDetails.MilestoneMonths);
        Assert.IsNull(chatItem.MembershipDetails.GiftCount);
        Assert.IsTrue(chatItem.IsMembership); // Recipient now has membership
        Assert.IsNull(chatItem.Superchat);
    }

    [TestMethod]
    public void ParseLiveChatResponse_ModeratorMessage_ReturnsCorrectChatItem()
    {
        // Arrange
        string modName = "Moddy";
        string json = ParserTestData.ModeratorMessageJson(author: modName);
        LiveChatResponse? response = DeserializeWrappedResponse(json);

        // Act
        ChatItem? chatItem = GetFirstChatItem(response);

        // Assert
        Assert.IsNotNull(chatItem);
        Assert.AreEqual(modName, chatItem.Author.Name);
        Assert.IsNull(chatItem.Author.Badge); // Standard mod badge doesn't populate Author.Badge.Thumbnail
        Assert.IsTrue(chatItem.IsModerator);
        Assert.IsFalse(chatItem.IsOwner);
        Assert.IsFalse(chatItem.IsVerified);
        Assert.IsFalse(chatItem.IsMembership);
        Assert.IsNull(chatItem.Superchat);
        Assert.IsNull(chatItem.MembershipDetails);
    }

    [TestMethod]
    public void ParseLiveChatResponse_HandlesNoContinuation()
    {
        // Arrange
        string json = ParserTestData.ResponseWithNoContinuation();
        LiveChatResponse? response = DeserializeWrappedResponse(json);

        // Act
        (List<ChatItem> items, string? continuation) = Parser.ParseLiveChatResponse(response);

        // Assert
        Assert.IsTrue(items.Count > 0); // Should still parse items
        Assert.IsNull(continuation); // Expecting null continuation
    }

    // Add more tests for:
    // - Owner messages (IsOwner = true)
    // - Verified messages (IsVerified = true)
    // - Messages from existing members (IsMembership = true, MembershipDetails = null)
    // - Custom Emojis (IsCustomEmoji = true, correct EmojiText)
    // - Edge cases: Empty messages, messages with only emojis, missing optional fields in JSON
    // - Different continuation token types (InvalidationContinuationData)
}
