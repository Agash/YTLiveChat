using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YTLiveChat.DependencyInjection;
using YTLiveChat.Example;

Console.WriteLine("YTLiveChat Example Monitor");
Console.WriteLine("-------------------------");

// --- Prompt for Live ID ---
string? liveId = null;
while (string.IsNullOrWhiteSpace(liveId))
{
    Console.Write("Enter the YouTube Live ID (e.g., video ID from the URL): ");
    liveId = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(liveId))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Live ID cannot be empty. Please try again.");
        Console.ResetColor();
    }
}

Console.WriteLine($"Target Live ID: {liveId}");
Console.WriteLine("Attempting to connect...");

// --- Host Setup ---
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Warning); // Adjust as needed (e.g., LogLevel.Debug for more detail)

// Add YTLiveChat services using the extension method
// This automatically binds YTLiveChatOptions from configuration (appsettings.json, env vars, etc.) if present
// You could also configure options programmatically here if needed:
// builder.Services.Configure<YTLiveChat.Contracts.YTLiveChatOptions>(options => {
//     options.RequestFrequency = 1500; // Example override
// });
builder.Services.AddYTLiveChat(builder.Configuration);

// Register the options class containing the Live ID for injection into ChatMonitorService
builder.Services.AddSingleton(new ExampleRunOptions { LiveId = liveId }); // Use the liveId read from input

// Register our ChatMonitorService as a Hosted Service
builder.Services.AddHostedService<ChatMonitorService>();

// --- Run Host ---
try
{
    using IHost host = builder.Build();
    Console.WriteLine("Host built. Running... (Press Ctrl+C to stop)");
    // Run the host. This will start the ChatMonitorService.
    await host.RunAsync();
    Console.WriteLine("Host execution finished.");
    return 0;
}
catch (OperationCanceledException)
{
    // Expected when Ctrl+C is pressed or host shutdown is initiated gracefully elsewhere.
    Console.WriteLine("Host operation cancelled (shutdown initiated).");
    return 0;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"An unexpected error occurred during host execution: {ex}");
    Console.ResetColor();
    // Try to log the exception if the logger was successfully configured
    ILogger<Program>? logger = builder
        .Services?.BuildServiceProvider()
        ?.GetService<ILogger<Program>>();
    logger?.LogCritical(ex, "Host terminated unexpectedly");
    return 1; // Indicate error
}
