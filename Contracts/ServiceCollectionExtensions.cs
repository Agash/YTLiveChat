using Microsoft.Extensions.DependencyInjection;
using YTLiveChat.Common;
using YTLiveChat.Contracts.Services;
using YTLiveChat.Services;

namespace YTLiveChat.Contracts
{
    /// <summary>
    /// Extensions class
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds all relevant services as well as the Service backing IYTLiveChat to the ServiceCollection
        /// </summary>
        /// <param name="services">IServiceCollection to add the services to</param>
        /// <returns>return IServiceCollection after the services have been added</returns>
        public static IServiceCollection AddYTLiveChat(this IServiceCollection services)
        {
            _ = services.AddSingleton<IYTLiveChat, YTLiveChat.Services.YTLiveChat>();
            _ = services.AddHttpClient<YTHttpClient>("YouTubeClient", x => { x.BaseAddress = new Uri(Constants.YTBaseUrl); });
            _ = services.AddSingleton<YTHttpClientFactory>();

            return services;
        }
    }
}
