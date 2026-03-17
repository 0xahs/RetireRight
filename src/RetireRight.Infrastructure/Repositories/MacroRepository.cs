using Microsoft.EntityFrameworkCore;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class MacroRepository : IMacroRepository
{
    private readonly RetireRightDbContext _db;

    public MacroRepository(RetireRightDbContext db) => _db = db;

    public async Task<MacroIndicator?> GetLatestAsync(string indicatorName, CancellationToken ct = default)
        => await _db.MacroIndicators
            .Where(m => m.IndicatorName == indicatorName)
            .OrderByDescending(m => m.Date)
            .FirstOrDefaultAsync(ct);

    public async Task UpsertAsync(MacroIndicator indicator, CancellationToken ct = default)
    {
        var existing = await _db.MacroIndicators
            .FirstOrDefaultAsync(m =>
                m.IndicatorName == indicator.IndicatorName &&
                m.Date == indicator.Date, ct);

        if (existing is null)
        {
            indicator.CreatedAt = DateTime.UtcNow;
            _db.MacroIndicators.Add(indicator);
        }
        else
        {
            existing.Value = indicator.Value;
            existing.Unit = indicator.Unit;
            existing.Source = indicator.Source;
            existing.ScrapeRunId = indicator.ScrapeRunId;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
