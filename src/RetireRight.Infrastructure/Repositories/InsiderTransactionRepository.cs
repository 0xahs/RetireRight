using Microsoft.EntityFrameworkCore;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class InsiderTransactionRepository : IInsiderTransactionRepository
{
    private readonly RetireRightDbContext _db;

    public InsiderTransactionRepository(RetireRightDbContext db) => _db = db;

    public async Task<IReadOnlyList<InsiderTransaction>> GetRecentAsync(
        int stockId, int days = 90, CancellationToken ct = default)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days));

        return await _db.InsiderTransactions
            .Where(t => t.StockId == stockId && t.TransactionDate >= cutoff)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(InsiderTransaction transaction, CancellationToken ct = default)
    {
        var existing = await _db.InsiderTransactions
            .FirstOrDefaultAsync(t =>
                t.StockId == transaction.StockId &&
                t.PersonName == transaction.PersonName &&
                t.TransactionDate == transaction.TransactionDate &&
                t.Action == transaction.Action, ct);

        if (existing is null)
        {
            transaction.CreatedAt = DateTime.UtcNow;
            _db.InsiderTransactions.Add(transaction);
        }
        else
        {
            existing.Position = transaction.Position;
            existing.Quantity = transaction.Quantity;
            existing.Rate = transaction.Rate;
            existing.Source = transaction.Source;
            existing.ScrapeRunId = transaction.ScrapeRunId;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
