using Microsoft.EntityFrameworkCore;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class PriceRepository : IPriceRepository
{
    private readonly RetireRightDbContext _db;

    public PriceRepository(RetireRightDbContext db) => _db = db;

    public async Task<DailyPrice?> GetLatestAsync(int stockId, CancellationToken ct = default)
        => await _db.DailyPrices
            .Where(p => p.StockId == stockId)
            .OrderByDescending(p => p.Date)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<DailyPrice>> GetRangeAsync(
        int stockId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await _db.DailyPrices
            .Where(p => p.StockId == stockId && p.Date >= from && p.Date <= to)
            .OrderBy(p => p.Date)
            .ToListAsync(ct);

    public async Task UpsertAsync(DailyPrice price, CancellationToken ct = default)
    {
        var existing = await _db.DailyPrices
            .FirstOrDefaultAsync(p => p.StockId == price.StockId && p.Date == price.Date, ct);

        if (existing is null)
        {
            price.CreatedAt = DateTime.UtcNow;
            _db.DailyPrices.Add(price);
        }
        else
        {
            existing.Open = price.Open;
            existing.High = price.High;
            existing.Low = price.Low;
            existing.Close = price.Close;
            existing.Volume = price.Volume;
            existing.Trades = price.Trades;
            existing.Value = price.Value;
            existing.ChangePct = price.ChangePct;
            existing.Source = price.Source;
            existing.ScrapeRunId = price.ScrapeRunId;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpsertBatchAsync(IEnumerable<DailyPrice> prices, CancellationToken ct = default)
    {
        foreach (var price in prices)
        {
            var existing = await _db.DailyPrices
                .FirstOrDefaultAsync(p => p.StockId == price.StockId && p.Date == price.Date, ct);

            if (existing is null)
            {
                price.CreatedAt = DateTime.UtcNow;
                _db.DailyPrices.Add(price);
            }
            else
            {
                existing.Open = price.Open;
                existing.High = price.High;
                existing.Low = price.Low;
                existing.Close = price.Close;
                existing.Volume = price.Volume;
                existing.Trades = price.Trades;
                existing.Value = price.Value;
                existing.ChangePct = price.ChangePct;
                existing.Source = price.Source;
                existing.ScrapeRunId = price.ScrapeRunId;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
