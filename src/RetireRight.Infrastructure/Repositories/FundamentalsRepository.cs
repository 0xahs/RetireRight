using Microsoft.EntityFrameworkCore;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class FundamentalsRepository : IFundamentalsRepository
{
    private readonly RetireRightDbContext _db;

    public FundamentalsRepository(RetireRightDbContext db) => _db = db;

    public async Task<StockFundamental?> GetLatestAsync(
        int stockId, string? periodicity = null, CancellationToken ct = default)
    {
        var query = _db.StockFundamentals
            .Where(f => f.StockId == stockId);

        if (periodicity is not null)
            query = query.Where(f => f.Periodicity == periodicity);

        return await query
            .OrderByDescending(f => f.ScrapedDate)
            .FirstOrDefaultAsync(ct);
    }

    public async Task UpsertAsync(StockFundamental fundamental, CancellationToken ct = default)
    {
        var existing = await _db.StockFundamentals
            .FirstOrDefaultAsync(f =>
                f.StockId == fundamental.StockId &&
                f.ScrapedDate == fundamental.ScrapedDate &&
                f.Periodicity == fundamental.Periodicity, ct);

        if (existing is null)
        {
            fundamental.CreatedAt = DateTime.UtcNow;
            _db.StockFundamentals.Add(fundamental);
        }
        else
        {
            // Merge new data into existing — don't overwrite with nulls
            foreach (var (key, value) in fundamental.Data)
            {
                if (value is not null)
                    existing.Data[key] = value;
            }

            existing.Source = fundamental.Source;
            existing.ScrapeRunId = fundamental.ScrapeRunId;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
