using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Models;
using YTLiveChat.Example;
using YTLiveChat.Services;

Console.WriteLine("YTLiveChat Example Monitor");
Console.WriteLine("-------------------------");

// Force UTF-8 console IO so Japanese and other multilingual text is not rendered as '?'.
Console.InputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

List<ExampleRunOptions> runOptionsList = [];

while (true)
{
    Console.Write("Enter YouTube target (Live ID, @Handle, or Channel ID UC..., empty to start): ");
    string? identifier = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(identifier))
    {
        if (runOptionsList.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("At least one target is required.");
            Console.ResetColor();
            continue;
        }

        break;
    }

    ExampleRunOptions runOptions = new() { SourceTag = identifier };
    if (identifier.StartsWith("@", StringComparison.Ordinal))
    {
        runOptions.Handle = identifier;
        Console.WriteLine($"Target Handle: {identifier}");
    }
    else if (identifier.StartsWith("UC", StringComparison.OrdinalIgnoreCase))
    {
        runOptions.ChannelId = identifier;
        Console.WriteLine($"Target Channel ID: {identifier}");
    }
    else
    {
        runOptions.LiveId = identifier;
        Console.WriteLine($"Target Live ID: {identifier}");
    }

    if (
        !string.IsNullOrWhiteSpace(runOptions.Handle)
        || !string.IsNullOrWhiteSpace(runOptions.ChannelId)
    )
    {
        Console.Write("Enable continuous livestream monitor mode (BETA/UNSUPPORTED)? (y/N): ");
        string? monitorResponse = Console.ReadLine();
        if (
            !string.IsNullOrWhiteSpace(monitorResponse)
            && monitorResponse.Trim().Equals("y", StringComparison.OrdinalIgnoreCase)
        )
        {
            runOptions.EnableContinuousMonitor = true;

            Console.Write(
                "Only auto-detect streams that are actively broadcasting (skip scheduled/free-chat)? (Y/n): "
            );
            string? activeOnlyResponse = Console.ReadLine();
            runOptions.RequireActiveBroadcastForAutoDetectedStream =
                string.IsNullOrWhiteSpace(activeOnlyResponse)
                || activeOnlyResponse.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);

            Console.Write("Ignored auto-detected live IDs (comma-separated, optional): ");
            string? ignoredLiveIdsInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(ignoredLiveIdsInput))
            {
                runOptions.IgnoredAutoDetectedLiveIds = ignoredLiveIdsInput
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            Console.Write("Live-check frequency in ms (default 10000): ");
            string? frequencyInput = Console.ReadLine();
            if (
                !string.IsNullOrWhiteSpace(frequencyInput)
                && int.TryParse(frequencyInput, out int liveCheckFrequency)
                && liveCheckFrequency > 0
            )
            {
                runOptions.LiveCheckFrequency = liveCheckFrequency;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(
                "Continuous monitor mode is BETA/UNSUPPORTED and may change or break at any time."
            );
            Console.ResetColor();
        }
    }

    Console.Write("Record raw InnerTube JSON for analysis for this target? (y/N): ");
    string? logResponse = Console.ReadLine();
    if (
        !string.IsNullOrWhiteSpace(logResponse)
        && logResponse.Trim().Equals("y", StringComparison.OrdinalIgnoreCase)
    )
    {
        runOptions.EnableJsonLogging = true;
        Console.Write("Log file path (leave empty for auto path): ");
        string? pathInput = Console.ReadLine();
        runOptions.DebugLogPath = !string.IsNullOrWhiteSpace(pathInput)
            ? Path.GetFullPath(pathInput.Trim())
            : BuildDefaultLogPath(runOptions.SourceTag);
    }

    runOptionsList.Add(runOptions);
    Console.WriteLine(
        $"Added target [{runOptions.SourceTag}]. Total targets: {runOptionsList.Count}"
    );
}

static string BuildDefaultLogPath(string sourceTag)
{
    string safe = string.Concat(
        sourceTag.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_')
    );
    string fileName = $"{safe}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.json";
    return Path.GetFullPath(Path.Combine("logs", fileName));
}

Console.WriteLine("Configured targets:");
foreach (ExampleRunOptions options in runOptionsList)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Write($"[{options.SourceTag}] ");
    Console.ResetColor();
    Console.WriteLine(
        $"{options.Handle ?? options.ChannelId ?? options.LiveId} | monitor={options.EnableContinuousMonitor} | rawLog={options.EnableJsonLogging}"
    );
}

// Offer streams listing for all targets that have a handle or channelId
List<ExampleRunOptions> streamsTargets = runOptionsList
    .Where(o => !string.IsNullOrWhiteSpace(o.Handle) || !string.IsNullOrWhiteSpace(o.ChannelId))
    .ToList();

if (streamsTargets.Count > 0)
{
    Console.Write("Fetch streams list for handle/channel targets? (y/N): ");
    string? streamsResponse = Console.ReadLine();
    if (
        !string.IsNullOrWhiteSpace(streamsResponse)
        && streamsResponse.Trim().Equals("y", StringComparison.OrdinalIgnoreCase)
    )
    {
        using ILoggerFactory streamsLogFactory = LoggerFactory.Create(b =>
            b.AddConsole().SetMinimumLevel(LogLevel.Warning)
        );
        using HttpClient streamsHttpClient = new()
        {
            BaseAddress = new Uri("https://www.youtube.com"),
        };
        YTHttpClient streamsYtHttpClient = new(
            streamsHttpClient,
            streamsLogFactory.CreateLogger<YTHttpClient>()
        );
        YTLiveChatOptions streamsYtOptions = new() { YoutubeBaseUrl = "https://www.youtube.com" };
        YTLiveChat.Services.YTLiveChat streamsService = new(
            streamsYtOptions,
            streamsYtHttpClient,
            streamsLogFactory.CreateLogger<YTLiveChat.Services.YTLiveChat>()
        );

        foreach (ExampleRunOptions target in streamsTargets)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"[{target.SourceTag}] ");
            Console.ResetColor();
            Console.WriteLine("Fetching streams...");

            try
            {
                IReadOnlyList<StreamInfo> streams = await streamsService.GetStreamsAsync(
                    handle: target.Handle,
                    channelId: target.ChannelId
                );

                if (streams.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("  (No streams found.)");
                    Console.ResetColor();
                }
                else
                {
                    foreach (StreamInfo stream in streams)
                    {
                        ConsoleColor statusColor = stream.Status switch
                        {
                            StreamStatus.Live => ConsoleColor.Red,
                            StreamStatus.Upcoming => ConsoleColor.Yellow,
                            _ => ConsoleColor.DarkGray,
                        };
                        Console.ForegroundColor = statusColor;
                        Console.Write($"  [{stream.Status}] ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(stream.Title);
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write($"  ({stream.LiveId})");
                        if (stream.ViewerCount.HasValue)
                            Console.Write($"  {stream.ViewerCount:N0} watching");
                        else if (stream.ViewCount.HasValue)
                            Console.Write($"  {stream.ViewCount:N0} views");
                        if (stream.ScheduledAt.HasValue)
                            Console.Write($"  @ {stream.ScheduledAt.Value.ToLocalTime():g}");
                        if (!string.IsNullOrWhiteSpace(stream.PublishedTimeText))
                            Console.Write($"  {stream.PublishedTimeText}");
                        if (stream.Duration.HasValue)
                            Console.Write($"  [{stream.Duration.Value:h\\:mm\\:ss}]");
                        Console.WriteLine();
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Error: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}

Console.WriteLine("Attempting to connect...");

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("YTLiveChat.Services.YTLiveChat", LogLevel.Information);
builder.Logging.AddFilter("YTLiveChat.Example.ChatMonitorService", LogLevel.Information);

_ = builder.Services.AddHttpClient(
    "YTLiveChatExample",
    (serviceProvider, httpClient) =>
    {
        httpClient.BaseAddress = new Uri("https://www.youtube.com");
    }
);

builder.Services.AddSingleton<IReadOnlyList<ExampleRunOptions>>(runOptionsList);
builder.Services.AddHostedService<ChatMonitorService>();

try
{
    using IHost host = builder.Build();
    Console.WriteLine("Host built. Running... (Press Ctrl+C to stop)");
    await host.RunAsync();
    Console.WriteLine("Host execution finished.");
    return 0;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Host operation cancelled (shutdown initiated).");
    return 0;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"An unexpected error occurred during host execution: {ex}");
    Console.ResetColor();

    ILogger<Program>? logger = builder
        .Services?.BuildServiceProvider()
        ?.GetService<ILogger<Program>>();
    logger?.LogCritical(ex, "Host terminated unexpectedly");
    return 1;
}
