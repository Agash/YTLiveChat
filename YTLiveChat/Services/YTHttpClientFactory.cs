using Microsoft.Extensions.DependencyInjection;

namespace YTLiveChat.Services;

internal class YTHttpClientFactory(IServiceProvider serviceProvider)
{
    public YTHttpClient Create() => serviceProvider.GetRequiredService<YTHttpClient>();
}
