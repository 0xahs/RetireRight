using Microsoft.EntityFrameworkCore;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class IndexLevelRepository : IIndexLevelRepository
{
    private readonly RetireRightDbContext _db;

    public IndexLevelRepository(RetireRightDbContext db) => _db = db;

    public async Task<IndexLevel?> GetLatestAsync(string indexName, CancellationToken ct = default)
        => await _db.IndexLevels
            .Where(i => i.IndexName == indexName)
            .OrderByDescending(i => i.Date)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<IndexLevel>> GetRangeAsync(
        string indexName, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await _db.IndexLevels
            .Where(i => i.IndexName == indexName && i.Date >= from && i.Date <= to)
            .OrderBy(i => i.Date)
            .ToListAsync(ct);

    public async Task UpsertAsync(IndexLevel level, CancellationToken ct = default)
    {
        var existing = await _db.IndexLevels
            .FirstOrDefaultAsync(i =>
                i.IndexName == level.IndexName &&
                i.Date == level.Date, ct);

        if (existing is null)
        {
            level.CreatedAt = DateTime.UtcNow;
            _db.IndexLevels.Add(level);
        }
        else
        {
            existing.Open = level.Open;
            existing.High = level.High;
            existing.Low = level.Low;
            existing.Close = level.Close;
            existing.Volume = level.Volume;
            existing.ChangePct = level.ChangePct;
            existing.Source = level.Source;
            existing.ScrapeRunId = level.ScrapeRunId;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
