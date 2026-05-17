using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RetireRight.Core.Configuration;
using RetireRight.Core.Enums;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Scrapers.AntiBot;
using RetireRight.Scrapers.Base;

namespace RetireRight.Scrapers.Sources;

/// <summary>
/// Populates the macro_indicators table with PKR/USD, Brent Crude, and SBP Policy Rate.
///
/// Sources:
///   PKR/USD      — Frankfurter API (free, no key, always fresh)
///   Brent Crude  — Yahoo Finance unofficial v8 API (free, no key)
///   SBP Rate     — sbp.org.pk monetary policy page (HTML, changes ~6x/year)
/// </summary>
public class MacroScraper : BaseScraper
{
    private readonly IMacroRepository _macroRepo;
    private readonly HttpClient _http;

    public override DataSourceType Source => DataSourceType.TradingEconomics; // closest enum for macro
    public override ScrapingMethod Method => ScrapingMethod.Api;

    public MacroScraper(
        ILogger<MacroScraper> logger,
        RequestThrottler throttler,
        RetryPolicy retryPolicy,
        DataSourceOptions sourceConfig,
        IMacroRepository macroRepo)
        : base(logger, throttler, retryPolicy, sourceConfig)
    {
        _macroRepo = macroRepo;
        _http = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip
                | System.Net.DecompressionMethods.Deflate
                | System.Net.DecompressionMethods.Brotli
        });
        _http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        _http.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _http.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        _http.Timeout = TimeSpan.FromSeconds(20);
    }

    protected override async Task<(int scraped, int failed, List<string> errors)> ExecuteScrapeAsync(
        ScrapeRequest request, CancellationToken ct)
    {
        var scraped = 0;
        var failed = 0;
        var errors = new List<string>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // ── PKR/USD ───────────────────────────────────────────────────────────────
        try
        {
            var pkrUsd = await FetchPkrUsdAsync(ct);
            if (pkrUsd.HasValue)
            {
                await _macroRepo.UpsertAsync(new MacroIndicator
                {
                    IndicatorName = "PKR_USD",
                    Date = today,
                    Value = pkrUsd.Value,
                    Unit = "PKR per USD",
                    Source = "Frankfurter",
                    ScrapeRunId = request.ScrapeRunId
                }, ct);
                Logger.LogInformation("PKR/USD: {Rate}", pkrUsd.Value);
                scraped++;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to fetch PKR/USD");
            errors.Add($"PKR_USD: {ex.Message}");
            failed++;
        }

        // ── Brent Crude ───────────────────────────────────────────────────────────
        try
        {
            var brent = await FetchBrentCrudeAsync(ct);
            if (brent.HasValue)
            {
                await _macroRepo.UpsertAsync(new MacroIndicator
                {
                    IndicatorName = "BRENT_CRUDE",
                    Date = today,
                    Value = brent.Value,
                    Unit = "USD per barrel",
                    Source = "Yahoo Finance",
                    ScrapeRunId = request.ScrapeRunId
                }, ct);
                Logger.LogInformation("Brent Crude: {Price}", brent.Value);
                scraped++;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to fetch Brent Crude");
            errors.Add($"BRENT_CRUDE: {ex.Message}");
            failed++;
        }

        // ── SBP Policy Rate ───────────────────────────────────────────────────────
        try
        {
            var sbpRate = await FetchSbpRateAsync(ct);
            if (sbpRate.HasValue)
            {
                await _macroRepo.UpsertAsync(new MacroIndicator
                {
                    IndicatorName = "SBP_RATE",
                    Date = today,
                    Value = sbpRate.Value,
                    Unit = "% per annum",
                    Source = "SBP",
                    ScrapeRunId = request.ScrapeRunId
                }, ct);
                Logger.LogInformation("SBP Policy Rate: {Rate}%", sbpRate.Value);
                scraped++;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to fetch SBP rate");
            errors.Add($"SBP_RATE: {ex.Message}");
            failed++;
        }

        return (scraped, failed, errors);
    }

    private async Task<decimal?> FetchPkrUsdAsync(CancellationToken ct)
    {
        // Open Exchange Rates (free tier, no key): returns all currency rates vs USD
        // PKR is consistently available here
        using var resp = await _http.GetAsync(
            "https://open.er-api.com/v6/latest/USD", ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
        var pkr = json?["rates"]?["PKR"]?.GetValue<decimal>();
        return pkr;
    }

    private async Task<decimal?> FetchBrentCrudeAsync(CancellationToken ct)
    {
        // Yahoo Finance v8 chart API — BZ=F is Brent Crude futures
        using var resp = await _http.GetAsync(
            "https://query1.finance.yahoo.com/v8/finance/chart/BZ%3DF?interval=1d&range=5d", ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);

        // Navigate: result[0].meta.regularMarketPrice
        var price = json?["chart"]?["result"]?[0]?["meta"]?["regularMarketPrice"]?.GetValue<decimal>();
        return price;
    }

    private async Task<decimal?> FetchSbpRateAsync(CancellationToken ct)
    {
        // SBP monetary policy stats page — simpler static page, not Cloudflare-protected
        using var resp = await _http.GetAsync(
            "https://www.sbp.org.pk/m_policy/StatsRel.asp", ct);

        if (!resp.IsSuccessStatusCode)
        {
            Logger.LogWarning("SBP page returned {Status}", resp.StatusCode);
            return null;
        }

        var html = await resp.Content.ReadAsStringAsync(ct);
        var rate = ParseSbpRateFromHtml(html);
        if (!rate.HasValue)
            Logger.LogWarning("SBP policy rate not found in HTML — page structure may have changed or Cloudflare challenge returned");
        return rate;
    }

    private static readonly Regex SbpRatePaPattern =
        new(@"(\d{1,2}(?:\.\d{1,2})?)\s*%\s*p\.a", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

    private static readonly Regex SbpRateGenericPattern =
        new(@"policy\s+rate[^<]{0,200}?(\d{1,2}(?:\.\d{1,2})?)%", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(2));

    private static decimal? ParseSbpRateFromHtml(string html)
    {
        // Primary: look for "12.50% p.a" pattern — SBP uses "p.a" only for the policy rate cell
        var paMatch = SbpRatePaPattern.Match(html);
        if (paMatch.Success &&
            decimal.TryParse(paMatch.Groups[1].Value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var paRate) &&
            paRate >= 1m && paRate <= 40m)
        {
            return paRate;
        }

        // Fallback: "Policy Rate ... XX%"
        var genericMatch = SbpRateGenericPattern.Match(html);
        if (genericMatch.Success &&
            decimal.TryParse(genericMatch.Groups[1].Value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var genericRate) &&
            genericRate >= 1m && genericRate <= 40m)
        {
            return genericRate;
        }

        return null;
    }

    public override async ValueTask DisposeAsync()
    {
        _http.Dispose();
        await base.DisposeAsync();
    }
}
