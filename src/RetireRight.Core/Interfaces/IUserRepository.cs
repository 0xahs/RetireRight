using RetireRight.Core.Models;

namespace RetireRight.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetFirstAdminAsync(CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task<User> CreateAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
