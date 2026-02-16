using System.Globalization;
using System.Text.RegularExpressions;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace YTLiveChat.Helpers;

internal static partial class CurrencyParser
{
    private const string DefaultCurrency = "USD";

    private static readonly Dictionary<string, string> s_xmlMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["$"] = "USD",
        ["A$"] = "AUD",
        ["CA$"] = "CAD",
        ["CFPF"] = "XPF",
        ["CN¥"] = "CNY",
        ["EC$"] = "XCD",
        ["F CFA"] = "XOF",
        ["F\u202FCFA"] = "XOF",
        ["FCFA"] = "XAF",
        ["HK$"] = "HKD",
        ["MX$"] = "MXN",
        ["NT$"] = "TWD",
        ["NZ$"] = "NZD",
        ["R$"] = "BRL",
        ["£"] = "GBP",
        ["¥"] = "JPY",
        ["₩"] = "KRW",
        ["₪"] = "ILS",
        ["₫"] = "VND",
        ["€"] = "EUR",
        ["₱"] = "PHP",
        ["₹"] = "INR",
    };

    private static readonly Dictionary<string, string> s_closureFallbackMap = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["AU$"] = "AUD",
        ["Tk"] = "BDT",
        ["lev"] = "BGN",
        ["FrCD"] = "CDF",
        ["CL$"] = "CLP",
        ["RMB¥"] = "CNY",
        ["COL$"] = "COP",
        ["Kč"] = "CZK",
        ["kr."] = "DKK",
        ["RD$"] = "DOP",
        ["LE"] = "EGP",
        ["Birr"] = "ETB",
        ["GB£"] = "GBP",
        ["kn"] = "HRK",
        ["Ft"] = "HUF",
        ["Rp"] = "IDR",
        ["IL₪"] = "ILS",
        ["Rs"] = "INR",
        ["JP¥"] = "JPY",
        ["KR₩"] = "KRW",
        ["SLRs"] = "LKR",
        ["Lt"] = "LTL",
        ["MN₮"] = "MNT",
        ["Rf"] = "MVR",
        ["Mex$"] = "MXN",
        ["RM"] = "MYR",
        ["NOkr"] = "NOK",
        ["kr"] = "SEK",
        ["B/."] = "PAB",
        ["S/."] = "PEN",
        ["PKRs."] = "PKR",
        ["zł"] = "PLN",
        ["din"] = "RSD",
        ["SAR"] = "SAR",
        ["S$"] = "SGD",
        ["TSh"] = "TZS",
        ["грн."] = "UAH",
        ["US$"] = "USD",
        ["$U"] = "UYU",
        ["VN₫"] = "VND",
        ["Af."] = "AFN",
        ["dram"] = "AMD",
        ["NAf."] = "ANG",
        ["Kz"] = "AOA",
        ["AR$"] = "ARS",
        ["Afl."] = "AWG",
        ["KM"] = "BAM",
        ["Bds$"] = "BBD",
        ["FBu"] = "BIF",
        ["BD$"] = "BMD",
        ["B$"] = "BND",
        ["Bs"] = "BOB",
        ["BS$"] = "BSD",
        ["Nu."] = "BTN",
        ["pula"] = "BWP",
        ["BZ$"] = "BZD",
        ["UF"] = "CLF",
        ["CUC$"] = "CUC",
        ["CU$"] = "CUP",
        ["Esc"] = "CVE",
        ["Fdj"] = "DJF",
        ["Nfk"] = "ERN",
        ["FJ$"] = "FJD",
        ["FK£"] = "FKP",
        ["GHS"] = "GHS",
        ["GI£"] = "GIP",
        ["FG"] = "GNF",
        ["Q"] = "GTQ",
        ["GY$"] = "GYD",
        ["L"] = "HNL",
        ["Ksh"] = "KES",
        ["KGS"] = "KGS",
        ["Riel"] = "KHR",
        ["CF"] = "KMF",
        ["KY$"] = "KYD",
        ["L$"] = "LRD",
        ["LSL"] = "LSL",
        ["LD"] = "LYD",
        ["dh"] = "MAD",
        ["MDL"] = "MDL",
        ["Ar"] = "MGA",
        ["K"] = "MMK",
        ["MOP$"] = "MOP",
        ["MURs"] = "MUR",
        ["MWK"] = "MWK",
        ["MTn"] = "MZN",
        ["N$"] = "NAD",
        ["NG₦"] = "NGN",
        ["C$"] = "CAD",
        ["NPRs"] = "NPR",
        ["QR"] = "QAR",
        ["RF"] = "RWF",
        ["SI$"] = "SBD",
        ["SH£"] = "SHP",
        ["SR$"] = "SRD",
        ["SY£"] = "SYP",
        ["T$"] = "TOP",
        ["TT$"] = "TTD",
        ["soʻm"] = "UZS",
        ["VT"] = "VUV",
        ["WS$"] = "WST",
        ["Z$"] = "ZWD",
    };

#if NET8_0_OR_GREATER
    private static readonly FrozenDictionary<string, string> s_symbolToCode =
        BuildMap().ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
#else
    private static readonly Dictionary<string, string> s_symbolToCode = BuildMap();
#endif

    public static (decimal AmountValue, string Currency) Parse(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return (0m, DefaultCurrency);
        }

        string normalizedInput = NormalizeSpaces(rawInput!);
        Match numberMatch = NumberExtractor().Match(normalizedInput);
        if (!numberMatch.Success)
        {
            return (0m, ResolveCurrencyCode(normalizedInput, normalizedInput));
        }

        decimal amountValue = ParseDecimal(numberMatch.Value);
        string before = normalizedInput.Substring(0, numberMatch.Index);
        string after = normalizedInput.Substring(numberMatch.Index + numberMatch.Length);
        string symbolPart = string.Concat(before, after).Trim();
        string currencyCode = ResolveCurrencyCode(symbolPart, normalizedInput);
        return (amountValue, currencyCode);
    }

    private static Dictionary<string, string> BuildMap()
    {
        Dictionary<string, string> merged = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> kvp in s_xmlMap)
        {
            merged[kvp.Key] = kvp.Value;
        }

        foreach (KeyValuePair<string, string> kvp in s_closureFallbackMap)
        {
            if (!merged.ContainsKey(kvp.Key))
            {
                merged[kvp.Key] = kvp.Value;
            }
        }

        // Closure collisions exist for "kr". For YT chat, SEK is generally the best fallback.
        if (!merged.ContainsKey("kr"))
        {
            merged["kr"] = "SEK";
        }

        return merged;
    }

    private static decimal ParseDecimal(string input)
    {
        string normalized = NormalizeAmountToken(input);
        return decimal.TryParse(
                normalized,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out decimal amount
            )
            ? amount
            : 0m;
    }

    private static string ResolveCurrencyCode(string symbolPart, string rawInput)
    {
        string normalizedSymbol = NormalizeSpaces(symbolPart).Replace(" ", string.Empty);
        if (s_symbolToCode.TryGetValue(normalizedSymbol, out string? directCode))
        {
            return directCode;
        }

        if (IsIsoCode(normalizedSymbol))
        {
            return normalizedSymbol.ToUpperInvariant();
        }

        Match embeddedSymbolCode = IsoCodeRegex().Match(normalizedSymbol.ToUpperInvariant());
        if (embeddedSymbolCode.Success)
        {
            return embeddedSymbolCode.Value;
        }

        Match embeddedRawCode = IsoCodeRegex().Match(rawInput.ToUpperInvariant());
        return embeddedRawCode.Success
            ? embeddedRawCode.Value
            : normalizedSymbol.Contains('$') ? DefaultCurrency : normalizedSymbol == "¥" ? "JPY" : DefaultCurrency;
    }

    private static string NormalizeSpaces(string value) =>
        value.Replace('\u00A0', ' ').Replace('\u202F', ' ').Trim();

    private static bool IsIsoCode(string value) => value.Length == 3 && IsAsciiUpper(value[0]) && IsAsciiUpper(value[1]) && IsAsciiUpper(value[2]);

    private static bool IsAsciiUpper(char value) => value is >= 'A' and <= 'Z';

    private static string NormalizeAmountToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return token;
        }

        string normalized = NormalizeSpaces(token).Replace(" ", string.Empty);
        bool hasComma = normalized.Contains(',');
        bool hasDot = normalized.Contains('.');

        if (hasComma && hasDot)
        {
            normalized = normalized.LastIndexOf(',') > normalized.LastIndexOf('.')
                ? normalized.Replace(".", string.Empty).Replace(",", ".")
                : normalized.Replace(",", string.Empty);

            return normalized;
        }

        if (hasComma)
        {
            int commaCount = normalized.Count(c => c == ',');
            if (commaCount > 1)
            {
                return normalized.Replace(",", string.Empty);
            }

            int commaIndex = normalized.LastIndexOf(',');
            int digitsAfter = normalized.Length - commaIndex - 1;
            return digitsAfter == 3 ? normalized.Replace(",", string.Empty) : normalized.Replace(",", ".");
        }

        if (hasDot)
        {
            int dotCount = normalized.Count(c => c == '.');
            if (dotCount > 1)
            {
                int lastDotIndex = normalized.LastIndexOf('.');
                string integerPart = normalized
                    .Substring(0, lastDotIndex)
                    .Replace(".", string.Empty);
                string decimalPart = normalized.Substring(lastDotIndex + 1);
                return decimalPart.Length == 3 ? integerPart + decimalPart : integerPart + "." + decimalPart;
            }
        }

        return normalized;
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"[\d\.,\s\u00A0\u202F]+", RegexOptions.Compiled)]
    private static partial Regex NumberExtractor();

    [GeneratedRegex(@"\b[A-Z]{3}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex IsoCodeRegex();
#else
    private static readonly Regex s_numberExtractor = new(
        @"[\d\.,\s\u00A0\u202F]+",
        RegexOptions.Compiled
    );

    private static readonly Regex s_isoCodeRegex = new(
        @"\b[A-Z]{3}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static Regex NumberExtractor() => s_numberExtractor;

    private static Regex IsoCodeRegex() => s_isoCodeRegex;
#endif
}
