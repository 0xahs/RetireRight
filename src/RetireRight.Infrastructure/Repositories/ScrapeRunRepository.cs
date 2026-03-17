using Microsoft.EntityFrameworkCore;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class ScrapeRunRepository : IScrapeRunRepository
{
    private readonly RetireRightDbContext _db;

    public ScrapeRunRepository(RetireRightDbContext db) => _db = db;

    public async Task<ScrapeRun> CreateAsync(ScrapeRun run, CancellationToken ct = default)
    {
        _db.ScrapeRuns.Add(run);
        await _db.SaveChangesAsync(ct);
        return run;
    }

    public async Task UpdateAsync(ScrapeRun run, CancellationToken ct = default)
    {
        _db.ScrapeRuns.Update(run);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> DeleteTestDataAsync(CancellationToken ct = default)
    {
        var testRunIds = await _db.ScrapeRuns
            .Where(r => r.Mode == "test")
            .Select(r => r.Id)
            .ToListAsync(ct);

        if (testRunIds.Count == 0)
            return 0;

        // Delete data from all tables that reference test scrape runs.
        // Order matters: children before parents.
        var deleted = 0;

        deleted += await _db.InsiderTransactions
            .Where(t => t.ScrapeRunId != null && testRunIds.Contains(t.ScrapeRunId.Value))
            .ExecuteDeleteAsync(ct);

        deleted += await _db.StockOwnerships
            .Where(o => o.ScrapeRunId != null && testRunIds.Contains(o.ScrapeRunId.Value))
            .ExecuteDeleteAsync(ct);

        deleted += await _db.StockFundamentals
            .Where(f => f.ScrapeRunId != null && testRunIds.Contains(f.ScrapeRunId.Value))
            .ExecuteDeleteAsync(ct);

        deleted += await _db.ComputedMetrics
            .Where(c => c.ScrapeRunId != null && testRunIds.Contains(c.ScrapeRunId.Value))
            .ExecuteDeleteAsync(ct);

        deleted += await _db.Dividends
            .Where(d => d.ScrapeRunId != null && testRunIds.Contains(d.ScrapeRunId.Value))
            .ExecuteDeleteAsync(ct);

        deleted += await _db.DailyPrices
            .Where(p => p.ScrapeRunId != null && testRunIds.Contains(p.ScrapeRunId.Value))
            .ExecuteDeleteAsync(ct);

        deleted += await _db.IndexLevels
            .Where(i => i.ScrapeRunId != null && testRunIds.Contains(i.ScrapeRunId.Value))
            .ExecuteDeleteAsync(ct);

        deleted += await _db.MacroIndicators
            .Where(m => m.ScrapeRunId != null && testRunIds.Contains(m.ScrapeRunId.Value))
            .ExecuteDeleteAsync(ct);

        // SET NULL on stocks (don't delete stocks themselves)
        await _db.Stocks
            .Where(s => s.ScrapeRunId != null && testRunIds.Contains(s.ScrapeRunId.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ScrapeRunId, (Guid?)null), ct);

        // Finally delete the scrape runs
        deleted += await _db.ScrapeRuns
            .Where(r => testRunIds.Contains(r.Id))
            .ExecuteDeleteAsync(ct);

        return deleted;
    }
}
