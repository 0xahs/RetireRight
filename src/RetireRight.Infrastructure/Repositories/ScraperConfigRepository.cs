using Microsoft.EntityFrameworkCore;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class ScraperConfigRepository(RetireRightDbContext db) : IScraperConfigRepository
{
    public Task<List<DataSourceConfig>> GetAllAsync(CancellationToken ct = default) =>
        db.DataSourceConfigs.OrderBy(c => c.Id).ToListAsync(ct);

    public Task<List<DataSourceConfig>> GetEnabledAsync(CancellationToken ct = default) =>
        db.DataSourceConfigs.Where(c => c.Enabled).OrderBy(c => c.Id).ToListAsync(ct);

    public Task<DataSourceConfig?> GetByNameAsync(string name, CancellationToken ct = default) =>
        db.DataSourceConfigs.FirstOrDefaultAsync(c => c.Name == name, ct);

    public async Task<DataSourceConfig> UpsertAsync(DataSourceConfig config, CancellationToken ct = default)
    {
        var existing = await db.DataSourceConfigs.FirstOrDefaultAsync(c => c.Name == config.Name, ct);
        if (existing is null)
        {
            config.CreatedAt = DateTime.UtcNow;
            config.UpdatedAt = DateTime.UtcNow;
            db.DataSourceConfigs.Add(config);
            await db.SaveChangesAsync(ct);
            return config;
        }

        existing.Type = config.Type;
        existing.Description = config.Description;
        existing.BaseUrl = config.BaseUrl;
        existing.Enabled = config.Enabled;
        existing.MaxRequestsPerMinute = config.MaxRequestsPerMinute;
        existing.MinDelayMs = config.MinDelayMs;
        existing.MaxDelayMs = config.MaxDelayMs;
        existing.RetryMaxAttempts = config.RetryMaxAttempts;
        existing.RetryInitialDelayMs = config.RetryInitialDelayMs;
        existing.RetryBackoffMultiplier = config.RetryBackoffMultiplier;
        if (!string.IsNullOrEmpty(config.CredentialEmail)) existing.CredentialEmail = config.CredentialEmail;
        if (!string.IsNullOrEmpty(config.CredentialPassword)) existing.CredentialPassword = config.CredentialPassword;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> SetEnabledAsync(string name, bool enabled, CancellationToken ct = default)
    {
        var config = await db.DataSourceConfigs.FirstOrDefaultAsync(c => c.Name == name, ct);
        if (config is null) return false;
        config.Enabled = enabled;
        config.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public Task<int> CountAsync(CancellationToken ct = default) =>
        db.DataSourceConfigs.CountAsync(ct);
}
