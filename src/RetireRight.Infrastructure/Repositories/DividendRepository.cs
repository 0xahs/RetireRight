using Microsoft.EntityFrameworkCore;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class DividendRepository : IDividendRepository
{
    private readonly RetireRightDbContext _db;

    public DividendRepository(RetireRightDbContext db) => _db = db;

    public async Task<IReadOnlyList<Dividend>> GetUpcomingAsync(int days = 30, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var cutoff = today.AddDays(days);

        return await _db.Dividends
            .Include(d => d.Stock)
            .Where(d => d.ExDate >= today && d.ExDate <= cutoff)
            .OrderBy(d => d.ExDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Dividend>> GetHistoryAsync(int stockId, CancellationToken ct = default)
        => await _db.Dividends
            .Where(d => d.StockId == stockId)
            .OrderByDescending(d => d.ExDate)
            .ToListAsync(ct);

    public async Task UpsertAsync(Dividend dividend, CancellationToken ct = default)
    {
        var existing = await _db.Dividends
            .FirstOrDefaultAsync(d =>
                d.StockId == dividend.StockId &&
                d.ExDate == dividend.ExDate, ct);

        if (existing is null)
        {
            dividend.CreatedAt = DateTime.UtcNow;
            _db.Dividends.Add(dividend);
        }
        else
        {
            // Merge: a new row from one source must NOT null-out fields another source
            // already populated. The universe scraper (StockAnalysisList) carries only
            // ex_date + dps + frequency; the per-symbol scrapers (Sarmaaya/PsxTerminal)
            // add payment_date and record_date. Whichever runs last shouldn't erase.
            existing.RecordDate   = dividend.RecordDate   ?? existing.RecordDate;
            existing.PaymentDate  = dividend.PaymentDate  ?? existing.PaymentDate;
            existing.Dps          = dividend.Dps;  // always present
            existing.Percentage   = dividend.Percentage   ?? existing.Percentage;
            existing.Year         = dividend.Year         ?? existing.Year;
            existing.Frequency    = dividend.Frequency    ?? existing.Frequency;
            existing.DividendType = dividend.DividendType;
            existing.Source       = dividend.Source;
            existing.ScrapeRunId  = dividend.ScrapeRunId;
            existing.UpdatedAt    = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
