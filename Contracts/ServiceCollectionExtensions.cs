using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using YTLiveChat.Common;
using YTLiveChat.Contracts.Services;
using YTLiveChat.Services;

namespace YTLiveChat.Contracts;

/// <summary>
/// Extensions class
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all relevant services as well as the Service backing IYTLiveChat to the ServiceCollection
    /// </summary>
    /// <param name="services">IServiceCollection to add the services to</param>
    /// <param name="context">HostBuilderContext to get relevant Configuration to configure Options (YTLiveChatOptions)</param>
    /// <returns>return IServiceCollection after the services have been added</returns>
    public static IServiceCollection AddYTLiveChat(this IServiceCollection services, HostBuilderContext context)
    {
        _ = services.AddSingleton<IYTLiveChat, YTLiveChat.Services.YTLiveChat>();
        _ = services.AddHttpClient<YTHttpClient>("YouTubeClient", (serviceProvider, httpClient) =>
        {
            var ytChatOptions = serviceProvider.GetRequiredService<IOptions<YTLiveChatOptions>>().Value;
            httpClient.BaseAddress = new Uri(ytChatOptions.YoutubeBaseUrl ?? Constants.YTBaseUrl);
        });
        _ = services.AddSingleton<YTHttpClientFactory>();

        _ = services.Configure<YTLiveChatOptions>(context.Configuration.GetSection(nameof(YTLiveChatOptions)));

        return services;
    }
}
