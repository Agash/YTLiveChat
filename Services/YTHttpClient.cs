using System.Net.Http.Json;
using YTLiveChat.Models;
using YTLiveChat.Models.Response;

namespace YTLiveChat.Services
{
    internal class YTHttpClient(HttpClient httpClient)
    {
        private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        public async Task<GetLiveChatResponse?> GetLiveChatAsync(FetchOptions options)
        {
            string url = $"/youtubei/v1/live_chat/get_live_chat?key={options.ApiKey}";
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, new
            {
                context = new
                {
                    client = new
                    {
                        clientVersion = options.ClientVersion,
                        clientName = "WEB"
                    }
                },
                continuation = options.Continuation,
            });

            _ = response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<GetLiveChatResponse>();
        }

        public async Task<string> GetOptionsAsync(string? handle, string? channelId, string? liveId)
        {
            string url = string.Empty;
            if (!string.IsNullOrEmpty(handle))
            {
                handle = handle.StartsWith('@') ? handle : '@' + handle;
                url = $"/{handle}/live";
            }

            if (!string.IsNullOrEmpty(channelId))
            {
                url = $"/channel/{channelId}/live";
            }

            if (!string.IsNullOrEmpty(liveId))
            {
                url = $"/watch?v={liveId}";
            }

            ArgumentException.ThrowIfNullOrEmpty(url, "LiveID");

            return await _httpClient.GetStringAsync(url);
        }
    }
}
