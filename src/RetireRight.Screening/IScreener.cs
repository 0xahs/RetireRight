namespace RetireRight.Screening;

/// <summary>
/// Applies filter criteria to a set of stocks and returns ranked results.
/// </summary>
public interface IScreener
{
    Task<ScreenResult> ScreenAsync(ScreenCriteria criteria, CancellationToken ct = default);
}

public class ScreenCriteria
{
    /// <summary>Named preset: "dividend_safety", "high_yield", "growth", "value", "retirement_core"</summary>
    public string? Preset { get; init; }

    /// <summary>Custom metric filters: metric_key → (min, max)</summary>
    public Dictionary<string, (decimal? Min, decimal? Max)> Filters { get; init; } = new();

    /// <summary>Sort by this metric (descending by default)</summary>
    public string SortBy { get; init; } = "composite_score";

    /// <summary>Ascending sort instead of descending</summary>
    public bool Ascending { get; init; }

    /// <summary>Maximum results to return</summary>
    public int Limit { get; init; } = 20;

    /// <summary>Only include specific sectors</summary>
    public string[]? Sectors { get; init; }

    /// <summary>Only watchlist stocks</summary>
    public bool WatchlistOnly { get; init; }
}

public class ScreenResult
{
    public IReadOnlyList<ScreenedStock> Stocks { get; init; } = [];
    public int TotalMatched { get; init; }
    public int TotalEvaluated { get; init; }
    public string AppliedPreset { get; init; } = "";
    public Dictionary<string, (decimal? Min, decimal? Max)> AppliedFilters { get; init; } = new();
}

public class ScreenedStock
{
    public required string Symbol { get; init; }
    public required string Name { get; init; }
    public string? Sector { get; init; }
    public decimal? Price { get; init; }
    public decimal? CompositeScore { get; init; }
    public decimal? DividendYield { get; init; }
    public string? Signal { get; init; }
    public Dictionary<string, decimal?> SelectedMetrics { get; init; } = new();
    public int Rank { get; init; }
}
