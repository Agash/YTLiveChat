using YTLiveChat.Helpers;

namespace YTLiveChat.Tests.Helpers;

[TestClass]
public class CurrencyParserTests
{
    [TestMethod]
    [DataRow("$5.00", 5.00, "USD")]
    [DataRow("¥1,000", 1000, "JPY")]
    [DataRow("CA$10.00", 10.00, "CAD")]
    [DataRow("€2.50", 2.50, "EUR")]
    [DataRow("PHP 100.00", 100.00, "PHP")]
    public void Parse_KnownCurrency_ReturnsIsoCode(string input, double amount, string currency)
    {
        (decimal parsedAmount, string? parsedCurrency) = CurrencyParser.Parse(input);

        Assert.AreEqual((decimal)amount, parsedAmount);
        Assert.AreEqual(currency, parsedCurrency);
    }

    [TestMethod]
    [DataRow("@@5.00")]
    [DataRow("5.00")]
    public void Parse_UnrecognizedCurrency_ReturnsNullCurrency(string input)
    {
        (decimal parsedAmount, string? parsedCurrency) = CurrencyParser.Parse(input);

        Assert.AreEqual(5.00m, parsedAmount);
        Assert.IsNull(parsedCurrency);
    }

    [TestMethod]
    public void Parse_Empty_ReturnsZeroAndNoCurrency()
    {
        (decimal parsedAmount, string? parsedCurrency) = CurrencyParser.Parse("  ");

        Assert.AreEqual(0m, parsedAmount);
        Assert.IsNull(parsedCurrency);
    }
}
