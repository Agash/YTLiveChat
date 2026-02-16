using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.IO;
using System.Text;

using YTLiveChat.Contracts;
using YTLiveChat.DependencyInjection;
using YTLiveChat.Example;

Console.WriteLine("YTLiveChat Example Monitor");
Console.WriteLine("-------------------------");

// Force UTF-8 console IO so Japanese and other multilingual text is not rendered as '?'.
Console.InputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

string? identifier = null;
while (string.IsNullOrWhiteSpace(identifier))
{
    Console.Write("Enter the YouTube Live ID OR Handle (e.g., dQw4w9WgXcQ or @Google): ");
    identifier = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(identifier))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Input cannot be empty. Please try again.");
        Console.ResetColor();
    }
}

ExampleRunOptions runOptions = new();
if (identifier.StartsWith('@'))
{
    runOptions.Handle = identifier;
    Console.WriteLine($"Target Handle: {identifier}");
}
else
{
    runOptions.LiveId = identifier;
    Console.WriteLine($"Target Live ID: {identifier}");
}

Console.Write("Record raw InnerTube JSON for analysis? (y/N): ");
string? logResponse = Console.ReadLine();
if (
    !string.IsNullOrWhiteSpace(logResponse)
    && logResponse.Trim().Equals("y", StringComparison.OrdinalIgnoreCase)
)
{
    runOptions.EnableJsonLogging = true;
    Console.Write("Log file path (leave empty for default): ");
    string? pathInput = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(pathInput))
    {
        runOptions.DebugLogPath = Path.GetFullPath(pathInput.Trim());
    }
}

Console.WriteLine("Attempting to connect...");

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddYTLiveChat(builder.Configuration);

builder.Services.Configure<YTLiveChatOptions>(options =>
{
    if (runOptions.EnableJsonLogging)
    {
        options.DebugLogReceivedJsonItems = true;
        if (!string.IsNullOrWhiteSpace(runOptions.DebugLogPath))
        {
            options.DebugLogFilePath = runOptions.DebugLogPath!;
        }
    }
});

builder.Services.AddSingleton(runOptions);
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
