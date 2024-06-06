using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using YTLiveChat.Contracts.Services;
using YTLiveChat.Services;

namespace YTLiveChat.Contracts;

/// <summary>
/// Extensions class
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all relevant services as well as the Service backing IYTLiveChat to the ServiceCollection and Configures YTLiveChatOptions from appsettings.json
    /// </summary>
    /// <param name="builder">IHostApplicationBuilder to add the services to</param>
    /// <returns>return IHostApplicationBuilder after the services have been added</returns>
    public static IHostApplicationBuilder AddYTLiveChat(this IHostApplicationBuilder builder)
    {
        _ = builder.Services.Configure<YTLiveChatOptions>(builder.Configuration.GetSection(nameof(YTLiveChatOptions)));

        _ = builder.Services.AddSingleton<IYTLiveChat, YTLiveChat.Services.YTLiveChat>();
        _ = builder.Services.AddHttpClient<YTHttpClient>("YouTubeClient", (serviceProvider, httpClient) =>
        {
            YTLiveChatOptions ytChatOptions = serviceProvider.GetRequiredService<IOptions<YTLiveChatOptions>>().Value;
            httpClient.BaseAddress = new Uri(ytChatOptions.YoutubeBaseUrl);
        });
        _ = builder.Services.AddSingleton<YTHttpClientFactory>();

        return builder;
    }
}