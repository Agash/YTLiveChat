using System.Text.Json;
using System.Net;

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

#if DEBUG
    private const string PrettyPrintQueryValue = "true";
#else
    private const string PrettyPrintQueryValue = "false";
#endif

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
    public virtual async Task<(LiveChatResponse? Response, string? RawJson)> GetLiveChatAsync(
        FetchOptions options,
        CancellationToken cancellationToken = default
    )
    {
        // HttpClient.BaseAddress is expected to be set (e.g., "https://www.youtube.com")
        string url =
            $"/youtubei/v1/live_chat/get_live_chat?key={options.ApiKey}&prettyPrint={PrettyPrintQueryValue}";
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

#if NETSTANDARD2_1 || NETSTANDARD2_0
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
#if NETSTANDARD2_1 || NETSTANDARD2_0
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
    public virtual async Task<string> GetOptionsAsync(
        string? handle,
        string? channelId,
        string? liveId,
        CancellationToken cancellationToken = default
    )
    {
        string urlPath = BuildLivePagePath(handle, channelId, liveId);
        return await GetPageHtmlWithConsentFallbackAsync(urlPath, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Fetches the streams tab HTML page for a given channel handle or ID.
    /// </summary>
    public virtual async Task<string> GetStreamsPageAsync(
        string? handle,
        string? channelId,
        CancellationToken cancellationToken = default
    )
    {
        string urlPath = BuildStreamsPagePath(handle, channelId);
        return await GetPageHtmlWithConsentFallbackAsync(urlPath, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<string> GetPageHtmlWithConsentFallbackAsync(
        string urlPath,
        CancellationToken cancellationToken
    )
    {
        // Always fetch channel/watch pages with a stateless client to avoid cookie-driven
        // consent interstitial loops between monitor probes.
        string html = await GetStringStatelessAsync(urlPath, cancellationToken).ConfigureAwait(false);
        if (!IsConsentInterstitialPage(html))
        {
            return html;
        }

        _logger.LogWarning(
            "Stateless fetch returned YouTube consent interstitial for {UrlPath}. Retrying with configured HttpClient.",
            urlPath
        );

        string fallbackHtml = await GetStringAsync(urlPath, cancellationToken)
            .ConfigureAwait(false);
        if (!IsConsentInterstitialPage(fallbackHtml))
        {
            return fallbackHtml;
        }

        _logger.LogWarning(
            "Configured HttpClient fallback still returned consent interstitial for {UrlPath}.",
            urlPath
        );
        return fallbackHtml;
    }

    private async Task<string> GetStringAsync(string urlPath, CancellationToken cancellationToken)
    {
        // HttpClient.GetStringAsync will combine BaseAddress and urlPath.
#if NETSTANDARD2_1 || NETSTANDARD2_0
        return await _httpClient.GetStringAsync(urlPath).ConfigureAwait(false);
#else
        return await _httpClient.GetStringAsync(urlPath, cancellationToken).ConfigureAwait(false);
#endif
    }

    private async Task<string> GetStringStatelessAsync(
        string urlPath,
        CancellationToken cancellationToken
    )
    {
        HttpClientHandler handler = new()
        {
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        };

        using HttpClient statelessClient = new(handler)
        {
            BaseAddress = _httpClient.BaseAddress,
            Timeout = _httpClient.Timeout,
        };

        using HttpRequestMessage request = new(HttpMethod.Get, urlPath);
#if NETSTANDARD2_1 || NETSTANDARD2_0
        using HttpResponseMessage response = await statelessClient
            .SendAsync(request)
            .ConfigureAwait(false);
#else
        using HttpResponseMessage response = await statelessClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
#endif
        response.EnsureSuccessStatusCode();
#if NETSTANDARD2_1 || NETSTANDARD2_0
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#else
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#endif
    }

    private static bool IsConsentInterstitialPage(string html) =>
        html.Contains("consent.youtube.com", StringComparison.OrdinalIgnoreCase)
        || html.Contains("Before you continue to YouTube", StringComparison.OrdinalIgnoreCase);

    private static string BuildLivePagePath(string? handle, string? channelId, string? liveId)
    {
        if (!string.IsNullOrEmpty(handle))
        {
            string normalizedHandle = handle!.StartsWith("@", StringComparison.Ordinal)
                ? handle
                : '@' + handle;
            return $"/{normalizedHandle}/live";
        }

        return !string.IsNullOrEmpty(channelId) ? $"/channel/{channelId}/live"
            : !string.IsNullOrEmpty(liveId) ? $"/watch?v={liveId}"
            : throw new ArgumentException(
                "At least one identifier (handle, channelId, or liveId) must be provided."
            );
    }

    private static string BuildStreamsPagePath(string? handle, string? channelId)
    {
        if (!string.IsNullOrEmpty(handle))
        {
            string normalizedHandle = handle!.StartsWith("@", StringComparison.Ordinal)
                ? handle
                : '@' + handle;
            return $"/{normalizedHandle}/streams";
        }

        return !string.IsNullOrEmpty(channelId) ? $"/channel/{channelId}/streams"
            : throw new ArgumentException(
                "A channel handle or channelId must be provided."
            );
    }
}

