using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using YTLiveChat.Services;

/// <summary>
/// Snapshot mode: fetches one or more YouTube channel/video pages using the library's own
/// HTTP client (which handles the consent interstitial fallback) and saves the raw HTML to
/// files suitable for use as test fixtures in YTLiveChat.Tests/TestData/WebSnapshots/.
/// </summary>
internal static class SnapshotMode
{
    private static readonly string[] s_allPageTypes = ["home", "streams", "live"];

    public static async Task<int> RunAsync(string[] args)
    {
        SnapshotOptions options = ParseSnapshotOptions(args);

        if (options.Target is null)
        {
            PrintSnapshotUsage();
            return 1;
        }

        string? handle = options.Target.StartsWith("@", StringComparison.Ordinal)
            ? options.Target
            : null;
        string? channelId =
            !options.Target.StartsWith("@", StringComparison.Ordinal)
            && options.Target.StartsWith("UC", StringComparison.OrdinalIgnoreCase)
                ? options.Target
                : null;
        string? liveId = handle is null && channelId is null ? options.Target : null;

        string outputDir =
            options.OutputDir
            ?? Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "YTLiveChat.Tests",
                    "TestData",
                    "WebSnapshots"
                )
            );
        Directory.CreateDirectory(outputDir);

        string date = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        string safeTag = string.Concat(
            (options.Target ?? "unknown").Select(c => char.IsLetterOrDigit(c) ? c : '_')
        );

        using HttpClient httpClient = new() { BaseAddress = new Uri("https://www.youtube.com") };
        YTHttpClient ytHttpClient = new(httpClient, NullLogger<YTHttpClient>.Instance);

        IReadOnlyList<string> pages = options.Pages.Count > 0 ? options.Pages : s_allPageTypes;

        foreach (string page in pages)
        {
            string fileName = $"{safeTag}.{page}.{date}.html";
            string filePath = Path.Combine(outputDir, fileName);

            Console.Write($"Fetching /{page} ... ");

            try
            {
                string html = page switch
                {
                    "home" => await ytHttpClient
                        .GetChannelPageAsync(handle, channelId)
                        .ConfigureAwait(false),
                    "streams" => await ytHttpClient
                        .GetStreamsPageAsync(handle, channelId)
                        .ConfigureAwait(false),
                    "live" => await ytHttpClient
                        .GetOptionsAsync(handle, channelId, liveId)
                        .ConfigureAwait(false),
                    _ => throw new ArgumentException($"Unknown page type: {page}"),
                };

                await File.WriteAllTextAsync(filePath, html, new UTF8Encoding(false))
                    .ConfigureAwait(false);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"OK ({html.Length:N0} chars)");
                Console.ResetColor();
                Console.WriteLine($" → {filePath}");

                if (options.Diagnose)
                {
                    DiagnoseHtml(html, page);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.ResetColor();
            }
        }

        return 0;
    }

    private static void DiagnoseHtml(string html, string page)
    {
        string[] keys = ["INNERTUBE_API_KEY", "ytInitialData"];

        bool anyFound = false;
        foreach (string key in keys)
        {
            if (html.Contains(key, StringComparison.Ordinal))
            {
                if (!anyFound)
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine($"  [{page}] Keys found:");
                    Console.ResetColor();
                    anyFound = true;
                }
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"    + {key}");
                Console.ResetColor();
            }
        }

        if (!anyFound)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  [{page}] No known keys found.");
            Console.ResetColor();
        }
    }

    private static SnapshotOptions ParseSnapshotOptions(string[] args)
    {
        string? target = null;
        string? outputDir = null;
        List<string> pages = [];
        bool diagnose = false;

        foreach (string arg in args)
        {
            if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase))
            {
                PrintSnapshotUsage();
                Environment.Exit(0);
            }

            if (arg.Equals("--diagnose", StringComparison.OrdinalIgnoreCase))
            {
                diagnose = true;
                continue;
            }

            const string pagesPrefix = "--pages=";
            if (arg.StartsWith(pagesPrefix, StringComparison.OrdinalIgnoreCase))
            {
                foreach (
                    string p in arg[pagesPrefix.Length..]
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                )
                {
                    pages.Add(p.Trim().ToLowerInvariant());
                }
                continue;
            }

            const string outputPrefix = "--output-dir=";
            if (arg.StartsWith(outputPrefix, StringComparison.OrdinalIgnoreCase))
            {
                outputDir = arg[outputPrefix.Length..];
                continue;
            }

            target ??= arg;
        }

        return new SnapshotOptions(target, pages, outputDir, diagnose);
    }

    public static void PrintSnapshotUsage()
    {
        Console.WriteLine(
            "━━━ snapshot ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        );
        Console.WriteLine(
            "  dotnet run --project YTLiveChat.Tools -- snapshot <@handle|UCxxx|liveId> [options]"
        );
        Console.WriteLine();
        Console.WriteLine(
            "  Fetches YouTube channel/video pages and saves HTML snapshots as test fixtures."
        );
        Console.WriteLine(
            "  Uses the library's own HTTP client with consent-interstitial handling."
        );
        Console.WriteLine();
        Console.WriteLine("  Options:");
        Console.WriteLine(
            "    --pages=<list>         Comma-separated pages to fetch. Default: all."
        );
        Console.WriteLine("                           Values: home, streams, live");
        Console.WriteLine(
            "    --output-dir=<path>    Output directory. Default: YTLiveChat.Tests/TestData/WebSnapshots/"
        );
        Console.WriteLine(
            "    --diagnose             After saving, print which keys are present in the HTML."
        );
        Console.WriteLine();
        Console.WriteLine("  Examples:");
        Console.WriteLine(
            "    dotnet run --project YTLiveChat.Tools -- snapshot @HakosBaelz --diagnose"
        );
        Console.WriteLine(
            "    dotnet run --project YTLiveChat.Tools -- snapshot @AkiRosenthal --pages=live"
        );
    }

    private sealed record SnapshotOptions(
        string? Target,
        IReadOnlyList<string> Pages,
        string? OutputDir,
        bool Diagnose
    );
}
