using Microsoft.EntityFrameworkCore;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class ComputedMetricsRepository : IComputedMetricsRepository
{
    private readonly RetireRightDbContext _db;

    public ComputedMetricsRepository(RetireRightDbContext db) => _db = db;

    public async Task<ComputedMetrics?> GetLatestAsync(int stockId, CancellationToken ct = default)
        => await _db.ComputedMetrics
            .Where(c => c.StockId == stockId)
            .OrderByDescending(c => c.ComputedDate)
            .FirstOrDefaultAsync(ct);

    public async Task UpsertAsync(ComputedMetrics metrics, CancellationToken ct = default)
    {
        var existing = await _db.ComputedMetrics
            .FirstOrDefaultAsync(c =>
                c.StockId == metrics.StockId &&
                c.ComputedDate == metrics.ComputedDate, ct);

        if (existing is null)
        {
            metrics.CreatedAt = DateTime.UtcNow;
            _db.ComputedMetrics.Add(metrics);
        }
        else
        {
            existing.Metrics = metrics.Metrics;
            existing.CompositeScore = metrics.CompositeScore;
            existing.DividendSafetyScore = metrics.DividendSafetyScore;
            existing.DividendGrowthScore = metrics.DividendGrowthScore;
            existing.ValuationScore = metrics.ValuationScore;
            existing.Signal = metrics.Signal;
            existing.ScrapeRunId = metrics.ScrapeRunId;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
