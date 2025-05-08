using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YTLiveChat.Models;
using YTLiveChat.Models.Response;

namespace YTLiveChat.Services;

/// <summary>
/// Client for making HTTP requests to YouTube's internal InnerTube API endpoints for live chat.
/// </summary>
/// <param name="httpClient">An instance of <see cref="HttpClient"/> configured with the base YouTube URL. Its lifetime should be managed externally.</param>
/// <param name="logger">An optional logger instance.</param>
/// <exception cref="ArgumentNullException">Thrown if httpClient is null.</exception>
public class YTHttpClient(HttpClient httpClient, ILogger<YTHttpClient>? logger = null)
{
    private readonly HttpClient _httpClient =
        httpClient
        ?? throw new ArgumentNullException(nameof(httpClient), "HttpClient cannot be null.");
    private readonly ILogger<YTHttpClient> _logger = logger ?? NullLogger<YTHttpClient>.Instance;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Fetches the latest live chat messages and continuation token based on the provided options.
    /// </summary>
    /// <param name="options">The fetch options containing API key, context, and continuation token.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A tuple containing the deserialized <see cref="LiveChatResponse"/> and the raw JSON string.
    /// Returns (null, rawJsonWithError) if an HTTP error (non-2xx) occurs after logging it.
    /// Returns (null, rawJsonWithError) if JSON deserialization fails after logging it.
    /// Returns (null, null) for other critical exceptions after logging them.
    /// </returns>
    public async Task<(LiveChatResponse? Response, string? RawJson)> GetLiveChatAsync(
        FetchOptions options,
        CancellationToken cancellationToken = default
    )
    {
        // HttpClient.BaseAddress is expected to be set (e.g., "https://www.youtube.com")
        string url = $"/youtubei/v1/live_chat/get_live_chat?key={options.ApiKey}";
        string? rawJson = null;
        try
        {
            // The PostAsJsonAsync extension method might not be available if Microsoft.Extensions.Http is removed from the core.
            // We need to use standard HttpClient methods.
            var payload = new
            {
                context = new
                {
                    client = new { clientVersion = options.ClientVersion, clientName = "WEB" },
                },
                continuation = options.Continuation,
            };
            string jsonPayload = JsonSerializer.Serialize(payload);
            using StringContent content = new(
                jsonPayload,
                System.Text.Encoding.UTF8,
                "application/json"
            );

            using HttpResponseMessage response = await _httpClient
                .PostAsync(url, content, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "HTTP Request Error in GetLiveChatAsync: {StatusCode} - {ReasonPhrase}. URL: {Url}",
                    response.StatusCode,
                    response.ReasonPhrase,
                    url
                );
                // Optionally read content for more details if not success
                // string errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                // _logger.LogDebug("Error content: {ErrorContent}", errorContent);
                response.EnsureSuccessStatusCode(); // This will throw HttpRequestException
            }

#if NETSTANDARD2_1
            rawJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#else
            rawJson = await response
                .Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
#endif
            LiveChatResponse? responseObject = JsonSerializer.Deserialize<LiveChatResponse>(
                rawJson,
                s_jsonOptions
            );
            return (responseObject, rawJson);
        }
        catch (HttpRequestException httpEx)
        {
#if NETSTANDARD2_1
            // StatusCode not available, log message only
            _logger.LogError(
                httpEx,
                "HTTP Request Error in GetLiveChatAsync: {Message}. URL: {Url}",
                httpEx.Message,
                url
            );
#else
            _logger.LogError(
                httpEx,
                "HTTP Request Error in GetLiveChatAsync: {StatusCode} - {Message}. URL: {Url}",
                httpEx.StatusCode,
                httpEx.Message,
                url
            );
#endif
            return (null, rawJson); // Return rawJson if available, even on error
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(
                jsonEx,
                "JSON Deserialization Error in GetLiveChatAsync: {Message}. RawJSON: {RawJSON}",
                jsonEx.Message,
                rawJson ?? "N/A"
            );
            return (null, rawJson);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogCritical(
                ex,
                "Generic Error in GetLiveChatAsync: {Message}. URL: {Url}",
                ex.Message,
                url
            );
            return (null, rawJson);
        }
    }

    /// <summary>
    /// Fetches the initial HTML page for a given channel handle, ID, or video ID to extract initial chat options.
    /// </summary>
    /// <param name="handle">The channel handle (e.g., "@ChannelHandle").</param>
    /// <param name="channelId">The channel ID (e.g., "UCxxxxxxxxxxxxxxx").</param>
    /// <param name="liveId">The video ID of the live stream.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The raw HTML content of the page.</returns>
    /// <exception cref="ArgumentException">Thrown if none of handle, channelId, or liveId are provided.</exception>
    public async Task<string> GetOptionsAsync(
        string? handle,
        string? channelId,
        string? liveId,
        CancellationToken cancellationToken = default
    )
    {
        string urlPath; // Relative path
        if (!string.IsNullOrEmpty(handle))
        {
            handle = handle.StartsWith('@') ? handle : '@' + handle;
            urlPath = $"/{handle}/live";
        }
        else
        {
            urlPath =
                !string.IsNullOrEmpty(channelId) ? $"/channel/{channelId}/live"
                : !string.IsNullOrEmpty(liveId) ? $"/watch?v={liveId}"
                : throw new ArgumentException(
                    "At least one identifier (handle, channelId, or liveId) must be provided."
                );
        }
        // HttpClient.GetStringAsync will combine BaseAddress and urlPath
#if NETSTANDARD2_1
        return await _httpClient.GetStringAsync(urlPath).ConfigureAwait(false);
#else
        return await _httpClient.GetStringAsync(urlPath, cancellationToken).ConfigureAwait(false);
#endif
    }
}
