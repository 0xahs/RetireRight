using Microsoft.EntityFrameworkCore;
using RetireRight.Core.DTOs;
using RetireRight.Core.Interfaces;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Api.Endpoints;

public static class PortfolioEndpoints
{
    public static void MapPortfolioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/portfolio").WithTags("Portfolio");

        group.MapGet("/holdings", GetHoldingsAsync)
            .WithName("GetPortfolioHoldings");

        group.MapGet("/transactions", GetTransactionsAsync)
            .WithName("GetPortfolioTransactions");

        group.MapGet("/summary", GetSummaryAsync)
            .WithName("GetPortfolioSummary");
    }

    private static async Task<IResult> GetHoldingsAsync(
        IPortfolioRepository portfolioRepo,
        RetireRightDbContext db,
        CancellationToken ct)
    {
        var holdings = await portfolioRepo.GetHoldingsAsync(ct);
        var result = new List<HoldingDto>();

        foreach (var h in holdings)
        {
            var stock = h.Stock!;
            var price = await db.DailyPrices
                .Where(p => p.StockId == h.StockId)
                .OrderByDescending(p => p.Date)
                .FirstOrDefaultAsync(ct);

            var computed = await db.ComputedMetrics
                .Where(c => c.StockId == h.StockId)
                .OrderByDescending(c => c.ComputedDate)
                .FirstOrDefaultAsync(ct);

            var currentPrice = price?.Close;
            var costBasis = h.Shares * h.AvgCost;
            var marketValue = currentPrice.HasValue ? h.Shares * currentPrice.Value : (decimal?)null;
            var gainLoss = marketValue.HasValue ? marketValue.Value - costBasis : (decimal?)null;
            var gainLossPct = costBasis > 0 && gainLoss.HasValue ? (gainLoss.Value / costBasis) * 100 : (decimal?)null;

            var yieldPct = computed?.Metrics.GetValueOrDefault("c9_dividend_yield");
            var yieldOnCost = yieldPct.HasValue && h.AvgCost > 0 && currentPrice.HasValue && currentPrice.Value > 0
                ? yieldPct.Value * (currentPrice.Value / h.AvgCost)
                : (decimal?)null;

            var dps = computed?.Metrics.GetValueOrDefault("dps");
            var annualIncome = dps.HasValue ? h.Shares * dps.Value : (decimal?)null;

            var nextExDate = await db.Dividends
                .Where(d => d.StockId == h.StockId && d.ExDate >= DateOnly.FromDateTime(DateTime.Now))
                .OrderBy(d => d.ExDate)
                .Select(d => (DateOnly?)d.ExDate)
                .FirstOrDefaultAsync(ct);

            result.Add(new HoldingDto(
                Id: h.Id,
                Symbol: stock.Symbol,
                Name: stock.Name,
                Sector: stock.Sector.ToString(),
                Shares: h.Shares,
                AvgCost: h.AvgCost,
                CurrentPrice: currentPrice,
                MarketValue: marketValue,
                CostBasis: costBasis,
                GainLoss: gainLoss,
                GainLossPct: gainLossPct,
                YieldOnCost: yieldOnCost,
                AnnualIncome: annualIncome,
                SafetyScore: computed?.DividendSafetyScore,
                NextExDate: nextExDate,
                Platform: h.Platform,
                PurchaseDate: h.PurchaseDate));
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> GetTransactionsAsync(
        string? symbol,
        IPortfolioRepository portfolioRepo,
        IStockRepository stockRepo,
        CancellationToken ct)
    {
        int? stockId = null;
        if (symbol is not null)
        {
            var stock = await stockRepo.GetBySymbolAsync(symbol.ToUpper(), ct);
            if (stock is null) return Results.NotFound();
            stockId = stock.Id;
        }

        var transactions = await portfolioRepo.GetTransactionsAsync(stockId, ct);
        var result = transactions.Select(t => new TransactionDto(
            Id: t.Id,
            Symbol: t.Stock!.Symbol,
            Name: t.Stock.Name,
            Action: t.Action,
            Shares: t.Shares,
            Price: t.Price,
            TotalCost: t.TotalCost,
            Commission: t.Commission,
            TransactionDate: t.TransactionDate,
            Platform: t.Platform)).ToArray();

        return Results.Ok(result);
    }

    private static async Task<IResult> GetSummaryAsync(
        IPortfolioRepository portfolioRepo,
        RetireRightDbContext db,
        CancellationToken ct)
    {
        var holdings = await portfolioRepo.GetHoldingsAsync(ct);
        if (holdings.Count == 0)
            return Results.Ok(new PortfolioSummaryDto(0, 0, 0, 0, 0, 0, 0));

        var totalCost = holdings.Sum(h => h.Shares * h.AvgCost);
        decimal? totalMarketValue = 0;
        decimal? annualIncome = 0;

        foreach (var h in holdings)
        {
            var price = await db.DailyPrices
                .Where(p => p.StockId == h.StockId)
                .OrderByDescending(p => p.Date)
                .Select(p => p.Close)
                .FirstOrDefaultAsync(ct);

            if (price > 0) totalMarketValue += h.Shares * price;

            var dps = await db.ComputedMetrics
                .Where(c => c.StockId == h.StockId)
                .OrderByDescending(c => c.ComputedDate)
                .Select(c => (decimal?)c.Metrics.GetValueOrDefault("dps"))
                .FirstOrDefaultAsync(ct);

            if (dps.HasValue) annualIncome += h.Shares * dps.Value;
        }

        var gainLoss = totalMarketValue - totalCost;
        var gainLossPct = totalCost > 0 ? (gainLoss / totalCost) * 100 : null;
        var portfolioYield = totalMarketValue > 0 ? (annualIncome / totalMarketValue) * 100 : null;

        return Results.Ok(new PortfolioSummaryDto(
            totalCost, totalMarketValue, gainLoss, gainLossPct,
            annualIncome, portfolioYield, holdings.Count));
    }
}
