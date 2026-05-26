using Microsoft.EntityFrameworkCore;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class StockRepository : IStockRepository
{
    private readonly RetireRightDbContext _db;

    public StockRepository(RetireRightDbContext db) => _db = db;

    public async Task<Stock?> GetBySymbolAsync(string symbol, CancellationToken ct = default)
        => await _db.Stocks.FirstOrDefaultAsync(s => s.Symbol == symbol, ct);

    public async Task<IReadOnlyList<Stock>> GetWatchlistAsync(CancellationToken ct = default)
        => await _db.Stocks.Where(s => s.IsWatchlist && s.IsActive).OrderBy(s => s.Symbol).ToListAsync(ct);

    public async Task<IReadOnlyList<Stock>> GetAllActiveAsync(CancellationToken ct = default)
        => await _db.Stocks.Where(s => s.IsActive).OrderBy(s => s.Symbol).ToListAsync(ct);

    public async Task<bool> SetOperationalWatchlistAsync(string symbol, bool isWatchlist, CancellationToken ct = default)
    {
        var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.Symbol == symbol, ct);
        if (stock is null) return false;
        stock.IsWatchlist = isWatchlist;
        stock.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Stock> UpsertAsync(Stock stock, CancellationToken ct = default)
    {
        var existing = await _db.Stocks.FirstOrDefaultAsync(s => s.Symbol == stock.Symbol, ct);

        if (existing is null)
        {
            stock.CreatedAt = DateTime.UtcNow;
            stock.UpdatedAt = DateTime.UtcNow;
            _db.Stocks.Add(stock);
        }
        else
        {
            existing.Name = stock.Name;
            existing.Sector = stock.Sector;
            existing.Industry = stock.Industry;
            existing.IsActive = stock.IsActive;
            existing.IsWatchlist = stock.IsWatchlist;
            existing.CircuitLimitPct = stock.CircuitLimitPct;
            existing.LotSize = stock.LotSize;
            existing.TradingBoard = stock.TradingBoard;
            existing.UpdatedAt = DateTime.UtcNow;
            stock = existing;
        }

        await _db.SaveChangesAsync(ct);
        return stock;
    }
}
