using RetireRight.Core.Configuration;
using RetireRight.Core.DTOs;
using RetireRight.Core.Enums;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Scrapers.AntiBot;
using RetireRight.Scrapers.Sources;

namespace RetireRight.Api.Endpoints;

public static class MacroEndpoints
{
    public static void MapMacroEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/macro").WithTags("Macro");

        group.MapGet("/", GetMacroAsync).WithName("GetMacro");
        group.MapPost("/refresh", RefreshMacroAsync).WithName("RefreshMacro");
    }

    private static async Task<IResult> GetMacroAsync(
        IMacroRepository macroRepo,
        IIndexLevelRepository indexRepo,
        CancellationToken ct)
    {
        var kse100 = await indexRepo.GetLatestAsync("KSE100", ct);
        var pkrUsd = await macroRepo.GetLatestAsync("PKR_USD", ct);
        var brent = await macroRepo.GetLatestAsync("BRENT_CRUDE", ct);
        var sbpRate = await macroRepo.GetLatestAsync("SBP_RATE", ct);
        var cpi = await macroRepo.GetLatestAsync("CPI", ct);

        return Results.Ok(new MacroDto(
            Kse100Level: kse100?.Close,
            Kse100ChangePct: kse100?.ChangePct,
            SbpRate: sbpRate?.Value,
            PkrUsd: pkrUsd?.Value,
            BrentCrude: brent?.Value,
            Cpi: cpi?.Value,
            AsOf: DateTime.UtcNow));
    }

    /// <summary>
    /// Runs only the MacroScraper — fast (&lt;5s), no stock scraping.
    /// Designed for manual refresh of PKR/USD, Brent Crude, and SBP rate.
    /// </summary>
    private static async Task<IResult> RefreshMacroAsync(
        IMacroRepository macroRepo,
        ILoggerFactory loggerFactory,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var rrOptions = configuration.GetSection(RetireRightOptions.SectionName).Get<RetireRightOptions>()
            ?? new RetireRightOptions();

        var macroConfig = rrOptions.DataSources.FirstOrDefault(ds =>
            ds.Type.Equals("Macro", StringComparison.OrdinalIgnoreCase))
            ?? new DataSourceOptions
            {
                Name = "Macro",
                Type = "Macro",
                Enabled = true,
                RateLimit = new RateLimitOptions { MaxRequestsPerMinute = 10, MinDelayMs = 500, MaxDelayMs = 1000 },
                Retry = new RetryOptions { MaxAttempts = 2, InitialDelayMs = 1000, BackoffMultiplier = 2 }
            };

        var throttler = new RequestThrottler(loggerFactory.CreateLogger<RequestThrottler>());
        var retryPolicy = new RetryPolicy(loggerFactory.CreateLogger<RetryPolicy>());

        await using var scraper = new MacroScraper(
            loggerFactory.CreateLogger<MacroScraper>(),
            throttler,
            retryPolicy,
            macroConfig,
            macroRepo);

        var result = await scraper.ScrapeAsync(new RetireRight.Core.Interfaces.ScrapeRequest(), ct);

        // Return fresh data immediately
        var pkrUsd = await macroRepo.GetLatestAsync("PKR_USD", ct);
        var brent = await macroRepo.GetLatestAsync("BRENT_CRUDE", ct);
        var sbpRate = await macroRepo.GetLatestAsync("SBP_RATE", ct);

        return Results.Ok(new
        {
            scraped = result.RecordsScraped,
            failed = result.RecordsFailed,
            errors = result.Errors,
            data = new MacroDto(
                Kse100Level: null,
                Kse100ChangePct: null,
                SbpRate: sbpRate?.Value,
                PkrUsd: pkrUsd?.Value,
                BrentCrude: brent?.Value,
                Cpi: null,
                AsOf: DateTime.UtcNow)
        });
    }
}
