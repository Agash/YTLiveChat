using Microsoft.Extensions.DependencyInjection;

namespace YTLiveChat.Services
{
    internal class YTHttpClientFactory(IServiceProvider serviceProvider)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public YTHttpClient Create()
        {
            return _serviceProvider.GetRequiredService<YTHttpClient>();
        }
    }
}
