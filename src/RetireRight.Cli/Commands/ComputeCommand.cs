using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RetireRight.Core.Enums;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.MetricsEngine;
using RetireRight.MetricsEngine.Calculators;

namespace RetireRight.Cli.Commands;

public static class ComputeCommand
{
    public static Command Create(IServiceProvider services)
    {
        var allOption = new Option<bool>("--all") { Description = "Compute metrics for all active stocks" };
        var symbolsOption = new Option<string[]>("--symbols")
        {
            Description = "Specific symbols to compute",
            AllowMultipleArgumentsPerToken = true
        };
        var watchlistOption = new Option<bool>("--watchlist-only") { Description = "Only compute for watchlist stocks" };

        var command = new Command("compute") { Description = "Compute metrics from stored data (run scrapers first)" };
        command.Options.Add(allOption);
        command.Options.Add(symbolsOption);
        command.Options.Add(watchlistOption);

        command.SetAction(async (parseResult, ct) =>
        {
            var all = parseResult.GetValue(allOption);
            var symbols = parseResult.GetValue(symbolsOption);
            var watchlistOnly = parseResult.GetValue(watchlistOption);

            await using var scope = services.CreateAsyncScope();
            var stockRepo = scope.ServiceProvider.GetRequiredService<IStockRepository>();
            var priceRepo = scope.ServiceProvider.GetRequiredService<IPriceRepository>();
            var dividendRepo = scope.ServiceProvider.GetRequiredService<IDividendRepository>();
            var fundamentalsRepo = scope.ServiceProvider.GetRequiredService<IFundamentalsRepository>();
            var metricsRepo = scope.ServiceProvider.GetRequiredService<IComputedMetricsRepository>();
            var engine = scope.ServiceProvider.GetRequiredService<MetricsEngine.MetricsEngine>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            // Determine target stocks
            IReadOnlyList<Stock> stocks;
            if (symbols is { Length: > 0 })
            {
                var found = new List<Stock>();
                foreach (var sym in symbols)
                {
                    var stock = await stockRepo.GetBySymbolAsync(sym, ct);
                    if (stock is not null)
                        found.Add(stock);
                    else
                        logger.LogWarning("Stock {Symbol} not found in database — skipping", sym);
                }
                stocks = found;
            }
            else if (watchlistOnly || !all)
            {
                stocks = await stockRepo.GetWatchlistAsync(ct);
            }
            else
            {
                stocks = await stockRepo.GetAllActiveAsync(ct);
            }

            if (stocks.Count == 0)
            {
                logger.LogWarning("No stocks to compute. Run a scraper first.");
                return;
            }

            logger.LogInformation("Computing metrics for {Count} stock(s)...", stocks.Count);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var oneYearAgo = today.AddDays(-365);
            var computed = 0;
            var failed = 0;

            foreach (var stock in stocks)
            {
                try
                {
                    // Load data
                    var latestPrice = await priceRepo.GetLatestAsync(stock.Id, ct);
                    var priceHistory = await priceRepo.GetRangeAsync(stock.Id, oneYearAgo, today, ct);
                    var dividends = await dividendRepo.GetHistoryAsync(stock.Id, ct);

                    // Load fundamentals: try LTM first, fallback to ANN, merge both
                    var fundamentals = new Dictionary<string, decimal?>();
                    var annFundamentals = await fundamentalsRepo.GetLatestAsync(stock.Id, "ANN", ct);
                    var ltmFundamentals = await fundamentalsRepo.GetLatestAsync(stock.Id, "LTM", ct);

                    // Start with ANN as base, overlay LTM (more current)
                    if (annFundamentals?.Data is not null)
                    {
                        foreach (var (key, value) in annFundamentals.Data)
                            fundamentals[key] = value;
                    }
                    if (ltmFundamentals?.Data is not null)
                    {
                        foreach (var (key, value) in ltmFundamentals.Data)
                        {
                            if (value is not null)
                                fundamentals[key] = value;
                        }
                    }

                    // Enrich fundamentals with derived values (DPS, market cap, bank revenue, etc.)
                    var dividendPoints = dividends
                        .Select(d => new DividendPoint(d.ExDate, d.Dps, d.DividendType))
                        .ToList();
                    FundamentalsEnricher.Enrich(fundamentals, dividendPoints, latestPrice?.Close, stock.Sector);

                    // Build MetricContext
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

                    // Run all calculators
                    var result = await engine.ComputeAllAsync(context, ct);

                    // Compute composite scores
                    var (safety, growth, valuation, composite) =
                        CompositeScoreCalculator.ComputeScores(result.Metrics);

                    // Compute opportunity score (post-processor — needs all metrics + context)
                    var opportunityMetrics = OpportunityScorer.Score(result.Metrics, context);
                    foreach (var (key, value) in opportunityMetrics)
                        result.Metrics[key] = value;

                    // Determine signal
                    var signal = composite switch
                    {
                        >= 80 => SignalType.StrongBuy,
                        >= 65 => SignalType.Buy,
                        >= 50 => SignalType.Accumulate,
                        >= 35 => SignalType.Hold,
                        >= 20 => SignalType.Sell,
                        _ when composite.HasValue => SignalType.StrongSell,
                        _ => (SignalType?)null
                    };

                    // Persist
                    await metricsRepo.UpsertAsync(new ComputedMetrics
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

                    var nonNullMetrics = result.Metrics.Count(kv => kv.Value is not null);
                    logger.LogInformation(
                        "[{Symbol}] {NonNull}/{Total} metrics | Safety: {Safety:F0} | Growth: {Growth:F0} | " +
                        "Valuation: {Val:F0} | Composite: {Comp:F0} → {Signal}",
                        stock.Symbol, nonNullMetrics, result.Metrics.Count,
                        safety, growth, valuation, composite, signal);

                    computed++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to compute metrics for {Symbol}", stock.Symbol);
                    failed++;
                }
            }

            logger.LogInformation("Compute complete: {Computed} succeeded, {Failed} failed", computed, failed);
        });

        return command;
    }
}
