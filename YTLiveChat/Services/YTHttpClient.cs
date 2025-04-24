using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using YTLiveChat.Models;
using YTLiveChat.Models.Response;

namespace YTLiveChat.Services;

internal class YTHttpClient(HttpClient httpClient, ILogger<YTHttpClient> logger) : IDisposable
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Fetches the live chat data from YouTube.
    /// </summary>
    /// <param name="options">Fetch options containing API key, continuation token, etc.</param>
    /// <param name="cancellationToken">Cancellation token to be passed to the HttpClient</param>
    /// <returns>A tuple containing the deserialized response and the raw JSON string, or (null, null) on error.</returns>
    public async Task<(LiveChatResponse? Response, string? RawJson)> GetLiveChatAsync(
        FetchOptions options, CancellationToken cancellationToken = default
    )
    {
        string url = $"/youtubei/v1/live_chat/get_live_chat?key={options.ApiKey}";
        string? rawJson = null;
        try
        {
            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(url, new
            {
                context = new
                {
                    client = new { clientVersion = options.ClientVersion, clientName = "WEB" },
                },
                continuation = options.Continuation,
            }
, cancellationToken: cancellationToken);

            _ = response.EnsureSuccessStatusCode();

            // Read the raw content first
            rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

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
            logger.LogError(
                httpEx,
                "HTTP Request Error in GetLiveChatAsync: {StatusCode} - {Message}",
                httpEx.StatusCode,
                httpEx.Message
            );
            return (null, null);
        }
        catch (JsonException jsonEx)
        {
            // Log deserialization errors
            logger.LogError(
                jsonEx,
                "JSON Deserialization Error in GetLiveChatAsync: {Message}/n {RawJSON}",
                jsonEx.Message,
                rawJson ?? "N/A"
            );
            return (null, rawJson); // Return raw JSON even if deserialization fails
        }
        catch (Exception ex)
        {
            // Log generic errors
            logger.LogCritical(
                ex,
                "Generic Error in GetLiveChatAsync: {Message}",
                ex.Message
            );
            return (null, rawJson); // Return raw JSON if available
        }
    }

    public async Task<string> GetOptionsAsync(string? handle, string? channelId, string? liveId, CancellationToken cancellationToken = default)
    {
        string url;
        if (!string.IsNullOrEmpty(handle))
        {
            handle = handle.StartsWith('@') ? handle : '@' + handle;
            url = $"/{handle}/live";
        }
        else
        {
            url = !string.IsNullOrEmpty(channelId)
                ? $"/channel/{channelId}/live"
                : !string.IsNullOrEmpty(liveId)
                            ? $"/watch?v={liveId}"
                            : throw new ArgumentException("At least one identifier (handle, channelId, or liveId) must be provided.");
        }

        return await httpClient.GetStringAsync(url, cancellationToken);
    }

    public void Dispose() => httpClient.Dispose();
}
