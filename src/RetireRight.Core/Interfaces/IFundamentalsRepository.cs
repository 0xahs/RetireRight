using RetireRight.Core.Models;

namespace RetireRight.Core.Interfaces;

public interface IFundamentalsRepository
{
    Task<StockFundamental?> GetLatestAsync(int stockId, string? periodicity = null, CancellationToken ct = default);
    Task UpsertAsync(StockFundamental fundamental, CancellationToken ct = default);
}

public interface IOwnershipRepository
{
    Task<StockOwnership?> GetLatestAsync(int stockId, CancellationToken ct = default);
    Task UpsertAsync(StockOwnership ownership, CancellationToken ct = default);
}

public interface IInsiderTransactionRepository
{
    Task<IReadOnlyList<InsiderTransaction>> GetRecentAsync(int stockId, int days = 90, CancellationToken ct = default);
    Task UpsertAsync(InsiderTransaction transaction, CancellationToken ct = default);
}

public interface IScrapeRunRepository
{
    Task<ScrapeRun> CreateAsync(ScrapeRun run, CancellationToken ct = default);
    Task UpdateAsync(ScrapeRun run, CancellationToken ct = default);

    /// <summary>
    /// Deletes all data associated with test scrape runs, then the runs themselves.
    /// Per-table DELETE WHERE ScrapeRunId IN (test run IDs), then delete scrape_runs.
    /// </summary>
    Task<int> DeleteTestDataAsync(CancellationToken ct = default);
}

public interface IIndexLevelRepository
{
    Task<IndexLevel?> GetLatestAsync(string indexName, CancellationToken ct = default);
    Task<IReadOnlyList<IndexLevel>> GetRangeAsync(string indexName, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task UpsertAsync(IndexLevel level, CancellationToken ct = default);
}

public interface IComputedMetricsRepository
{
    Task<ComputedMetrics?> GetLatestAsync(int stockId, CancellationToken ct = default);
    Task UpsertAsync(ComputedMetrics metrics, CancellationToken ct = default);
}
