namespace RetireRight.Core.DTOs;

public record ScreenerRequestDto(
    string? Preset,
    ScreenerFilterDto[]? Filters,
    string SortBy = "composite_score",
    bool Ascending = false,
    int Limit = 50,
    bool WatchlistOnly = true);

public record ScreenerFilterDto(string MetricKey, decimal? Min, decimal? Max);

public record ScreenerResultDto(
    ScreenerStockDto[] Stocks,
    int TotalMatched,
    int TotalEvaluated,
    string? AppliedPreset);

public record ScreenerStockDto(
    string Symbol,
    string Name,
    string Sector,
    string? Signal,
    decimal? CompositeScore,
    decimal? OpportunityScore,
    decimal? DividendYield,
    decimal? DividendCoverage,
    decimal? PeRatio,
    string[] OpportunityFlags,
    int Rank);
