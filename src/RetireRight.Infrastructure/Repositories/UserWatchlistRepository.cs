using Microsoft.EntityFrameworkCore;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class UserWatchlistRepository : IUserWatchlistRepository
{
    private readonly RetireRightDbContext _db;

    public UserWatchlistRepository(RetireRightDbContext db) => _db = db;

    public Task<IReadOnlyList<int>> GetStockIdsAsync(int userId, CancellationToken ct = default)
        => _db.UserWatchlists
            .Where(w => w.UserId == userId)
            .Select(w => w.StockId)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<int>)t.Result, ct);

    public async Task<IReadOnlyList<Stock>> GetStocksAsync(int userId, CancellationToken ct = default)
        => await _db.UserWatchlists
            .Where(w => w.UserId == userId)
            .Include(w => w.Stock)
            .Select(w => w.Stock!)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Symbol)
            .ToListAsync(ct);

    public Task<bool> IsInWatchlistAsync(int userId, int stockId, CancellationToken ct = default)
        => _db.UserWatchlists.AnyAsync(w => w.UserId == userId && w.StockId == stockId, ct);

    public async Task<bool> ToggleAsync(int userId, int stockId, CancellationToken ct = default)
    {
        var existing = await _db.UserWatchlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.StockId == stockId, ct);
        if (existing is null)
        {
            _db.UserWatchlists.Add(new UserWatchlist { UserId = userId, StockId = stockId });
            await _db.SaveChangesAsync(ct);
            return true;
        }
        _db.UserWatchlists.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return false;
    }

    public Task<int> CountAsync(int userId, CancellationToken ct = default)
        => _db.UserWatchlists.CountAsync(w => w.UserId == userId, ct);
}
