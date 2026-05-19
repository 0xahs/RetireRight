using Microsoft.EntityFrameworkCore;
using RetireRight.Infrastructure.Database;
using RetireRight.MetricsEngine;
using RetireRight.Screening;

namespace RetireRight.Api.Services;

public class ScreenDataProvider : IScreenDataProvider
{
    private readonly RetireRightDbContext _db;

    public ScreenDataProvider(RetireRightDbContext db) => _db = db;

    public async Task<IReadOnlyList<ScreenableStock>> GetScreenableStocksAsync(
        bool watchlistOnly = false,
        string[]? sectors = null,
        CancellationToken ct = default)
    {
        var stockQuery = _db.Stocks.Where(s => s.IsActive);
        if (watchlistOnly) stockQuery = stockQuery.Where(s => s.IsWatchlist);
        if (sectors is { Length: > 0 }) stockQuery = stockQuery.Where(s => sectors.Contains(s.Sector));

        var stocks = await stockQuery.ToListAsync(ct);
        var result = new List<ScreenableStock>();

        foreach (var stock in stocks)
        {
            var price = await _db.DailyPrices
                .Where(p => p.StockId == stock.Id)
                .OrderByDescending(p => p.Date)
                .Select(p => p.Close)
                .FirstOrDefaultAsync(ct);

            var computed = await _db.ComputedMetrics
                .Where(c => c.StockId == stock.Id)
                .OrderByDescending(c => c.ComputedDate)
                .FirstOrDefaultAsync(ct);

            result.Add(new ScreenableStock
            {
                Symbol = stock.Symbol,
                Name = stock.Name,
                Sector = stock.Sector,
                Price = price > 0 ? price : null,
                CompositeScore = computed?.CompositeScore,
                Signal = computed?.Signal?.ToString(),
                Metrics = computed?.Metrics ?? new Dictionary<string, decimal?>()
            });
        }

        return result;
    }
}
