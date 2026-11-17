using System.Text;
using System.Text.Json;
using YTLiveChat.Contracts.Models;
using YTLiveChat.Helpers;

namespace YTLiveChat.Tests.Helpers;

// Jewels gifts from the chat replays of three streams, read on 2026-11-17 in 38 interface languages: one item per
// distinct gift, and every combo ("comboed x2 Gold coin for 20 Jewels"). The gift's key comes from its image file and
// must be the same in every language; the name, combo count and Jewels come from the English texts the library
// requests and must never disagree with English where another language yields them at all.
[TestClass]
public class JewelsLanguageTests
{
    private static readonly Lazy<JsonDocument> Fixture = new(() =>
        JsonDocument.Parse(
            File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "Languages", "jewels-gifts.2026-11-18.json")
            )
        )
    );

    private static IEnumerable<(string Id, Dictionary<string, JsonElement> Languages)> Items(
        string group
    )
    {
        foreach (
            JsonProperty item in Fixture.Value.RootElement.GetProperty(group).EnumerateObject()
        )
        {
            yield return (
                item.Name,
                item.Value.EnumerateObject().ToDictionary(l => l.Name, l => l.Value)
            );
        }
    }

    private static GiftItem Parse(JsonElement item)
    {
        string json = "{\"addChatItemAction\":{\"item\":" + item.GetRawText() + "}}";
        Models.Response.Action action = JsonSerializer.Deserialize(
            json,
            YTLiveChatJsonSerializerContext.Default.Action
        )!;
        return action.ToGiftItem()!;
    }

    private static string Label(JsonElement item) =>
        item.GetProperty("giftMessageViewModel").GetProperty("giftImageA11yLabel").GetString()!;

    [TestMethod]
    public void Gift_EveryLanguage_KeyIsTheSame_EnglishNamesIt()
    {
        StringBuilder failures = new();
        int checkedCount = 0;
        HashSet<string> keys = [];
        foreach ((string id, Dictionary<string, JsonElement> languages) in Items("single"))
        {
            GiftItem english = Parse(languages["en"]);
            string label = Label(languages["en"]);
            string expectedName = label.Substring(
                label.LastIndexOf(", ", StringComparison.Ordinal) + 2
            );
            if (
                english.GiftKey is null
                || english.GiftItemName != expectedName
                || english.ComboCount is not null
            )
            {
                _ = failures.AppendLine(
                    $"{id} en: key {english.GiftKey}, name {english.GiftItemName} (expected {expectedName}), combo {english.ComboCount}"
                );
            }

            _ = keys.Add(english.GiftKey ?? "");
            foreach ((string language, JsonElement item) in languages)
            {
                checkedCount++;
                GiftItem gift = Parse(item);
                if (
                    gift.GiftKey != english.GiftKey
                    || gift.ComboCount is not null
                    || gift.JewelAmount is not null
                )
                {
                    _ = failures.AppendLine(
                        $"{id} {language}: key {gift.GiftKey}, combo {gift.ComboCount}, jewels {gift.JewelAmount}"
                    );
                }
            }
        }

        Assert.AreEqual(Items("single").Count(), keys.Count, "every gift has its own key");
        Assert.IsTrue(checkedCount >= 700, $"only {checkedCount} items checked");
        Assert.AreEqual(0, failures.Length, failures.ToString());
    }

    [TestMethod]
    public void Combo_English_CountNameAndJewels_OtherLanguages_NeverDifferent()
    {
        StringBuilder failures = new();
        int checkedCount = 0;
        foreach ((string id, Dictionary<string, JsonElement> languages) in Items("combo"))
        {
            GiftItem english = Parse(languages["en"]);
            string label = Label(languages["en"]);
            string expectedName = label.Substring(
                label.LastIndexOf(", ", StringComparison.Ordinal) + 2
            );
            if (
                english.ComboCount is not > 1
                || english.JewelAmount is not > 0
                || english.GiftItemName != expectedName
            )
            {
                _ = failures.AppendLine(
                    $"{id} en: combo {english.ComboCount}, jewels {english.JewelAmount}, name {english.GiftItemName} from \"{english.Text}\""
                );
            }

            foreach ((string language, JsonElement item) in languages)
            {
                checkedCount++;
                GiftItem gift = Parse(item);
                if (
                    gift.GiftKey != english.GiftKey
                    || (gift.ComboCount is not null && gift.ComboCount != english.ComboCount)
                    || (gift.JewelAmount is not null && gift.JewelAmount != english.JewelAmount)
                )
                {
                    _ = failures.AppendLine(
                        $"{id} {language}: key {gift.GiftKey}, combo {gift.ComboCount}, jewels {gift.JewelAmount} from \"{gift.Text}\""
                    );
                }
            }
        }

        Assert.IsTrue(checkedCount >= 100, $"only {checkedCount} items checked");
        Assert.AreEqual(0, failures.Length, failures.ToString());
    }

    [TestMethod]
    [DataRow(
        "https://www.gstatic.com/youtube/img/pdg/gift/assets/sparkles_v2_320x320.png=w480-h480",
        "sparkles"
    )]
    [DataRow("//www.gstatic.com/youtube/img/pdg/gift/assets/gold_coin.png=w480-h480", "gold_coin")]
    [DataRow("//www.gstatic.com/youtube/img/pdg/gift/assets/ramen_jp.png=w480-h480", "ramen_jp")]
    [DataRow(
        "//www.gstatic.com/youtube/img/pdg/gift/assets/laughing_bear_www.png",
        "laughing_bear_www"
    )]
    [DataRow("//www.gstatic.com/youtube/img/other/heart.png", null)]
    public void GiftKey_FromImageFile(string url, string? expected)
    {
        string json =
            "{\"giftMessageViewModel\":{\"id\":\"x\",\"text\":{\"content\":\"sent Heart\"},\"authorName\":{\"content\":\"@a\"},"
            + "\"giftImage\":{\"sources\":[{\"url\":"
            + JsonSerializer.Serialize(url)
            + "}]}}}";
        using JsonDocument item = JsonDocument.Parse(json);

        GiftItem gift = Parse(item.RootElement);

        Assert.AreEqual(expected, gift.GiftKey);
        Assert.AreEqual("Heart", gift.GiftItemName);
    }
}
