namespace RetireRight.MetricsEngine;

/// <summary>
/// Each calculator handles one category of metrics (A through M).
/// Returns computed key-value pairs to be merged into the ComputedMetrics JSONB.
/// </summary>
public interface IMetricCalculator
{
    string Category { get; }
    Task<Dictionary<string, decimal?>> ComputeAsync(MetricContext context, CancellationToken ct = default);
}

/// <summary>
/// All data a calculator needs — pre-loaded by the engine to avoid repeated DB queries.
/// </summary>
public class MetricContext
{
    public required int StockId { get; init; }
    public required string Symbol { get; init; }

    // Latest price data
    public decimal? CurrentPrice { get; init; }
    public decimal? Volume { get; init; }

    // Historical prices (most recent first)
    public IReadOnlyList<PricePoint> PriceHistory { get; init; } = [];

    // Dividend history (most recent first)
    public IReadOnlyList<DividendPoint> DividendHistory { get; init; } = [];

    // Fundamental data (from scraped sources, keyed by field name)
    public Dictionary<string, decimal?> Fundamentals { get; init; } = new();

    // Sector string (as stored in DB — e.g. "Commercial Banks", "Banking Services")
    public string? Sector { get; init; }
}

public record PricePoint(DateOnly Date, decimal Close, decimal? High, decimal? Low, long? Volume);
public record DividendPoint(DateOnly ExDate, decimal Dps, string? DividendType);
