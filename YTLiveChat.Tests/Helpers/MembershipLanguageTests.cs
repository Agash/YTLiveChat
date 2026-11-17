using System.Text;
using System.Text.Json;
using YTLiveChat.Contracts.Models;
using YTLiveChat.Helpers;

namespace YTLiveChat.Tests.Helpers;

// Membership items from the chat replays of six streams, read on 2026-11-17 in up to 38 interface languages (Latin,
// Cyrillic, Greek, Arabic, Hebrew, Devanagari, Bengali, Tamil, Telugu, Kannada, Malayalam, Thai, Burmese, CJK
// scripts). The item ids are the same in every language, so each language must give the value English gives, or
// nothing where the language writes the number as a word (Arabic "شهران" for two months, "one month" as a word in
// Latvian, Romanian, Arabic, Hebrew, Marathi and Tamil); never a different number.
[TestClass]
public class MembershipLanguageTests
{
    private static readonly Lazy<JsonDocument> Fixture = new(() =>
        JsonDocument.Parse(
            File.ReadAllText(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Languages",
                    "membership-items.2026-11-17.json"
                )
            )
        )
    );

    // Every item of a group: item id, then (language, raw item) pairs.
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

    private static MembershipDetails Parse(JsonElement item)
    {
        string json = "{\"addChatItemAction\":{\"item\":" + item.GetRawText() + "}}";
        Models.Response.Action action = JsonSerializer.Deserialize(
            json,
            YTLiveChatJsonSerializerContext.Default.Action
        )!;
        return action.ToChatItem()!.MembershipDetails!;
    }

    private static string PrimaryText(JsonElement item) =>
        string.Concat(
            item.GetProperty("liveChatMembershipItemRenderer")
                .GetProperty("headerPrimaryText")
                .GetProperty("runs")
                .EnumerateArray()
                .Select(r => r.GetProperty("text").GetString())
        );

    private static void AssertNoFailures(StringBuilder failures, int checkedCount, int minimum)
    {
        Assert.IsTrue(checkedCount >= minimum, $"only {checkedCount} items checked");
        Assert.AreEqual(0, failures.Length, failures.ToString());
    }

    [TestMethod]
    public void Milestone_EveryLanguage_MonthsAreEnglishsOrUnknown()
    {
        StringBuilder failures = new();
        int checkedCount = 0;
        foreach ((string id, Dictionary<string, JsonElement> languages) in Items("milestone"))
        {
            int? english = Parse(languages["en"]).MilestoneMonths;
            if (english is null)
            {
                _ = failures.AppendLine(
                    $"{id} en: no months in \"{PrimaryText(languages["en"])}\""
                );
                continue;
            }

            foreach ((string language, JsonElement item) in languages)
            {
                checkedCount++;
                MembershipDetails details = Parse(item);
                string text = PrimaryText(item);
                int? expected = text.Any(char.IsDigit) ? english : null;
                if (
                    details.EventType != MembershipEventType.Milestone
                    || details.MilestoneMonths != expected
                )
                {
                    _ = failures.AppendLine(
                        $"{id} {language}: {details.EventType} {details.MilestoneMonths?.ToString() ?? "null"}, expected {expected?.ToString() ?? "null"} from \"{text}\""
                    );
                }
            }
        }

        AssertNoFailures(failures, checkedCount, 500);
    }

    [TestMethod]
    public void GiftPurchase_EveryLanguage_CountIsEnglishs()
    {
        StringBuilder failures = new();
        int checkedCount = 0;
        HashSet<int> counts = [];
        foreach ((string id, Dictionary<string, JsonElement> languages) in Items("purchase"))
        {
            int? english = Parse(languages["en"]).GiftCount;
            _ = counts.Add(english ?? 0);
            foreach ((string language, JsonElement item) in languages)
            {
                checkedCount++;
                int? count = Parse(item).GiftCount;
                if (count != english)
                {
                    _ = failures.AppendLine($"{id} {language}: {count}, expected {english}");
                }
            }
        }

        CollectionAssert.AreEquivalent(new[] { 1, 5, 10, 20, 50 }, counts.ToArray());
        AssertNoFailures(failures, checkedCount, 150);
    }

    [TestMethod]
    public void GiftRedemption_EveryLanguage_GifterIsEnglishs()
    {
        StringBuilder failures = new();
        int checkedCount = 0;
        foreach ((string id, Dictionary<string, JsonElement> languages) in Items("redemption"))
        {
            string? english = Parse(languages["en"]).GifterUsername;
            StringAssert.StartsWith(english, "@");
            foreach ((string language, JsonElement item) in languages)
            {
                checkedCount++;
                string? gifter = Parse(item).GifterUsername;
                if (gifter != english)
                {
                    _ = failures.AppendLine($"{id} {language}: {gifter}, expected {english}");
                }
            }
        }

        AssertNoFailures(failures, checkedCount, 120);
    }

    [TestMethod]
    public void NewMember_English_TierAndType_OtherLanguages_NeverAnotherType()
    {
        StringBuilder failures = new();
        int checkedCount = 0;
        foreach ((string id, Dictionary<string, JsonElement> languages) in Items("newMember"))
        {
            MembershipDetails english = Parse(languages["en"]);
            string tier = languages["en"]
                .GetProperty("liveChatMembershipItemRenderer")
                .GetProperty("headerSubtext")
                .GetProperty("runs")[1]
                .GetProperty("text")
                .GetString()!;
            if (english.EventType != MembershipEventType.New || english.LevelName != tier)
            {
                _ = failures.AppendLine(
                    $"{id} en: {english.EventType} {english.LevelName}, expected New {tier}"
                );
            }

            foreach ((string language, JsonElement item) in languages)
            {
                checkedCount++;
                MembershipEventType type = Parse(item).EventType;
                if (type is MembershipEventType.Milestone or MembershipEventType.Upgraded)
                {
                    _ = failures.AppendLine($"{id} {language}: read as {type}");
                }
            }
        }

        AssertNoFailures(failures, checkedCount, 100);
    }

    // Upgrades name the new tier in a run of their own in every language, but only the words around it say "upgrade"
    // (French even writes "Nouveau membre"), so the type is read from the English text the library requests.
    [TestMethod]
    public void Upgrade_English_TierAndType_OtherLanguages_NeverAMilestone()
    {
        StringBuilder failures = new();
        int checkedCount = 0;
        foreach ((string id, Dictionary<string, JsonElement> languages) in Items("upgrade"))
        {
            MembershipDetails english = Parse(languages["en"]);
            string tier = languages["en"]
                .GetProperty("liveChatMembershipItemRenderer")
                .GetProperty("headerSubtext")
                .GetProperty("runs")[1]
                .GetProperty("text")
                .GetString()!;
            if (english.EventType != MembershipEventType.Upgraded || english.LevelName != tier)
            {
                _ = failures.AppendLine(
                    $"{id} en: {english.EventType} {english.LevelName}, expected Upgraded {tier}"
                );
            }

            foreach ((string language, JsonElement item) in languages)
            {
                checkedCount++;
                if (Parse(item).EventType == MembershipEventType.Milestone)
                {
                    _ = failures.AppendLine($"{id} {language}: read as Milestone");
                }
            }
        }

        AssertNoFailures(failures, checkedCount, 40);
    }

    // Number shapes the captures do not contain (counts and months stay small), written the ways locales group
    // digits: comma, dot, apostrophe, no-break and narrow no-break spaces, Arabic thousands separator, native digits.
    [TestMethod]
    [DataRow("1,000", 1000)]
    [DataRow("1.000", 1000)]
    [DataRow("1'000", 1000)]
    [DataRow("1 000", 1000)]
    [DataRow("1 000", 1000)]
    [DataRow("1 000", 1000)]
    [DataRow("١٬٠٠٠", 1000)]
    [DataRow("۱۲", 12)]
    [DataRow("১২", 12)]
    [DataRow("१२", 12)]
    [DataRow("၁၂", 12)]
    [DataRow("‫12", 12)]
    [DataRow(" 12 ", 12)]
    public void Milestone_NumberRun_GroupingAndScripts(string run, int expected)
    {
        string json =
            "{\"liveChatMembershipItemRenderer\":{\"id\":\"x\",\"timestampUsec\":\"1\",\"authorName\":{\"simpleText\":\"@a\"},"
            + "\"headerPrimaryText\":{\"runs\":[{\"text\":\"Lorem \"},{\"text\":"
            + JsonSerializer.Serialize(run)
            + "},{\"text\":\" ipsum\"}]},\"headerSubtext\":{\"simpleText\":\"Tier\"}}}";
        using JsonDocument item = JsonDocument.Parse(json);

        MembershipDetails details = Parse(item.RootElement);

        Assert.AreEqual(MembershipEventType.Milestone, details.EventType);
        Assert.AreEqual(expected, details.MilestoneMonths);
    }

    [TestMethod]
    public void GiftPurchase_TwoNumberRuns_FallsBackToTheEnglishSentence()
    {
        // A channel named only with digits gives two number runs; the English sentence still names the count.
        string json =
            "{\"liveChatSponsorshipsGiftPurchaseAnnouncementRenderer\":{\"id\":\"x\",\"timestampUsec\":\"1\",\"authorExternalChannelId\":\"UC\","
            + "\"header\":{\"liveChatSponsorshipsHeaderRenderer\":{\"authorName\":{\"simpleText\":\"@a\"},"
            + "\"primaryText\":{\"runs\":[{\"text\":\"Sent \"},{\"text\":\"5\"},{\"text\":\" \"},{\"text\":\"2434\"},{\"text\":\" gift memberships\"}]}}}}}";
        using JsonDocument item = JsonDocument.Parse(json);

        Assert.AreEqual(5, Parse(item.RootElement).GiftCount);
    }
}
