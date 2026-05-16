namespace RetireRight.Core.DTOs;

public record StockSummaryDto(
    int Id,
    string Symbol,
    string Name,
    string Sector,
    decimal? Price,
    decimal? ChangePct,
    decimal? CompositeScore,
    decimal? DividendSafetyScore,
    decimal? DividendGrowthScore,
    decimal? ValuationScore,
    decimal? OpportunityScore,
    string? Signal,
    decimal? DividendYield,
    decimal? DividendCoverage,
    string[] OpportunityFlags,
    DateTime? ScrapedAt,
    DateTime? ComputedAt);

public record StockDetailDto(
    StockSummaryDto Summary,
    StockMetricsDto Metrics,
    DividendSummaryDto[] RecentDividends,
    PricePointDto[] PriceHistory);

public record PricePointDto(
    DateOnly Date,
    decimal? Open,
    decimal? High,
    decimal? Low,
    decimal Close,
    long? Volume,
    decimal? ChangePct);
