using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using YTLiveChat.Contracts.Services;
using YTLiveChat.Services;

namespace YTLiveChat.Contracts;

/// <summary>
/// Provides extension methods for registering YTLiveChat services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds YTLiveChat services to the specified <see cref="IHostApplicationBuilder"/>.
    /// This method configures <see cref="YTLiveChatOptions"/> from the application's
    /// configuration using the section named "YTLiveChatOptions".
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The <see cref="IHostApplicationBuilder"/> for chaining.</returns>
    public static IHostApplicationBuilder AddYTLiveChat(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Configure options from the standard configuration section
        builder.Services.Configure<YTLiveChatOptions>(
            builder.Configuration.GetSection(nameof(YTLiveChatOptions))
        );

        // Register core services
        AddYTLiveChatServices(builder.Services);

        return builder;
    }

    /// <summary>
    /// Adds YTLiveChat services to the specified <see cref="IServiceCollection"/>.
    /// <para>
    /// Note: This method ONLY registers the services. You must separately configure
    /// <see cref="YTLiveChatOptions"/>, for example, by calling:
    /// </para>
    /// <example>
    /// <code>
    /// services.Configure&lt;YTLiveChatOptions&gt;(configuration.GetSection("YTLiveChatOptions"));
    /// // or
    /// services.Configure&lt;YTLiveChatOptions&gt;(options => { options.RequestFrequency = 2000; });
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddYTLiveChat(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register core services ONLY
        AddYTLiveChatServices(services);

        return services;
    }

    private static void AddYTLiveChatServices(IServiceCollection services)
    {
        _ = services.AddHttpClient<YTHttpClient>(
            (serviceProvider, httpClient) =>
            {
                // Resolve options when the client is created
                YTLiveChatOptions ytChatOptions =
                    serviceProvider
                        .GetService<IOptions<YTLiveChatOptions>>()
                        ? // Use GetService for optional resolution initially
                        .Value ?? new();

                httpClient.BaseAddress = new Uri(ytChatOptions.YoutubeBaseUrl);

                // httpClient.DefaultRequestHeaders.Add("User-Agent", "YTLiveChatClient/1.0");
            }
        );

        // Register the factory as Singleton
        _ = services.AddSingleton<YTHttpClientFactory>();

        // Register the main service as Transient
        _ = services.AddTransient<IYTLiveChat, YTLiveChat.Services.YTLiveChat>();
    }
}
