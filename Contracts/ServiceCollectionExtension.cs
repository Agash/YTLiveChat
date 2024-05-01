using Microsoft.Extensions.DependencyInjection;
using YTLiveChat.Common;
using YTLiveChat.Contracts.Services;
using YTLiveChat.Services;

namespace YTLiveChat.Contracts
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddYTLiveChat(this IServiceCollection services)
        {
            _ = services.AddSingleton<IYTLiveChat, YTLiveChat.Services.YTLiveChat>();
            _ = services.AddHttpClient<YTHttpClient>("YouTubeClient", x => { x.BaseAddress = new Uri(Constants.YTBaseUrl); });
            _ = services.AddSingleton<YTHttpClientFactory>();
            
            return services;
        }
    }
}
