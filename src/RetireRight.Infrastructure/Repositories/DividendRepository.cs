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

    // Sources don't always agree on the canonical ex-date for the same dividend event.
    // Observed gaps (between PsxTerminal and StockAnalysisDividends for the same event):
    //   ATLH 2025-06: 1 day  (PT Jun 5, SA Jun 6)
    //   AABS 2026-05: 3 days (PT May 29, SA May 26)
    // PT appears to record the book-closure start / record date; SA records the actual
    // ex-trading date. ±3 covers both empirically and is still narrower than any real
    // dividend cadence (monthly = 30d gap, the only one that gets close).
    private const int DateMatchWindowDays = 3;

    public async Task UpsertAsync(Dividend dividend, CancellationToken ct = default, bool insertOnly = false)
    {
        // Look first for an exact (StockId, ExDate) match (the unique key); fall back to
        // a nearby-date match in the ±DateMatchWindowDays window for cross-source dedup.
        var lo = dividend.ExDate.AddDays(-DateMatchWindowDays);
        var hi = dividend.ExDate.AddDays(+DateMatchWindowDays);
        var existing = await _db.Dividends
            .Where(d => d.StockId == dividend.StockId && d.ExDate >= lo && d.ExDate <= hi)
            // Exact match first if present, otherwise the closest neighbour
            .OrderBy(d => d.ExDate == dividend.ExDate ? 0 : 1)
            .ThenBy(d => d.ExDate)
            .FirstOrDefaultAsync(ct);

        if (existing is null)
        {
            dividend.CreatedAt = DateTime.UtcNow;
            _db.Dividends.Add(dividend);
        }
        else if (insertOnly)
        {
            // Caller is a low-confidence seed (e.g. universe sweep with stale DPS).
            // Leave the existing authoritative row alone.
            return;
        }
        else
        {
            // Merge: a new row from one source must NOT null-out fields another source
            // already populated. The universe scraper (StockAnalysisList) carries only
            // ex_date + dps + frequency; the per-symbol scrapers (Sarmaaya/PsxTerminal)
            // add payment_date and record_date. Whichever runs last shouldn't erase.
            existing.RecordDate   = dividend.RecordDate   ?? existing.RecordDate;
            existing.PaymentDate  = dividend.PaymentDate  ?? existing.PaymentDate;
            // DPS authority rule — confirmed beats estimated regardless of write order.
            // Without this an estimated row (e.g. StockAnalysis info-card fallback Rs 46
            // for ATLH) could clobber a confirmed value (PsxTerminal homepage Rs 56)
            // depending on which scraper happens to run last. Both being confirmed (or
            // both estimated) falls through to last-writer-wins, matching the prior behaviour.
            var newWins = existing.IsEstimated || !dividend.IsEstimated;
            if (newWins)
            {
                existing.Dps         = dividend.Dps;
                existing.IsEstimated = dividend.IsEstimated;
            }
            existing.Percentage   = dividend.Percentage   ?? existing.Percentage;
            existing.Year         = dividend.Year         ?? existing.Year;
            existing.Frequency    = dividend.Frequency    ?? existing.Frequency;
            existing.DividendType = dividend.DividendType;
            existing.Source       = newWins ? dividend.Source : existing.Source;
            existing.ScrapeRunId  = dividend.ScrapeRunId;
            existing.UpdatedAt    = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
