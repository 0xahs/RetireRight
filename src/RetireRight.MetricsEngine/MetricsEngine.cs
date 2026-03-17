using Microsoft.Extensions.Logging;

namespace RetireRight.MetricsEngine;

/// <summary>
/// Orchestrates all metric calculators and merges results into a single dictionary.
/// </summary>
public class MetricsEngine
{
    private readonly IEnumerable<IMetricCalculator> _calculators;
    private readonly ILogger<MetricsEngine> _logger;

    public MetricsEngine(IEnumerable<IMetricCalculator> calculators, ILogger<MetricsEngine> logger)
    {
        _calculators = calculators;
        _logger = logger;
    }

    public async Task<MetricsResult> ComputeAllAsync(MetricContext context, CancellationToken ct = default)
    {
        var allMetrics = new Dictionary<string, decimal?>();
        var errors = new List<string>();

        foreach (var calculator in _calculators)
        {
            try
            {
                var metrics = await calculator.ComputeAsync(context, ct);

                foreach (var (key, value) in metrics)
                    allMetrics[key] = value;

                _logger.LogDebug("Category {Category}: computed {Count} metrics for {Symbol}",
                    calculator.Category, metrics.Count, context.Symbol);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Calculator {Category} failed for {Symbol}", calculator.Category, context.Symbol);
                errors.Add($"{calculator.Category}: {ex.Message}");
            }
        }

        return new MetricsResult
        {
            Metrics = allMetrics,
            ComputedCategories = _calculators.Count() - errors.Count,
            TotalCategories = _calculators.Count(),
            Errors = errors
        };
    }
}

public class MetricsResult
{
    public Dictionary<string, decimal?> Metrics { get; init; } = new();
    public int ComputedCategories { get; init; }
    public int TotalCategories { get; init; }
    public List<string> Errors { get; init; } = [];
}
