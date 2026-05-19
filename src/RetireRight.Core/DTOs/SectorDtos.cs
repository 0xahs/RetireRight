namespace RetireRight.Core.DTOs;

public record SectorSummaryDto(
    string Sector,
    int StockCount,
    decimal? AvgCompositeScore,
    decimal? AvgDividendYield,
    decimal? AvgPeRatio,
    decimal? AvgOpportunityScore,
    SectorTopPickDto[] TopPicks);

public record SectorTopPickDto(
    string Symbol,
    string Name,
    decimal? Price,
    decimal? CompositeScore,
    decimal? DividendYield,
    string Verdict,
    string ConvictionLevel,
    string SafetyTier);
