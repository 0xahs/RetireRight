using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RetireRight.Core.Configuration;
using RetireRight.Core.Enums;
using RetireRight.Scrapers.AntiBot;

namespace RetireRight.Scrapers.Base;

public abstract class BrowserScraper : BaseScraper
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public override ScrapingMethod Method => ScrapingMethod.Browser;

    protected BrowserScraper(
        ILogger logger,
        RequestThrottler throttler,
        RetryPolicy retryPolicy,
        DataSourceOptions sourceConfig)
        : base(logger, throttler, retryPolicy, sourceConfig)
    {
    }

    protected async Task<IBrowserContext> GetBrowserContextAsync(Proxy? proxy = null)
    {
        _playwright ??= await Playwright.CreateAsync();
        _browser ??= await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = ["--disable-blink-features=AutomationControlled"]
        });

        var options = StealthConfigurator.CreateStealthOptions();
        if (proxy is not null)
            options.Proxy = proxy;

        var context = await _browser.NewContextAsync(options);
        await StealthConfigurator.ApplyStealthScriptsAsync(context);

        return context;
    }

    protected async Task<T?> CaptureApiResponseAsync<T>(IPage page, string urlPattern, Func<Task> triggerAction)
    {
        var tcs = new TaskCompletionSource<string>();

        page.Response += async (_, response) =>
        {
            if (response.Url.Contains(urlPattern) && response.Status == 200)
            {
                try
                {
                    var body = await response.TextAsync();
                    tcs.TrySetResult(body);
                }
                catch
                {
                    // Response body may not be available
                }
            }
        };

        await triggerAction();

        // Wait up to 15 seconds for the API response
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            var json = await tcs.Task.WaitAsync(cts.Token);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Timeout waiting for API response matching pattern: {Pattern}", urlPattern);
            return default;
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if (_browser is not null)
            await _browser.DisposeAsync();
        _playwright?.Dispose();
    }
}
