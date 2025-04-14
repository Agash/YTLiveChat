using System.Net.Http.Json;
using System.Text.Json;

using YTLiveChat.Models;
using YTLiveChat.Models.Response;

namespace YTLiveChat.Services;

internal class YTHttpClient(HttpClient httpClient) : IDisposable
{
    private readonly HttpClient _httpClient =
        httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Fetches the live chat data from YouTube.
    /// </summary>
    /// <param name="options">Fetch options containing API key, continuation token, etc.</param>
    /// <returns>A tuple containing the deserialized response and the raw JSON string, or (null, null) on error.</returns>
    public async Task<(LiveChatResponse? Response, string? RawJson)> GetLiveChatAsync(
        FetchOptions options
    )
    {
        string url = $"/youtubei/v1/live_chat/get_live_chat?key={options.ApiKey}";
        string? rawJson = null;
        try
        {
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
                url,
                new
                {
                    context = new
                    {
                        client = new { clientVersion = options.ClientVersion, clientName = "WEB" },
                    },
                    continuation = options.Continuation,
                }
            );

            _ = response.EnsureSuccessStatusCode();

            // Read the raw content first
            rawJson = await response.Content.ReadAsStringAsync();

            // Then deserialize
            // Consider adding more robust error handling for deserialization
            LiveChatResponse? responseObject = JsonSerializer.Deserialize<LiveChatResponse>(
                rawJson,
                s_jsonOptions
            );
            return (responseObject, rawJson);
        }
        catch (HttpRequestException httpEx)
        {
            // Log specific HTTP errors if needed
            // Example: Log the status code if available
            Console.Error.WriteLine(
                $"HTTP Request Error in GetLiveChatAsync: {httpEx.StatusCode} - {httpEx.Message}"
            );
            return (null, null);
        }
        catch (JsonException jsonEx)
        {
            // Log deserialization errors
            Console.Error.WriteLine(
                $"JSON Deserialization Error in GetLiveChatAsync: {jsonEx.Message}"
            );
            Console.Error.WriteLine($"Raw JSON causing error (if available): {rawJson ?? "N/A"}");
            return (null, rawJson); // Return raw JSON even if deserialization fails
        }
        catch (Exception ex)
        {
            // Log generic errors
            Console.Error.WriteLine($"Generic Error in GetLiveChatAsync: {ex.Message}");
            return (null, rawJson); // Return raw JSON if available
        }
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

        ArgumentException.ThrowIfNullOrEmpty(url, "handle, channelId, or liveId"); // Ensure one is provided

        return await _httpClient.GetStringAsync(url);
    }

    public void Dispose() => ((IDisposable)_httpClient).Dispose();
}
