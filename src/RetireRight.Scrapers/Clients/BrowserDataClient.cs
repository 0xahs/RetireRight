using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RetireRight.Core.Configuration;
using RetireRight.Scrapers.AntiBot;

namespace RetireRight.Scrapers.Clients;

/// <summary>
/// Generic Playwright browser base client with full stealth infrastructure:
///   - StealthConfigurator (webdriver spoof, viewport randomization, locale)
///   - Proxy rotation per browser context (optional, from config)
///   - Human delay simulation
///   - API response interception (captures XHR calls the SPA makes internally)
///
/// Source-specific clients (SarmaayaBrowser, PsxOfficialBrowser, etc.) extend this
/// and add their own page navigation, tab clicking, and parsing logic.
/// </summary>
public abstract class BrowserDataClient : IStockDataClient
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    protected readonly DataSourceOptions SourceConfig;
    protected readonly ILogger Logger;
    private readonly ProxyRotator _proxyRotator;

    public abstract string Name { get; }
    public abstract int Priority { get; }

    protected BrowserDataClient(
        ILogger logger,
        DataSourceOptions sourceConfig)
    {
        Logger = logger;
        SourceConfig = sourceConfig;
        _proxyRotator = new ProxyRotator(sourceConfig.Proxy);
    }

    public abstract Task<Models.ScrapedStockData?> FetchAsync(string symbol, CancellationToken ct);

    /// <summary>
    /// Creates a stealthed browser context with optional proxy.
    /// Caller should dispose the context when done.
    /// </summary>
    protected async Task<IBrowserContext> CreateContextAsync()
    {
        _playwright ??= await Playwright.CreateAsync();
        _browser ??= await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = ["--disable-blink-features=AutomationControlled"]
        });

        var options = StealthConfigurator.CreateStealthOptions();
        var proxy = _proxyRotator.GetRandomPlaywrightProxy();
        if (proxy is not null)
            options.Proxy = proxy;

        var context = await _browser.NewContextAsync(options);
        await StealthConfigurator.ApplyStealthScriptsAsync(context);

        return context;
    }

    /// <summary>
    /// Navigate to a URL with NetworkIdle wait and human delay simulation.
    /// Returns the page + response, or (page, null) on failure.
    /// Caller must close the page.
    /// </summary>
    protected async Task<(IPage page, IResponse? response)> NavigateAsync(
        IBrowserContext context, string url, int timeoutMs = 30000)
    {
        var page = await context.NewPageAsync();

        var response = await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = timeoutMs
        });

        await StealthConfigurator.SimulateHumanDelayAsync(1000, 3000);
        return (page, response);
    }

    /// <summary>
    /// Register an interceptor that captures API responses matching a URL pattern.
    /// Call this BEFORE navigating. The returned list fills as matching responses arrive.
    /// </summary>
    protected static List<(string url, string body)> InterceptApiCalls(IPage page, string urlPattern)
    {
        var captured = new List<(string url, string body)>();

        page.Response += async (_, resp) =>
        {
            if (resp.Url.Contains(urlPattern) && resp.Status == 200)
            {
                try
                {
                    var body = await resp.TextAsync();
                    captured.Add((resp.Url, body));
                }
                catch { /* response body may not be available */ }
            }
        };

        return captured;
    }

    /// <summary>Get the full page HTML for raw archival.</summary>
    protected static async Task<string> GetPageHtmlAsync(IPage page)
    {
        return await page.ContentAsync();
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_browser is not null)
            await _browser.DisposeAsync();
        _playwright?.Dispose();
    }
}
