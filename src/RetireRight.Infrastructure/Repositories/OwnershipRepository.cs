using Microsoft.EntityFrameworkCore;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class OwnershipRepository : IOwnershipRepository
{
    private readonly RetireRightDbContext _db;

    public OwnershipRepository(RetireRightDbContext db) => _db = db;

    public async Task<StockOwnership?> GetLatestAsync(int stockId, CancellationToken ct = default)
        => await _db.StockOwnerships
            .Where(o => o.StockId == stockId)
            .OrderByDescending(o => o.AsOfDate)
            .FirstOrDefaultAsync(ct);

    public async Task UpsertAsync(StockOwnership ownership, CancellationToken ct = default)
    {
        var existing = await _db.StockOwnerships
            .FirstOrDefaultAsync(o =>
                o.StockId == ownership.StockId &&
                o.AsOfDate == ownership.AsOfDate, ct);

        if (existing is null)
        {
            ownership.CreatedAt = DateTime.UtcNow;
            _db.StockOwnerships.Add(ownership);
        }
        else
        {
            existing.Holders = ownership.Holders;
            existing.Source = ownership.Source;
            existing.ScrapeRunId = ownership.ScrapeRunId;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
