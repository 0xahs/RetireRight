using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly RetireRightDbContext _db;

    public EmailVerificationTokenRepository(RetireRightDbContext db) => _db = db;

    public async Task<EmailVerificationToken> CreateAsync(int userId, TimeSpan ttl, CancellationToken ct = default)
    {
        // 32 cryptographically-random bytes → 43-char URL-safe base64. Unique index
        // on `token` enforces uniqueness; collisions are astronomically unlikely.
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var entity = new EmailVerificationToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.Add(ttl),
        };
        _db.EmailVerificationTokens.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public Task<EmailVerificationToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => _db.EmailVerificationTokens.FirstOrDefaultAsync(t => t.Token == token, ct);

    public async Task MarkUsedAsync(int tokenId, CancellationToken ct = default)
    {
        var token = await _db.EmailVerificationTokens.FindAsync([tokenId], ct);
        if (token is null) return;
        token.UsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task InvalidateAllForUserAsync(int userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var open = await _db.EmailVerificationTokens
            .Where(t => t.UserId == userId && t.UsedAt == null && t.ExpiresAt > now)
            .ToListAsync(ct);
        foreach (var t in open) t.UsedAt = now;
        if (open.Count > 0) await _db.SaveChangesAsync(ct);
    }
}
