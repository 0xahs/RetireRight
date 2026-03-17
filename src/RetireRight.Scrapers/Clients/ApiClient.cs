using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RetireRight.Core.Configuration;
using RetireRight.Scrapers.AntiBot;

namespace RetireRight.Scrapers.Clients;

/// <summary>
/// Generic REST API base client with full anti-bot infrastructure:
///   - CookieContainer for session continuity
///   - UserAgent rotation per instance
///   - RequestFingerprint randomization per request
///   - Proxy rotation (optional, from config)
///   - RetryPolicy with exponential backoff
///
/// Source-specific clients (SarmaayaApi, PsxOfficialApi, etc.) extend this
/// and add their own endpoint paths and response parsing.
/// </summary>
public abstract class ApiClient : IStockDataClient
{
    protected readonly HttpClient Http;
    protected readonly RetryPolicy RetryPolicy;
    protected readonly DataSourceOptions SourceConfig;
    protected readonly ILogger Logger;

    private bool _sessionWarmedUp;

    public abstract string Name { get; }
    public abstract int Priority { get; }

    protected ApiClient(
        string baseUrl,
        ILogger logger,
        RetryPolicy retryPolicy,
        DataSourceOptions sourceConfig)
    {
        Logger = logger;
        RetryPolicy = retryPolicy;
        SourceConfig = sourceConfig;

        var proxyRotator = new ProxyRotator(sourceConfig.Proxy);
        var selectedProxy = proxyRotator.GetRandomWebProxy();

        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.All
        };

        if (selectedProxy is not null)
        {
            handler.Proxy = selectedProxy;
            handler.UseProxy = true;
            logger.LogDebug("Using proxy: {Proxy}", selectedProxy.Address);
        }

        Http = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        Http.DefaultRequestHeaders.Add("User-Agent", UserAgentRotator.GetRandom());
        Http.DefaultRequestHeaders.Add("Accept", "application/json");
        Http.DefaultRequestHeaders.Add("Referer", sourceConfig.BaseUrl);
    }

    public abstract Task<Models.ScrapedStockData?> FetchAsync(string symbol, CancellationToken ct);

    /// <summary>
    /// GET a JSON endpoint with fingerprint randomization and retry.
    /// Returns deserialized response + raw JSON, or null on failure.
    /// </summary>
    protected async Task<ApiResult<T>?> GetJsonAsync<T>(
        string endpoint, JsonSerializerOptions jsonOptions, CancellationToken ct)
    {
        try
        {
            return await RetryPolicy.ExecuteAsync(async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                RequestFingerprint.Apply(request);

                var response = await Http.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogDebug("{Name} got {Status} from {Endpoint}",
                        Name, response.StatusCode, endpoint);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                var parsed = JsonSerializer.Deserialize<T>(json, jsonOptions);

                return new ApiResult<T>
                {
                    Data = parsed,
                    RawJson = json,
                    StatusCode = (int)response.StatusCode
                };
            }, SourceConfig.Retry, $"{Name}:{endpoint}", ct);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "{Name} failed to fetch {Endpoint}", Name, endpoint);
            return null;
        }
    }

    /// <summary>
    /// Warms up the session by hitting a known-good endpoint to collect cookies.
    /// Call once before the first real request. Override to customize.
    /// </summary>
    protected async Task WarmUpSessionAsync(string warmUpEndpoint, CancellationToken ct)
    {
        if (_sessionWarmedUp) return;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, warmUpEndpoint);
            RequestFingerprint.Apply(request);
            await Http.SendAsync(request, ct);
            _sessionWarmedUp = true;
            Logger.LogDebug("{Name} session warmed up", Name);
        }
        catch
        {
            _sessionWarmedUp = true; // Don't retry every symbol
        }
    }

    public virtual ValueTask DisposeAsync()
    {
        Http.Dispose();
        return ValueTask.CompletedTask;
    }
}

/// <summary>Wraps a deserialized API response with the raw JSON for archival.</summary>
public class ApiResult<T>
{
    public T? Data { get; init; }
    public string? RawJson { get; init; }
    public int StatusCode { get; init; }
}
