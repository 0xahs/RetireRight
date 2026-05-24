using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RetireRight.Core.Attributes;
using RetireRight.Core.Configuration;
using RetireRight.Core.Enums;
using RetireRight.Core.Interfaces;
using RetireRight.Scrapers.AntiBot;

namespace RetireRight.Scrapers.Base;

public abstract class BaseScraper : IScraper
{
    protected readonly ILogger Logger;
    protected readonly RequestThrottler Throttler;
    protected readonly RetryPolicy RetryPolicy;
    protected readonly DataSourceOptions SourceConfig;

    private readonly DataSourceAttribute _meta;

    public string Source => _meta.Identifier;
    public bool IsPerSymbol => _meta.PerSymbol;

    public abstract ScrapingMethod Method { get; }

    protected BaseScraper(
        ILogger logger,
        RequestThrottler throttler,
        RetryPolicy retryPolicy,
        DataSourceOptions sourceConfig)
    {
        Logger = logger;
        Throttler = throttler;
        RetryPolicy = retryPolicy;
        SourceConfig = sourceConfig;

        _meta = GetType().GetCustomAttributes(typeof(DataSourceAttribute), inherit: false)
                        .Cast<DataSourceAttribute>()
                        .FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"{GetType().Name} must be decorated with [DataSource(\"...\")] — this is the only place the type identifier should live.");
    }

    public async Task<ScrapeResult> ScrapeAsync(ScrapeRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var errors = new List<string>();
        var scraped = 0;
        var failed = 0;

        Logger.LogInformation("Starting scrape from {Source} for {Count} symbols",
            Source, request.Symbols.Length == 0 ? "all" : request.Symbols.Length);

        try
        {
            (scraped, failed, errors) = await ExecuteScrapeAsync(request, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Scrape failed for {Source}", Source);
            errors.Add(ex.Message);
        }

        sw.Stop();

        var (followUpSource, followUpSymbols) = GetFollowUp();
        var result = new ScrapeResult
        {
            Source = Source,
            Success = failed == 0 && errors.Count == 0,
            RecordsScraped = scraped,
            RecordsFailed = failed,
            Duration = sw.Elapsed,
            Errors = errors,
            FollowUpSource = followUpSource,
            FollowUpSymbols = followUpSymbols,
        };

        Logger.LogInformation(
            "Scrape completed: {Source} | {Scraped} records | {Failed} failed | {Duration}ms",
            Source, scraped, failed, sw.ElapsedMilliseconds);

        return result;
    }

    protected abstract Task<(int scraped, int failed, List<string> errors)> ExecuteScrapeAsync(
        ScrapeRequest request, CancellationToken ct);

    /// <summary>
    /// Optional follow-up another scraper should run after this one — orchestrator picks
    /// up the (source, symbols) tuple and enqueues a separate scrape job. Default = no follow-up.
    /// Override in scrapers that discover work for other scrapers (e.g. PsxUniverse finds
    /// stocks with announced ex-dates that StockAnalysisDividends needs to refine).
    /// </summary>
    protected virtual (string? source, string[] symbols) GetFollowUp() => (null, []);

    protected async Task ThrottleAsync(CancellationToken ct)
    {
        await Throttler.ThrottleAsync(Source, SourceConfig.RateLimit, ct);
    }

    protected async Task<T> WithRetryAsync<T>(Func<Task<T>> action, string operation, CancellationToken ct)
    {
        return await RetryPolicy.ExecuteAsync(action, SourceConfig.Retry, operation, ct);
    }

    public virtual Task<bool> HealthCheckAsync(CancellationToken ct = default) => Task.FromResult(true);

    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
