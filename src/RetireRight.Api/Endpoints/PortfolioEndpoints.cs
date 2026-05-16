using Microsoft.EntityFrameworkCore;
using RetireRight.Core.DTOs;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Api.Endpoints;

public static class PortfolioEndpoints
{
    public static void MapPortfolioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/portfolio").WithTags("Portfolio");

        group.MapGet("/holdings", GetHoldingsAsync).WithName("GetPortfolioHoldings");
        group.MapGet("/transactions", GetTransactionsAsync).WithName("GetPortfolioTransactions");
        group.MapGet("/summary", GetSummaryAsync).WithName("GetPortfolioSummary");

        group.MapPost("/transactions", AddTransactionAsync).WithName("AddTransaction");
        group.MapPut("/holdings/{id}", UpdateHoldingAsync).WithName("UpdateHolding");
        group.MapDelete("/holdings/{id}", DeleteHoldingAsync).WithName("DeleteHolding");
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

    private static async Task<IResult> AddTransactionAsync(
        AddTransactionRequest request,
        IPortfolioRepository portfolioRepo,
        IStockRepository stockRepo,
        CancellationToken ct)
    {
        var symbol = request.Symbol.Trim().ToUpper();
        var stock = await stockRepo.GetBySymbolAsync(symbol, ct);
        if (stock is null) return Results.NotFound($"Symbol '{symbol}' not found in watchlist");

        var action = request.Action.Trim().ToUpper();
        if (action is not ("BUY" or "SELL"))
            return Results.BadRequest("Action must be BUY or SELL");

        if (request.Shares <= 0) return Results.BadRequest("Shares must be positive");
        if (request.Price <= 0) return Results.BadRequest("Price must be positive");

        var transactionDate = request.TransactionDate ?? DateOnly.FromDateTime(DateTime.Now);
        var totalCost = request.Shares * request.Price + (request.Commission ?? 0);

        var tx = new PortfolioTransaction
        {
            StockId = stock.Id,
            Action = action,
            Shares = request.Shares,
            Price = request.Price,
            TotalCost = totalCost,
            Commission = request.Commission,
            TransactionDate = transactionDate,
            Platform = request.Platform,
        };

        await portfolioRepo.AddTransactionAsync(tx, ct);

        // Update holding
        var existing = await portfolioRepo.GetHoldingsAsync(ct)
            .ContinueWith(t => t.Result.FirstOrDefault(h => h.StockId == stock.Id), ct);

        if (action == "BUY")
        {
            if (existing is null)
            {
                await portfolioRepo.UpsertHoldingAsync(new PortfolioHolding
                {
                    StockId = stock.Id,
                    Shares = request.Shares,
                    AvgCost = request.Price,
                    Platform = request.Platform,
                    PurchaseDate = transactionDate,
                }, ct);
            }
            else
            {
                var newTotalShares = existing.Shares + request.Shares;
                var newAvgCost = (existing.Shares * existing.AvgCost + request.Shares * request.Price) / newTotalShares;
                await portfolioRepo.UpsertHoldingAsync(new PortfolioHolding
                {
                    StockId = stock.Id,
                    Shares = newTotalShares,
                    AvgCost = Math.Round(newAvgCost, 4),
                    Platform = existing.Platform ?? request.Platform,
                    PurchaseDate = existing.PurchaseDate,
                }, ct);
            }
        }
        else // SELL
        {
            if (existing is null) return Results.BadRequest($"No holding found for {symbol}");
            if (existing.Shares < request.Shares)
                return Results.BadRequest($"Insufficient shares: holding {existing.Shares}, selling {request.Shares}");

            var remainingShares = existing.Shares - request.Shares;
            if (remainingShares == 0)
            {
                await portfolioRepo.DeactivateHoldingAsync(existing.Id, ct);
            }
            else
            {
                await portfolioRepo.UpsertHoldingAsync(new PortfolioHolding
                {
                    StockId = stock.Id,
                    Shares = remainingShares,
                    AvgCost = existing.AvgCost,
                    Platform = existing.Platform,
                    PurchaseDate = existing.PurchaseDate,
                }, ct);
            }
        }

        return Results.Created($"/api/portfolio/transactions/{tx.Id}", new TransactionDto(
            tx.Id, stock.Symbol, stock.Name, tx.Action,
            tx.Shares, tx.Price, tx.TotalCost, tx.Commission,
            tx.TransactionDate, tx.Platform));
    }

    private static async Task<IResult> UpdateHoldingAsync(
        int id,
        UpdateHoldingRequest request,
        IPortfolioRepository portfolioRepo,
        CancellationToken ct)
    {
        var holdings = await portfolioRepo.GetHoldingsAsync(ct);
        var holding = holdings.FirstOrDefault(h => h.Id == id);
        if (holding is null) return Results.NotFound();

        await portfolioRepo.UpsertHoldingAsync(new PortfolioHolding
        {
            StockId = holding.StockId,
            Shares = request.Shares,
            AvgCost = request.AvgCost,
            Platform = request.Platform ?? holding.Platform,
            PurchaseDate = request.PurchaseDate ?? holding.PurchaseDate,
        }, ct);

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteHoldingAsync(
        int id,
        IPortfolioRepository portfolioRepo,
        CancellationToken ct)
    {
        var deleted = await portfolioRepo.DeactivateHoldingAsync(id, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
