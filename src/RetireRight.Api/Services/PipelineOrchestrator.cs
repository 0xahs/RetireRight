using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RetireRight.Api.Jobs;
using RetireRight.Core.Configuration;
using RetireRight.Core.DTOs;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;
using RetireRight.MetricsEngine;
using RetireRight.MetricsEngine.Calculators;
using RetireRight.Scrapers;
using EngineService = RetireRight.MetricsEngine.MetricsEngine;

namespace RetireRight.Api.Services;

/// <summary>
/// Encapsulates all pipeline execution logic (scrape, compute, export).
/// Scoped service — resolved fresh per pipeline job execution.
/// </summary>
public sealed class PipelineOrchestrator
{
    private readonly IStockRepository _stockRepo;
    private readonly IPriceRepository _priceRepo;
    private readonly IDividendRepository _dividendRepo;
    private readonly IFundamentalsRepository _fundamentalsRepo;
    private readonly IComputedMetricsRepository _metricsRepo;
    private readonly IScrapeRunRepository _scrapeRunRepo;
    private readonly IScraperConfigRepository _scraperConfigRepo;
    private readonly RetireRightDbContext _db;
    private readonly ScraperFactory _scraperFactory;
    private readonly EngineService _engine;
    private readonly IEnumerable<IExporter> _exporters;
    private readonly PipelineJobService _jobService;
    private readonly RetireRightOptions _options;
    private readonly ILogger<PipelineOrchestrator> _logger;

    public PipelineOrchestrator(
        IStockRepository stockRepo,
        IPriceRepository priceRepo,
        IDividendRepository dividendRepo,
        IFundamentalsRepository fundamentalsRepo,
        IComputedMetricsRepository metricsRepo,
        IScrapeRunRepository scrapeRunRepo,
        IScraperConfigRepository scraperConfigRepo,
        RetireRightDbContext db,
        ScraperFactory scraperFactory,
        EngineService engine,
        IEnumerable<IExporter> exporters,
        PipelineJobService jobService,
        IOptions<RetireRightOptions> options,
        ILogger<PipelineOrchestrator> logger)
    {
        _stockRepo = stockRepo;
        _priceRepo = priceRepo;
        _dividendRepo = dividendRepo;
        _fundamentalsRepo = fundamentalsRepo;
        _metricsRepo = metricsRepo;
        _scrapeRunRepo = scrapeRunRepo;
        _scraperConfigRepo = scraperConfigRepo;
        _db = db;
        _scraperFactory = scraperFactory;
        _engine = engine;
        _exporters = exporters;
        _jobService = jobService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ScrapeRunDto[]> ScrapeAsync(
        string[]? symbols,
        string mode,
        string? sourceFilter,
        Action<string, int, int>? onProgress,
        CancellationToken ct)
    {
        var targetSymbols = symbols?.Length > 0
            ? symbols
            : (await _stockRepo.GetWatchlistAsync(ct)).Select(s => s.Symbol).ToArray();

        var results = new List<ScrapeRunDto>();

        var scrapers = await _scraperFactory.CreateEnabledAsync(sourceFilter, ct);

        // Resolve scraper type -> user-facing name from DB so nothing here hardcodes "Sarmaaya" etc.
        var configs = await _scraperConfigRepo.GetAllAsync(ct);
        var nameByType = configs.ToDictionary(c => c.Type, c => c.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var scraper in scrapers)
        {
            var sourceName    = nameByType.TryGetValue(scraper.Source, out var n) ? n : scraper.Source;
            var symbolsForRun = scraper.IsPerSymbol ? targetSymbols.Length : 0;
            var run = await _scrapeRunRepo.CreateAsync(new ScrapeRun
            {
                Source = sourceName,
                Mode = mode,
                SymbolCount = symbolsForRun
            }, ct);

            ScrapeResult? result = null;
            try
            {
                var request = new ScrapeRequest
                {
                    Symbols = targetSymbols,
                    ScrapeRunId = run.Id
                };

                // Emit pre-start progress so UI reflects "currently running" not "just finished".
                onProgress?.Invoke(run.Source, 0, symbolsForRun);

                result = await scraper.ScrapeAsync(request, ct);

                run.RecordsScraped = result.RecordsScraped;
                run.RecordsFailed = result.RecordsFailed;
                run.Status = result.Success ? "completed" : "failed";
                run.CompletedAt = DateTime.UtcNow;
                // Whole-universe scrapers don't know their record count up-front — backfill.
                if (!scraper.IsPerSymbol) run.SymbolCount = result.RecordsScraped;

                var total = scraper.IsPerSymbol ? targetSymbols.Length : result.RecordsScraped;
                onProgress?.Invoke(run.Source, result.RecordsScraped, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scraper {Source} failed", run.Source);
                run.Status = "failed";
                run.CompletedAt = DateTime.UtcNow;
            }
            finally
            {
                // Clear the change tracker so the 800+ stock/fundamental entities accumulated
                // during the scrape don't interfere with this final save.
                _db.ChangeTracker.Clear();
                try
                {
                    await _scrapeRunRepo.UpdateAsync(run, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to finalize ScrapeRun {RunId} — inner: {Inner}",
                        run.Id, ex.InnerException?.Message ?? ex.Message);
                }
                await scraper.DisposeAsync();
            }

            // Enqueue any follow-up the scraper requested (e.g. PsxUniverse asks for a
            // per-stock StockAnalysisDividends pass on stocks with announced upcoming
            // ex-dates that aren't in the watchlist). The new job goes to the back of
            // the queue and runs after the current scrape pass completes.
            if (result is { FollowUpSource: { Length: > 0 } followUp, FollowUpSymbols.Length: > 0 })
            {
                var followUpJob = new PipelineJob("scrape", result.FollowUpSymbols, "live", followUp);
                _ = _jobService.TriggerAsync(followUpJob, ct);
                _logger.LogInformation("[{Source}] enqueued follow-up: {NextSource} for {Count} symbol(s) — {Symbols}",
                    run.Source, followUp, result.FollowUpSymbols.Length,
                    string.Join(",", result.FollowUpSymbols.Take(8)));
            }

            results.Add(new ScrapeRunDto(
                run.Id, run.Source, run.Mode, run.Status,
                run.StartedAt, run.CompletedAt,
                run.SymbolCount, run.RecordsScraped, run.RecordsFailed));
        }

        return results.ToArray();
    }

    public async Task<(int Computed, int Failed)> ComputeAsync(
        string[]? symbols,
        Action<string, int, int>? onProgress,
        CancellationToken ct)
    {
        var stocks = await ResolveStocksAsync(symbols, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var oneYearAgo = today.AddDays(-365);
        var computed = 0;
        var failed = 0;

        for (var i = 0; i < stocks.Count; i++)
        {
            var stock = stocks[i];
            ct.ThrowIfCancellationRequested();

            try
            {
                var latestPrice = await _priceRepo.GetLatestAsync(stock.Id, ct);
                var priceHistory = await _priceRepo.GetRangeAsync(stock.Id, oneYearAgo, today, ct);
                var dividends = await _dividendRepo.GetHistoryAsync(stock.Id, ct);

                var fundamentals = new Dictionary<string, decimal?>();
                var annFundamentals = await _fundamentalsRepo.GetLatestAsync(stock.Id, "ANN", ct);
                var ltmFundamentals = await _fundamentalsRepo.GetLatestAsync(stock.Id, "LTM", ct);

                if (annFundamentals?.Data is not null)
                    foreach (var (key, value) in annFundamentals.Data)
                        fundamentals[key] = value;

                if (ltmFundamentals?.Data is not null)
                    foreach (var (key, value) in ltmFundamentals.Data)
                        if (value is not null)
                            fundamentals[key] = value;

                var dividendPoints = dividends
                    .Select(d => new DividendPoint(d.ExDate, d.Dps, d.DividendType))
                    .ToList();

                FundamentalsEnricher.Enrich(fundamentals, dividendPoints, latestPrice?.Close, stock.Sector);

                var context = new MetricContext
                {
                    StockId = stock.Id,
                    Symbol = stock.Symbol,
                    CurrentPrice = latestPrice?.Close,
                    Volume = latestPrice?.Volume,
                    PriceHistory = priceHistory
                        .Select(p => new PricePoint(p.Date, p.Close, p.High, p.Low, p.Volume))
                        .ToList(),
                    DividendHistory = dividendPoints,
                    Fundamentals = fundamentals,
                    Sector = stock.Sector
                };

                var result = await _engine.ComputeAllAsync(context, ct);

                var (safety, growth, valuation, composite) =
                    CompositeScoreCalculator.ComputeScores(result.Metrics);

                var opportunityMetrics = OpportunityScorer.Score(result.Metrics, context);
                foreach (var (key, value) in opportunityMetrics)
                    result.Metrics[key] = value;

                var signal = composite switch
                {
                    >= 80 => RetireRight.Core.Enums.SignalType.StrongBuy,
                    >= 65 => RetireRight.Core.Enums.SignalType.Buy,
                    >= 50 => RetireRight.Core.Enums.SignalType.Accumulate,
                    >= 35 => RetireRight.Core.Enums.SignalType.Hold,
                    >= 20 => RetireRight.Core.Enums.SignalType.Sell,
                    _ when composite.HasValue => RetireRight.Core.Enums.SignalType.StrongSell,
                    _ => (RetireRight.Core.Enums.SignalType?)null
                };

                await _metricsRepo.UpsertAsync(new ComputedMetrics
                {
                    StockId = stock.Id,
                    ComputedDate = today,
                    Metrics = result.Metrics,
                    CompositeScore = composite,
                    DividendSafetyScore = safety,
                    DividendGrowthScore = growth,
                    ValuationScore = valuation,
                    Signal = signal
                }, ct);

                computed++;
                onProgress?.Invoke(stock.Symbol, computed, stocks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compute metrics for {Symbol}", stock.Symbol);
                failed++;
            }
        }

        return (computed, failed);
    }

    public async Task ExportAsync(Action<int, int>? onProgress, CancellationToken ct)
    {
        var data = await BuildExportDataAsync(ct);
        var outputDir = _options.Export.OutputPath;
        Directory.CreateDirectory(outputDir);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmm");
        var exporterList = _exporters.ToList();

        for (var i = 0; i < exporterList.Count; i++)
        {
            var exporter = exporterList[i];
            ct.ThrowIfCancellationRequested();

            var filePath = Path.Combine(outputDir, $"retireright_{timestamp}.{exporter.Format}");
            var latestPath = Path.Combine(outputDir, $"latest.{exporter.Format}");

            try
            {
                await exporter.ExportAsync(data, filePath, ct);
                if (File.Exists(filePath))
                    File.Copy(filePath, latestPath, overwrite: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Export {Format} failed", exporter.Format);
            }

            onProgress?.Invoke(i + 1, exporterList.Count);
        }
    }

    public async Task<ExportData> BuildExportDataAsync(CancellationToken ct)
    {
        var stocks = await _db.Stocks.Where(s => s.IsWatchlist && s.IsActive).ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.Now);
        var stockSnapshots = new List<StockSnapshot>();
        DateTime? latestPriceTime = null;

        foreach (var stock in stocks)
        {
            var price = await _db.DailyPrices
                .Where(p => p.StockId == stock.Id)
                .OrderByDescending(p => p.Date)
                .FirstOrDefaultAsync(ct);

            var metrics = await _db.ComputedMetrics
                .Where(c => c.StockId == stock.Id)
                .OrderByDescending(c => c.ComputedDate)
                .FirstOrDefaultAsync(ct);

            if (price is not null && (latestPriceTime is null || price.CreatedAt > latestPriceTime))
                latestPriceTime = price.CreatedAt;

            var metricsDict = metrics?.Metrics ?? new Dictionary<string, decimal?>();
            var opportunityScore = metricsDict.GetValueOrDefault("o6_opportunity_score");
            var opportunityFlags = OpportunityScorer.DecodeFlags(metricsDict.GetValueOrDefault("o7_flags"));

            stockSnapshots.Add(new StockSnapshot
            {
                Symbol = stock.Symbol,
                Name = stock.Name,
                Sector = stock.Sector,
                Price = price?.Close,
                ChangePct = price?.ChangePct,
                Volume = price?.Volume,
                Metrics = metricsDict,
                CompositeScore = metrics?.CompositeScore,
                Signal = metrics?.Signal.ToString(),
                OpportunityScore = opportunityScore,
                OpportunityFlags = opportunityFlags
            });
        }

        var upcomingDivs = await _db.Dividends
            .Include(d => d.Stock)
            .Where(d => d.ExDate >= today && d.ExDate <= today.AddDays(60))
            .OrderBy(d => d.ExDate)
            .ToListAsync(ct);

        var dividendSnapshots = upcomingDivs.Select(d => new DividendSnapshot
        {
            Symbol = d.Stock!.Symbol,
            ExDate = d.ExDate,
            Dps = d.Dps,
            PaymentDate = d.PaymentDate,
            LastBuyDate = d.ExDate.AddDays(-1),
            DaysUntilEx = d.ExDate.DayNumber - today.DayNumber
        }).ToList();

        var kse100 = await _db.IndexLevels.Where(i => i.IndexName == "KSE100")
            .OrderByDescending(i => i.Date).FirstOrDefaultAsync(ct);
        var pkrUsd = await _db.MacroIndicators.Where(m => m.IndicatorName == "PKR_USD")
            .OrderByDescending(m => m.Date).FirstOrDefaultAsync(ct);
        var brent = await _db.MacroIndicators.Where(m => m.IndicatorName == "BRENT_CRUDE")
            .OrderByDescending(m => m.Date).FirstOrDefaultAsync(ct);
        var sbpRate = await _db.MacroIndicators.Where(m => m.IndicatorName == "SBP_RATE")
            .OrderByDescending(m => m.Date).FirstOrDefaultAsync(ct);
        var latestFundamental = await _db.StockFundamentals
            .OrderByDescending(f => f.UpdatedAt).FirstOrDefaultAsync(ct);

        var staleWarnings = new List<string>();
        if (latestPriceTime.HasValue && (DateTime.UtcNow - latestPriceTime.Value).TotalHours > 24)
            staleWarnings.Add("Price data is more than 24 hours old");
        if (kse100 is not null && (today.DayNumber - kse100.Date.DayNumber) > 2)
            staleWarnings.Add("KSE-100 index data is stale");
        if (latestFundamental is not null && (DateTime.UtcNow - latestFundamental.UpdatedAt).TotalDays > 30)
            staleWarnings.Add("Fundamentals data is more than 30 days old");

        return new ExportData
        {
            Market = new MarketSnapshot
            {
                Kse100Level = kse100?.Close,
                Kse100ChangePct = kse100?.ChangePct,
                PkrUsd = pkrUsd?.Value,
                BrentCrude = brent?.Value,
                SbpRate = sbpRate?.Value,
                AsOf = DateTimeOffset.Now
            },
            Stocks = stockSnapshots,
            Dividends = dividendSnapshots,
            Alerts = GenerateAlerts(stockSnapshots, dividendSnapshots),
            Freshness = new DataFreshness
            {
                Prices = latestPriceTime,
                Fundamentals = latestFundamental?.UpdatedAt,
                Macro = sbpRate?.CreatedAt,
                Dividends = upcomingDivs.Count > 0 ? upcomingDivs.Max(d => d.CreatedAt) : null,
                StaleWarnings = staleWarnings
            }
        };
    }

    private async Task<List<RetireRight.Core.Models.Stock>> ResolveStocksAsync(
        string[]? symbols, CancellationToken ct)
    {
        if (symbols?.Length > 0)
        {
            var found = new List<RetireRight.Core.Models.Stock>();
            foreach (var sym in symbols)
            {
                var stock = await _stockRepo.GetBySymbolAsync(sym, ct);
                if (stock is not null) found.Add(stock);
                else _logger.LogWarning("Stock {Symbol} not found — skipping", sym);
            }
            return found;
        }
        return (await _stockRepo.GetWatchlistAsync(ct)).ToList();
    }

    private static List<AlertSnapshot> GenerateAlerts(
        List<StockSnapshot> stocks,
        List<DividendSnapshot> dividends)
    {
        var alerts = new List<AlertSnapshot>();

        foreach (var stock in stocks)
        {
            var yield = stock.Metrics.GetValueOrDefault("c9_dividend_yield");
            var payout = stock.Metrics.GetValueOrDefault("a1_earnings_payout_ratio");

            if (yield > 10 && payout > 90)
                alerts.Add(new AlertSnapshot { Type = "YieldTrap", Symbol = stock.Symbol,
                    Message = $"{stock.Symbol}: Yield {yield:F1}% with payout {payout:F0}% — potential yield trap",
                    Severity = "warning" });
            else if (payout > 100)
                alerts.Add(new AlertSnapshot { Type = "UnsafePayout", Symbol = stock.Symbol,
                    Message = $"{stock.Symbol}: Payout {payout:F0}% exceeds earnings — unsustainable",
                    Severity = "critical" });
        }

        foreach (var div in dividends.Where(d => d.DaysUntilEx <= 7 && d.DaysUntilEx >= 0))
            alerts.Add(new AlertSnapshot { Type = "UpcomingExDate", Symbol = div.Symbol,
                Message = $"{div.Symbol}: Ex-date in {div.DaysUntilEx} day(s) — last buy {div.LastBuyDate:yyyy-MM-dd}",
                Severity = "info" });

        foreach (var stock in stocks.Where(s => s.OpportunityScore >= 70))
            alerts.Add(new AlertSnapshot { Type = "BuyOpportunity", Symbol = stock.Symbol,
                Message = $"{stock.Symbol}: Opportunity score {stock.OpportunityScore:F0}/100 — {string.Join(", ", stock.OpportunityFlags)}",
                Severity = "actionable" });

        return alerts;
    }
}

