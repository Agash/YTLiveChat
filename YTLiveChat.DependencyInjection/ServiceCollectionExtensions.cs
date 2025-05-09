using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Services;
using YTLiveChat.Services;

namespace YTLiveChat.DependencyInjection;

/// <summary>
/// Extension methods for setting up YTLiveChat services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds YTLiveChat services to the specified <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to add services to.</param>
    /// <returns>The original <see cref="IHostApplicationBuilder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if builder is null.</exception>
    public static IHostApplicationBuilder AddYTLiveChat(this IHostApplicationBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(
                nameof(builder),
                "HostApplicationBuilder cannot be null."
            );
        }

        // Configure YTLiveChatOptions from the host configuration section "YTLiveChatOptions"
        builder.Services.Configure<YTLiveChatOptions>(
            builder.Configuration.GetSection(nameof(YTLiveChatOptions))
        );

        AddYTLiveChatServices(builder.Services);
        return builder;
    }

    /// <summary>
    /// Adds YTLiveChat services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">Optional <see cref="IConfiguration"/> instance to bind <see cref="YTLiveChatOptions"/> from (uses section "YTLiveChatOptions"). If null, options must be configured manually or via AddOptions.</param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if services is null.</exception>
    public static IServiceCollection AddYTLiveChat(
        this IServiceCollection services,
        IConfiguration? configuration = null
    )
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services), "ServiceCollection cannot be null.");

        // Configure YTLiveChatOptions if configuration is provided
        if (configuration != null)
        {
            services.Configure<YTLiveChatOptions>(
                configuration.GetSection(nameof(YTLiveChatOptions))
            );
        }
        else
        {
            // Ensure options can be configured programmatically if no IConfiguration is available
            services.AddOptions<YTLiveChatOptions>();
        }

        AddYTLiveChatServices(services);
        return services;
    }

    /// <summary>
    /// Helper method to register the core YTLiveChat services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    private static void AddYTLiveChatServices(IServiceCollection services)
    {
        // Register YTHttpClient and configure its underlying HttpClient via HttpClientFactory
        _ = services.AddHttpClient<YTHttpClient>(
            (serviceProvider, httpClient) =>
            {
                // Retrieve configured options
                YTLiveChatOptions ytChatOptions = serviceProvider
                    .GetRequiredService<IOptions<YTLiveChatOptions>>()
                    .Value;
                // Set base address for the HttpClient instance used by YTHttpClient
                httpClient.BaseAddress = new Uri(ytChatOptions.YoutubeBaseUrl);
            }
        );

        // Register the main IYTLiveChat service implementation
        _ = services.AddTransient<IYTLiveChat, Services.YTLiveChat>(provider =>
        {
            // Resolve dependencies from the DI container
            YTLiveChatOptions options = provider
                .GetRequiredService<IOptions<YTLiveChatOptions>>()
                .Value;
            YTHttpClient ytHttpClient = provider.GetRequiredService<YTHttpClient>(); // Get the configured YTHttpClient
            ILogger<Services.YTLiveChat>? logger = provider.GetService<
                ILogger<Services.YTLiveChat>
            >(); // Logger is optional

            // Construct the YTLiveChat service instance
            return new Services.YTLiveChat(options, ytHttpClient, logger);
        });
    }
}
