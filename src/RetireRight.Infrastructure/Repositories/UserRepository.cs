using Microsoft.EntityFrameworkCore;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly RetireRightDbContext _db;

    public UserRepository(RetireRightDbContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default)
        => _db.Users.AnyAsync(ct);

    public Task<int> CountAsync(CancellationToken ct = default)
        => _db.Users.CountAsync(ct);

    public Task<User?> GetFirstAdminAsync(CancellationToken ct = default)
        => _db.Users
            .Where(u => u.IsAdmin)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<User> CreateAsync(User user, CancellationToken ct = default)
    {
        user.Email = user.Email.ToLowerInvariant();
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
