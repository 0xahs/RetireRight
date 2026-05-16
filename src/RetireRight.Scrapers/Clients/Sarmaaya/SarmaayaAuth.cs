using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RetireRight.Core.Configuration;
using RetireRight.Scrapers.AntiBot;

namespace RetireRight.Scrapers.Clients.Sarmaaya;

public static class SarmaayaAuth
{
    private const string LoginUrl = "https://sarmaaya.pk/auth/login";

    public static async Task<SarmaayaTokenCache?> GetTokenAsync(
        CredentialsOptions? credentials,
        ILogger logger,
        CancellationToken ct)
    {
        if (credentials?.Email is null || credentials?.Password is null)
        {
            logger.LogWarning("Sarmaaya credentials not configured — financial endpoints will return 401");
            return null;
        }

        var cached = SarmaayaTokenCache.Load(logger);
        if (cached?.IsValid == true)
        {
            logger.LogDebug("Using cached Sarmaaya token (valid until {ExpiresAt})",
                DateTimeOffset.FromUnixTimeSeconds(cached.ExpiresAt).LocalDateTime);
            return cached;
        }

        logger.LogInformation("Sarmaaya token missing or expired — logging in via browser");
        return await LoginAsync(credentials, logger, ct);
    }

    private static async Task<SarmaayaTokenCache?> LoginAsync(
        CredentialsOptions credentials, ILogger logger, CancellationToken ct)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = ["--disable-blink-features=AutomationControlled"]
        });

        var contextOptions = StealthConfigurator.CreateStealthOptions();
        await using var context = await browser.NewContextAsync(contextOptions);
        await StealthConfigurator.ApplyStealthScriptsAsync(context);

        var page = await context.NewPageAsync();
        try
        {
            logger.LogDebug("Navigating to Sarmaaya login page");
            await page.GotoAsync(LoginUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30000
            });
            // Wait for the email input to be ready (SPA may render it after DOMContentLoaded)
            await page.WaitForSelectorAsync("input[type='email'], input[name='email']",
                new PageWaitForSelectorOptions { Timeout = 15000 });

            await page.FillAsync("input[type='email'], input[name='email']", credentials.Email!);
            await StealthConfigurator.SimulateHumanDelayAsync(400, 800);

            await page.FillAsync("input[type='password'], input[name='password']", credentials.Password!);
            await StealthConfigurator.SimulateHumanDelayAsync(400, 800);

            await page.ClickAsync("button[type='submit']");

            // Wait until we're redirected away from the login page
            await page.WaitForURLAsync(
                url => !url.Contains("/auth/login"),
                new PageWaitForURLOptions { Timeout = 15000 });

            await StealthConfigurator.SimulateHumanDelayAsync(1000, 2000);

            // The Supabase auth cookie is URL-encoded JSON
            var cookies = await context.CookiesAsync(["https://sarmaaya.pk"]);
            var authCookie = cookies.FirstOrDefault(c => c.Name.StartsWith("sb-") && c.Name.EndsWith("-auth-token"));

            if (authCookie is null)
            {
                logger.LogWarning("Sarmaaya auth cookie not found after login — login may have failed");
                return null;
            }

            var token = ParseAuthCookie(authCookie.Value, logger);
            if (token is null)
            {
                logger.LogWarning("Could not parse Sarmaaya auth cookie (name: {Name})", authCookie.Name);
                return null;
            }

            token.Save();
            logger.LogInformation("Sarmaaya login succeeded — token cached until {ExpiresAt}",
                DateTimeOffset.FromUnixTimeSeconds(token.ExpiresAt).LocalDateTime);

            return token;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Sarmaaya browser login failed");
            return null;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static SarmaayaTokenCache? ParseAuthCookie(string cookieValue, ILogger logger)
    {
        try
        {
            var decoded = Uri.UnescapeDataString(cookieValue);
            using var doc = JsonDocument.Parse(decoded);
            var root = doc.RootElement;

            var accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
            var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;

            long expiresAt = 0;
            if (root.TryGetProperty("expires_at", out var ea) && ea.ValueKind == JsonValueKind.Number)
                expiresAt = ea.GetInt64();
            else if (root.TryGetProperty("expires_in", out var ei) && ei.ValueKind == JsonValueKind.Number)
                expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ei.GetInt64();

            if (accessToken is null || expiresAt == 0)
                return null;

            return new SarmaayaTokenCache
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse Sarmaaya auth cookie — cookie format may have changed");
            return null;
        }
    }
}
