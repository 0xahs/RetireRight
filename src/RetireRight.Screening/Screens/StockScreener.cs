using Microsoft.Extensions.Logging;

namespace RetireRight.Screening.Screens;

/// <summary>
/// Filters and ranks stocks based on computed metrics.
/// Works on the ComputedMetrics JSONB data stored in the database.
/// </summary>
public class StockScreener : IScreener
{
    private readonly IScreenDataProvider _dataProvider;
    private readonly ILogger<StockScreener> _logger;

    public StockScreener(IScreenDataProvider dataProvider, ILogger<StockScreener> logger)
    {
        _dataProvider = dataProvider;
        _logger = logger;
    }

    public async Task<ScreenResult> ScreenAsync(ScreenCriteria criteria, CancellationToken ct = default)
    {
        // Build effective filters
        var filters = new Dictionary<string, (decimal? Min, decimal? Max)>(criteria.Filters);
        var appliedPreset = "";

        if (criteria.Preset is not null)
        {
            appliedPreset = criteria.Preset;
            foreach (var (key, range) in ScreenPresets.Get(criteria.Preset))
            {
                filters.TryAdd(key, range);
            }
        }

        // Get all stocks with their latest metrics
        var stocks = await _dataProvider.GetScreenableStocksAsync(
            criteria.WatchlistOnly, criteria.Sectors, ct);

        var totalEvaluated = stocks.Count;

        // Apply filters
        var filtered = stocks.Where(s => PassesFilters(s.Metrics, filters)).ToList();

        // Sort
        var sorted = criteria.Ascending
            ? filtered.OrderBy(s => s.Metrics.GetValueOrDefault(criteria.SortBy))
            : filtered.OrderByDescending(s => s.Metrics.GetValueOrDefault(criteria.SortBy));

        // Rank and limit
        var ranked = sorted.Take(criteria.Limit).Select((s, i) => new ScreenedStock
        {
            Symbol = s.Symbol,
            Name = s.Name,
            Sector = s.Sector,
            Price = s.Price,
            CompositeScore = s.CompositeScore,
            DividendYield = s.Metrics.GetValueOrDefault("c9_dividend_yield"),
            Signal = s.Signal,
            SelectedMetrics = ExtractRelevantMetrics(s.Metrics, filters.Keys),
            Rank = i + 1
        }).ToList();

        _logger.LogInformation("Screen '{Preset}': {Matched}/{Total} stocks matched, top {Count} returned",
            appliedPreset, filtered.Count, totalEvaluated, ranked.Count);

        return new ScreenResult
        {
            Stocks = ranked,
            TotalMatched = filtered.Count,
            TotalEvaluated = totalEvaluated,
            AppliedPreset = appliedPreset,
            AppliedFilters = filters
        };
    }

    private static bool PassesFilters(
        Dictionary<string, decimal?> metrics,
        Dictionary<string, (decimal? Min, decimal? Max)> filters)
    {
        foreach (var (key, (min, max)) in filters)
        {
            var value = metrics.GetValueOrDefault(key);
            if (value is null) continue; // Skip null — don't exclude stocks with missing data

            if (min is not null && value < min) return false;
            if (max is not null && value > max) return false;
        }

        return true;
    }

    private static Dictionary<string, decimal?> ExtractRelevantMetrics(
        Dictionary<string, decimal?> all,
        IEnumerable<string> filterKeys)
    {
        var result = new Dictionary<string, decimal?>();

        // Always include core metrics
        var coreKeys = new[]
        {
            "c1_pe_ratio", "c3_pb_ratio", "c9_dividend_yield", "c12_roe",
            "a1_earnings_payout_ratio", "a4_dividend_coverage",
            "b3_dps_cagr_5y", "f2_annual_volatility",
            "o6_opportunity_score", "o7_flags"
        };

        foreach (var key in coreKeys.Union(filterKeys))
        {
            if (all.TryGetValue(key, out var val))
                result[key] = val;
        }

        return result;
    }
}
