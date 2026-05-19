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
            existing.RecordDate = dividend.RecordDate;
            existing.PaymentDate = dividend.PaymentDate;
            existing.Dps = dividend.Dps;
            existing.Percentage = dividend.Percentage;
            existing.Year = dividend.Year;
            existing.Frequency = dividend.Frequency;
            existing.DividendType = dividend.DividendType;
            existing.Source = dividend.Source;
            existing.ScrapeRunId = dividend.ScrapeRunId;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
