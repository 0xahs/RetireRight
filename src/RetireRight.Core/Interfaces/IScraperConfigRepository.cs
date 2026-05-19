using RetireRight.Core.Models;

namespace RetireRight.Core.Interfaces;

public interface IScraperConfigRepository
{
    Task<List<DataSourceConfig>> GetAllAsync(CancellationToken ct = default);
    Task<List<DataSourceConfig>> GetEnabledAsync(CancellationToken ct = default);
    Task<DataSourceConfig?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<DataSourceConfig> UpsertAsync(DataSourceConfig config, CancellationToken ct = default);
    Task<bool> SetEnabledAsync(string name, bool enabled, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
