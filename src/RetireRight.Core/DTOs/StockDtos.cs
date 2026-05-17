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
    DateTime? ComputedAt,
    // ── Signal grammar (CapyTrade-evolution layer) ─────────────
    string Verdict,           // ACCUMULATE / HOLD / REVIEW / AVOID / WATCH
    decimal Confidence,        // 0..1
    string ConvictionLevel,    // HIGH / MEDIUM / LOW
    string SafetyTier,         // S / A / B / C / D
    decimal? NetYield,         // gross × 0.85 (15% filer WHT)
    string? WhyLine,           // ≤80 chars natural-language positive framing
    string? Risks,             // ≤80 chars natural-language risk framing
    int MetricCompleteness,    // 0..100 — % of populated metrics
    int MetricsTotal);         // expected total (77 non-bank, 92 banks)

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
