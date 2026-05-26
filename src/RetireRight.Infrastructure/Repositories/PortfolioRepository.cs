using Microsoft.EntityFrameworkCore;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class PortfolioRepository : IPortfolioRepository
{
    private readonly RetireRightDbContext _db;

    public PortfolioRepository(RetireRightDbContext db) => _db = db;

    public async Task<IReadOnlyList<PortfolioHolding>> GetHoldingsAsync(int userId, CancellationToken ct = default)
        => await _db.PortfolioHoldings
            .Include(h => h.Stock)
            .Where(h => h.IsActive && h.UserId == userId)
            .OrderBy(h => h.Stock!.Symbol)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PortfolioTransaction>> GetTransactionsAsync(
        int userId, int? stockId = null, CancellationToken ct = default)
    {
        var q = _db.PortfolioTransactions
            .Include(t => t.Stock)
            .Where(t => t.UserId == userId);
        if (stockId.HasValue)
            q = q.Where(t => t.StockId == stockId.Value);
        return await q.OrderByDescending(t => t.TransactionDate).ToListAsync(ct);
    }

    public async Task<PortfolioHolding> UpsertHoldingAsync(PortfolioHolding holding, CancellationToken ct = default)
    {
        if (holding.UserId is null)
            throw new ArgumentException("UserId is required on holdings.", nameof(holding));

        var existing = await _db.PortfolioHoldings
            .FirstOrDefaultAsync(h => h.StockId == holding.StockId && h.UserId == holding.UserId && h.IsActive, ct);

        if (existing is null)
        {
            _db.PortfolioHoldings.Add(holding);
        }
        else
        {
            existing.Shares = holding.Shares;
            existing.AvgCost = holding.AvgCost;
            existing.Platform = holding.Platform;
            existing.PurchaseDate = holding.PurchaseDate;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return existing ?? holding;
    }

    public async Task<PortfolioTransaction> AddTransactionAsync(
        PortfolioTransaction tx, CancellationToken ct = default)
    {
        if (tx.UserId is null)
            throw new ArgumentException("UserId is required on transactions.", nameof(tx));
        _db.PortfolioTransactions.Add(tx);
        await _db.SaveChangesAsync(ct);
        return tx;
    }

    public async Task<bool> DeactivateHoldingAsync(int userId, int holdingId, CancellationToken ct = default)
    {
        var holding = await _db.PortfolioHoldings
            .FirstOrDefaultAsync(h => h.Id == holdingId && h.UserId == userId, ct);
        if (holding is null) return false;
        holding.IsActive = false;
        holding.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
