using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using YTLiveChat.Example;

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

    if (!string.IsNullOrWhiteSpace(runOptions.Handle) || !string.IsNullOrWhiteSpace(runOptions.ChannelId))
    {
        Console.Write("Enable continuous livestream monitor mode (BETA/UNSUPPORTED)? (y/N): ");
        string? monitorResponse = Console.ReadLine();
        if (
            !string.IsNullOrWhiteSpace(monitorResponse)
            && monitorResponse.Trim().Equals("y", StringComparison.OrdinalIgnoreCase)
        )
        {
            runOptions.EnableContinuousMonitor = true;

            Console.Write("Only auto-detect streams that are actively broadcasting (skip scheduled/free-chat)? (Y/n): ");
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
        if (!string.IsNullOrWhiteSpace(pathInput))
        {
            runOptions.DebugLogPath = Path.GetFullPath(pathInput.Trim());
        }
        else
        {
            runOptions.DebugLogPath = BuildDefaultLogPath(runOptions.SourceTag);
        }
    }

    runOptionsList.Add(runOptions);
    Console.WriteLine($"Added target [{runOptions.SourceTag}]. Total targets: {runOptionsList.Count}");
}

static string BuildDefaultLogPath(string sourceTag)
{
    string safe = string.Concat(sourceTag.Select(ch =>
        char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_'));
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
        $"{(options.Handle ?? options.ChannelId ?? options.LiveId)} | monitor={options.EnableContinuousMonitor} | rawLog={options.EnableJsonLogging}"
    );
}

Console.WriteLine("Attempting to connect...");

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("YTLiveChat.Services.YTLiveChat", LogLevel.Information);
builder.Logging.AddFilter("YTLiveChat.Example.ChatMonitorService", LogLevel.Information);

_ = builder.Services.AddHttpClient("YTLiveChatExample", (serviceProvider, httpClient) =>
{
    httpClient.BaseAddress = new Uri("https://www.youtube.com");
});

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
