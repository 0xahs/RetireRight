using Microsoft.Playwright;

namespace RetireRight.Scrapers.AntiBot;

public static class StealthConfigurator
{
    private static readonly Random Rng = new();

    private static readonly (int Width, int Height)[] Viewports =
    [
        (1920, 1080),
        (1366, 768),
        (1536, 864),
        (1440, 900),
        (1280, 720),
        (1600, 900)
    ];

    public static BrowserNewContextOptions CreateStealthOptions()
    {
        var viewport = Viewports[Rng.Next(Viewports.Length)];

        return new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = viewport.Width, Height = viewport.Height },
            UserAgent = UserAgentRotator.GetRandom(),
            Locale = "en-PK",
            TimezoneId = "Asia/Karachi",
            JavaScriptEnabled = true,
            IgnoreHTTPSErrors = true
        };
    }

    public static async Task ApplyStealthScriptsAsync(IBrowserContext context)
    {
        // Override navigator.webdriver to avoid bot detection
        await context.AddInitScriptAsync(@"
            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
            Object.defineProperty(navigator, 'languages', { get: () => ['en-US', 'en', 'ur'] });

            // Override chrome detection
            window.chrome = { runtime: {} };

            // Override permissions
            const originalQuery = window.navigator.permissions.query;
            window.navigator.permissions.query = (parameters) =>
                parameters.name === 'notifications'
                    ? Promise.resolve({ state: Notification.permission })
                    : originalQuery(parameters);
        ");
    }

    public static async Task SimulateHumanDelayAsync(int minMs = 500, int maxMs = 2000)
    {
        await Task.Delay(Rng.Next(minMs, maxMs));
    }
}
