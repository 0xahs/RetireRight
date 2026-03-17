using System.Net;
using RetireRight.Core.Configuration;

namespace RetireRight.Scrapers.AntiBot;

/// <summary>
/// Selects a proxy from a configured list. Defeats IP-based rate limits and bans.
/// Each scraper instance picks a random proxy at construction time.
/// When no proxies are configured, all methods return null (direct connection).
/// </summary>
public class ProxyRotator
{
    private readonly List<ProxyEntry> _proxies = [];
    private readonly Random _rng = new();

    public ProxyRotator(ProxyOptions? options)
    {
        if (options is not { Enabled: true } || options.Urls.Count == 0) return;

        foreach (var url in options.Urls)
        {
            _proxies.Add(new ProxyEntry
            {
                Url = url,
                Username = options.Username,
                Password = options.Password
            });
        }
    }

    public bool HasProxies => _proxies.Count > 0;

    /// <summary>
    /// Returns a random WebProxy for HttpClient, or null if no proxies configured.
    /// </summary>
    public WebProxy? GetRandomWebProxy()
    {
        if (_proxies.Count == 0) return null;

        var entry = _proxies[_rng.Next(_proxies.Count)];
        var proxy = new WebProxy(entry.Url);

        if (entry.Username is not null)
            proxy.Credentials = new NetworkCredential(entry.Username, entry.Password);

        return proxy;
    }

    /// <summary>
    /// Returns proxy config for Playwright browser context, or null if no proxies configured.
    /// </summary>
    public Microsoft.Playwright.Proxy? GetRandomPlaywrightProxy()
    {
        if (_proxies.Count == 0) return null;

        var entry = _proxies[_rng.Next(_proxies.Count)];
        return new Microsoft.Playwright.Proxy
        {
            Server = entry.Url,
            Username = entry.Username,
            Password = entry.Password
        };
    }

    private class ProxyEntry
    {
        public required string Url { get; init; }
        public string? Username { get; init; }
        public string? Password { get; init; }
    }
}
