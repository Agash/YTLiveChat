using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using YTLiveChat.DependencyInjection;
using YTLiveChat.Example;

Console.WriteLine("YTLiveChat Example Monitor");
Console.WriteLine("-------------------------");

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

Console.WriteLine("Attempting to connect...");

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Optional: Set log level as needed, e.g., Information for more details from the library
builder.Logging.SetMinimumLevel(LogLevel.Information); // Changed to Information

builder.Services.AddYTLiveChat(builder.Configuration);

// Add the populated options object as a singleton
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
    // Attempt to get logger if host build failed early (might be null)
    ILogger<Program>? logger = builder
        .Services?.BuildServiceProvider()
        ?.GetService<ILogger<Program>>();
    logger?.LogCritical(ex, "Host terminated unexpectedly");
    return 1; // Indicate error
}
