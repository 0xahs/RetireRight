using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        var query = _db.StockFundamentals.Where(f => f.StockId == stockId);
        if (periodicity is not null)
            query = query.Where(f => f.Periodicity == periodicity);

        var all = await query.OrderByDescending(f => f.ScrapedDate).ToListAsync(ct);
        if (all.Count == 0) return null;
        if (all.Count == 1) return all[0];

        // Merge all source rows — each source owns its own keys, no conflicts expected.
        // Later scraped dates win on any key collision.
        var mergedData = new Dictionary<string, decimal?>();
        foreach (var record in all.OrderBy(f => f.ScrapedDate))
        {
            foreach (var (key, value) in record.Data)
                if (value is not null)
                    mergedData[key] = value;
        }

        // Detach before mutating so EF doesn't track the synthetic merged dict back to the DB.
        var merged = all[0]; // most recent as base (for metadata)
        _db.Entry(merged).State = EntityState.Detached;
        merged.Data = mergedData;
        return merged;
    }

    public async Task UpsertAsync(StockFundamental fundamental, CancellationToken ct = default)
    {
        // One row per (stock, periodicity, source) — each scraper owns its row exclusively.
        var existing = await _db.StockFundamentals
            .FirstOrDefaultAsync(f =>
                f.StockId == fundamental.StockId &&
                f.Periodicity == fundamental.Periodicity &&
                f.Source == fundamental.Source, ct);

        if (existing is null)
        {
            fundamental.CreatedAt = DateTime.UtcNow;
            _db.StockFundamentals.Add(fundamental);
        }
        else
        {
            existing.Data = fundamental.Data;
            existing.ScrapedDate = fundamental.ScrapedDate;
            existing.ScrapeRunId = fundamental.ScrapeRunId;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
