using Microsoft.EntityFrameworkCore;
using RetireRight.Api.Mapping;
using RetireRight.Core.DTOs;
using RetireRight.Core.Interfaces;
using RetireRight.Infrastructure.Database;
using RetireRight.MetricsEngine;

namespace RetireRight.Api.Endpoints;

public static class StocksEndpoints
{
    public static void MapStocksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stocks").WithTags("Stocks");

        group.MapGet("/", GetWatchlistAsync)
            .WithName("GetWatchlist")
            .WithSummary("Get all watchlist stocks with latest price and metrics");

        group.MapGet("/{symbol}", GetStockDetailAsync)
            .WithName("GetStockDetail")
            .WithSummary("Get full stock detail with metrics, dividends, and price history");

        group.MapGet("/{symbol}/metrics", GetStockMetricsAsync)
            .WithName("GetStockMetrics")
            .WithSummary("Get computed metrics for a stock");

        group.MapGet("/{symbol}/dividends", GetStockDividendsAsync)
            .WithName("GetStockDividends")
            .WithSummary("Get dividend history for a stock");

        group.MapGet("/{symbol}/price-history", GetPriceHistoryAsync)
            .WithName("GetPriceHistory")
            .WithSummary("Get price history for a stock");

        group.MapGet("/{symbol}/signal-history", GetSignalHistoryAsync)
            .WithName("GetSignalHistory")
            .WithSummary("Get derived verdict history for the last N days");

        group.MapGet("/{symbol}/memo", GetStockMemoAsync)
            .WithName("GetStockMemo")
            .WithSummary("Generate a per-stock investment memo with metric citations");
    }

    private static async Task<IResult> GetWatchlistAsync(
        RetireRightDbContext db,
        IStockRepository stockRepo,
        CancellationToken ct)
    {
        var stocks = await stockRepo.GetWatchlistAsync(ct);
        var result = new List<StockSummaryDto>();

        foreach (var stock in stocks)
        {
            var recentPrices = await db.DailyPrices
                .Where(p => p.StockId == stock.Id)
                .OrderByDescending(p => p.Date)
                .Take(2)
                .ToListAsync(ct);

            var price = recentPrices.FirstOrDefault();
            if (price is not null && price.ChangePct is null && recentPrices.Count >= 2)
            {
                var prev = recentPrices[1].Close;
                if (prev > 0) price.ChangePct = Math.Round((price.Close - prev) / prev * 100, 2);
            }

            var computed = await db.ComputedMetrics
                .Where(c => c.StockId == stock.Id)
                .OrderByDescending(c => c.ComputedDate)
                .FirstOrDefaultAsync(ct);

            result.Add(BuildSummaryDto(stock, price, computed));
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> GetStockDetailAsync(
        string symbol,
        RetireRightDbContext db,
        IStockRepository stockRepo,
        IPriceRepository priceRepo,
        IDividendRepository dividendRepo,
        CancellationToken ct)
    {
        var stock = await stockRepo.GetBySymbolAsync(symbol.ToUpper(), ct);
        if (stock is null) return Results.NotFound();

        var recentPrices2 = await db.DailyPrices
            .Where(p => p.StockId == stock.Id)
            .OrderByDescending(p => p.Date)
            .Take(2)
            .ToListAsync(ct);

        var price = recentPrices2.FirstOrDefault();
        if (price is not null && price.ChangePct is null && recentPrices2.Count >= 2)
        {
            var prev = recentPrices2[1].Close;
            if (prev > 0) price.ChangePct = Math.Round((price.Close - prev) / prev * 100, 2);
        }

        var computed = await db.ComputedMetrics
            .Where(c => c.StockId == stock.Id)
            .OrderByDescending(c => c.ComputedDate)
            .FirstOrDefaultAsync(ct);

        var dividends = await dividendRepo.GetHistoryAsync(stock.Id, ct);

        var today = DateOnly.FromDateTime(DateTime.Now);
        var oneYearAgo = today.AddYears(-2);
        var priceHistory = await priceRepo.GetRangeAsync(stock.Id, oneYearAgo, today, ct);

        var summary = BuildSummaryDto(stock, price, computed);

        var metrics = computed?.Metrics is not null
            ? MetricsMapper.ToDtoWithScores(computed.Metrics, computed)
            : MetricsMapper.ToDto(new Dictionary<string, decimal?>());

        var recentDividends = dividends
            .OrderByDescending(d => d.ExDate)
            .Take(20)
            .Select(d => new DividendSummaryDto(
                d.ExDate, d.PaymentDate, d.Dps, d.Percentage, d.DividendType, d.Year, d.Source))
            .ToArray();

        var pricePoints = priceHistory
            .OrderByDescending(p => p.Date)
            .Take(500)
            .Select(p => new PricePointDto(p.Date, p.Open, p.High, p.Low, p.Close, p.Volume, p.ChangePct))
            .ToArray();

        return Results.Ok(new StockDetailDto(summary, metrics, recentDividends, pricePoints));
    }

    private static async Task<IResult> GetStockMetricsAsync(
        string symbol,
        RetireRightDbContext db,
        IStockRepository stockRepo,
        CancellationToken ct)
    {
        var stock = await stockRepo.GetBySymbolAsync(symbol.ToUpper(), ct);
        if (stock is null) return Results.NotFound();

        var computed = await db.ComputedMetrics
            .Where(c => c.StockId == stock.Id)
            .OrderByDescending(c => c.ComputedDate)
            .FirstOrDefaultAsync(ct);

        if (computed is null) return Results.NotFound(new { message = "No metrics computed yet" });

        return Results.Ok(MetricsMapper.ToDtoWithScores(computed.Metrics, computed));
    }

    private static async Task<IResult> GetStockDividendsAsync(
        string symbol,
        IStockRepository stockRepo,
        IDividendRepository dividendRepo,
        CancellationToken ct)
    {
        var stock = await stockRepo.GetBySymbolAsync(symbol.ToUpper(), ct);
        if (stock is null) return Results.NotFound();

        var dividends = await dividendRepo.GetHistoryAsync(stock.Id, ct);
        var result = dividends
            .OrderByDescending(d => d.ExDate)
            .Select(d => new DividendSummaryDto(
                d.ExDate, d.PaymentDate, d.Dps, d.Percentage, d.DividendType, d.Year, d.Source))
            .ToArray();

        return Results.Ok(result);
    }

    private static async Task<IResult> GetPriceHistoryAsync(
        string symbol,
        string? from,
        string? to,
        IStockRepository stockRepo,
        IPriceRepository priceRepo,
        CancellationToken ct)
    {
        var stock = await stockRepo.GetBySymbolAsync(symbol.ToUpper(), ct);
        if (stock is null) return Results.NotFound();

        var today = DateOnly.FromDateTime(DateTime.Now);
        var fromDate = from is not null && DateOnly.TryParse(from, out var f) ? f : today.AddYears(-2);
        var toDate = to is not null && DateOnly.TryParse(to, out var t) ? t : today;

        var prices = await priceRepo.GetRangeAsync(stock.Id, fromDate, toDate, ct);
        var result = prices
            .OrderBy(p => p.Date)
            .Select(p => new PricePointDto(p.Date, p.Open, p.High, p.Low, p.Close, p.Volume, p.ChangePct))
            .ToArray();

        return Results.Ok(result);
    }

    private static async Task<IResult> GetStockMemoAsync(
        string symbol,
        RetireRightDbContext db,
        IStockRepository stockRepo,
        CancellationToken ct)
    {
        var stock = await stockRepo.GetBySymbolAsync(symbol.ToUpper(), ct);
        if (stock is null) return Results.NotFound();

        var price = await db.DailyPrices
            .Where(p => p.StockId == stock.Id)
            .OrderByDescending(p => p.Date)
            .FirstOrDefaultAsync(ct);

        var computed = await db.ComputedMetrics
            .Where(c => c.StockId == stock.Id)
            .OrderByDescending(c => c.ComputedDate)
            .FirstOrDefaultAsync(ct);

        var memo = RetireRight.Api.Mapping.MemoGenerator.Generate(stock, price, computed);
        return Results.Ok(memo);
    }

    private static async Task<IResult> GetSignalHistoryAsync(
        string symbol,
        int? days,
        RetireRightDbContext db,
        IStockRepository stockRepo,
        CancellationToken ct)
    {
        var stock = await stockRepo.GetBySymbolAsync(symbol.ToUpper(), ct);
        if (stock is null) return Results.NotFound();

        var lookbackDays = Math.Clamp(days ?? 30, 1, 365);
        var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-lookbackDays));

        var rows = await db.ComputedMetrics
            .Where(c => c.StockId == stock.Id && c.ComputedDate >= fromDate)
            .OrderBy(c => c.ComputedDate)
            .ToListAsync(ct);

        var sectorStr = stock.Sector.ToString();
        var points = rows.Select(c =>
        {
            var grammar = SignalGrammar.Derive(
                c.Metrics,
                c,
                // Use ComputedDate as scrapedAt proxy — the actual scrapedAt at compute time is
                // not preserved in the row, but ComputedDate gives the day-level granularity
                // needed for verdict freshness band logic.
                c.UpdatedAt,
                sectorStr);

            return new SignalHistoryPointDto(
                c.ComputedDate,
                grammar.Verdict,
                c.CompositeScore,
                c.DividendSafetyScore,
                grammar.ConvictionLevel);
        }).ToArray();

        return Results.Ok(new SignalHistoryDto(stock.Symbol, lookbackDays, points));
    }

    private static StockSummaryDto BuildSummaryDto(
        RetireRight.Core.Models.Stock stock,
        RetireRight.Core.Models.DailyPrice? price,
        RetireRight.Core.Models.ComputedMetrics? computed)
    {
        var m = computed?.Metrics ?? new Dictionary<string, decimal?>();
        var flags = RetireRight.MetricsEngine.OpportunityScorer
            .DecodeFlags(m.GetValueOrDefault("o7_flags")).ToArray();

        var sectorStr = stock.Sector.ToString();
        var grammar = SignalGrammar.Derive(m, computed, price?.UpdatedAt, sectorStr);

        return new StockSummaryDto(
            Id: stock.Id,
            Symbol: stock.Symbol,
            Name: stock.Name,
            Sector: sectorStr,
            Price: price?.Close,
            ChangePct: price?.ChangePct,
            CompositeScore: computed?.CompositeScore,
            DividendSafetyScore: computed?.DividendSafetyScore,
            DividendGrowthScore: computed?.DividendGrowthScore,
            ValuationScore: computed?.ValuationScore,
            OpportunityScore: m.GetValueOrDefault("o6_opportunity_score"),
            Signal: computed?.Signal?.ToString(),
            DividendYield: m.GetValueOrDefault("c9_dividend_yield"),
            DividendCoverage: m.GetValueOrDefault("a4_dividend_coverage"),
            OpportunityFlags: flags,
            ScrapedAt: price?.UpdatedAt,
            ComputedAt: computed is not null ? computed.UpdatedAt : null,
            Verdict: grammar.Verdict,
            Confidence: grammar.Confidence,
            ConvictionLevel: grammar.ConvictionLevel,
            SafetyTier: grammar.SafetyTier,
            NetYield: grammar.NetYield,
            WhyLine: grammar.WhyLine,
            Risks: grammar.Risks,
            MetricCompleteness: grammar.Completeness,
            MetricsTotal: grammar.Total);
    }
}
