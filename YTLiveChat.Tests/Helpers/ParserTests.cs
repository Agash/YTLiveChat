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

    // Helper: Parses JSON representing the *content* of a specific renderer type
    // (e.g., the value of "liveChatTextMessageRenderer") into a ChatItem.
    private ChatItem? ParseRendererContentToChatItem(
        string rendererContentJson,
        string rendererType = "liveChatTextMessageRenderer"
    )
    {
        // Step 1: Construct the AddChatItemActionItem object structure.
        // This object has properties like "liveChatTextMessageRenderer", "liveChatPaidMessageRenderer", etc.
        // The value of one of these properties will be the rendererContentJson.
        string addChatItemActionItemJson = $$"""
            {
              "{{rendererType}}": {{rendererContentJson}}
            }
            """;
        // Example: { "liveChatTextMessageRenderer": { "message": ..., "id": ... } }

        // Step 2: Wrap this AddChatItemActionItem into the 'Action' structure that Parser.ToChatItem expects.
        string actionJson = $$"""
            {
              "addChatItemAction": {
                "item": {{addChatItemActionItemJson}},
                "clientId": "TEST_CLIENT_ID_FOR_PARSER_TEST"
              }
            }
            """;

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
        Assert.IsNull(chatItem.Author.Badge); // Based on current TestMessageTestData
        Assert.IsNull(chatItem.Superchat);
        Assert.IsNull(chatItem.MembershipDetails);
        Assert.IsFalse(chatItem.IsOwner);
        Assert.IsFalse(chatItem.IsModerator);
        Assert.IsFalse(chatItem.IsVerified);
        // Timestamps are dynamic in test data, so precise check is hard.
        // Check it's within a reasonable recent window (e.g., last few seconds).
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
        Assert.AreEqual(2, chatItem.Message.Length); // "Check this out: " and the custom emoji

        Assert.IsInstanceOfType<TextPart>(chatItem.Message[0]);
        Assert.AreEqual("Check this out: ", ((TextPart)chatItem.Message[0]).Text);

        Assert.IsInstanceOfType<EmojiPart>(chatItem.Message[1]);
        EmojiPart emojiPart = (EmojiPart)chatItem.Message[1];
        Assert.IsTrue(emojiPart.IsCustomEmoji);
        Assert.AreEqual(":customcat:", emojiPart.EmojiText); // Based on shortcut defined in test data
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
        Assert.AreEqual(3, chatItem.Message.Length); // "Text part 1, ", thumbs_up_emoji, " then more text."

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

    // Tests for ParseLiveChatResponse
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

    // Tests for GetOptionsFromLivePage
    [TestMethod]
    public void GetOptionsFromLivePage_ValidHtml_ReturnsOptions()
    {
        string html = UtilityTestData.GetSampleLivePageHtml(
            "LIVE_ID_PAGE_TEST",
            "API_KEY_PAGE_TEST",
            "CLIENT_VERSION_PAGE_TEST",
            "CONTINUATION_PAGE_TEST"
        );
        Models.FetchOptions options = Parser.GetOptionsFromLivePage(html); // Namespace qualified FetchOptions

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
        // Simulate HTML missing the API key
        string html = UtilityTestData
            .GetSampleLivePageHtml("LIVE_ID_NO_KEY", " ", "CLIENT_V", "CONT")
            .Replace("\"INNERTUBE_API_KEY\": \" \",", ""); // Remove the key
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
            ); // Break continuation
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
