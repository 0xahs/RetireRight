namespace RetireRight.Core.DTOs;

public record DividendSummaryDto(
    DateOnly ExDate,
    DateOnly? PaymentDate,
    decimal Dps,
    decimal? Percentage,
    string DividendType,
    string? Year,
    string Source,
    /// <summary>True when the DPS is a scraper estimate (info-card-only announcement
    /// whose history-table row hasn't been published yet by the source).</summary>
    bool IsEstimated);

public record UpcomingDividendDto(
    string Symbol,
    string StockName,
    string Sector,
    DateOnly ExDate,
    /// <summary>Last calendar day a buy will make you eligible for the dividend
    /// (PSX T+2 settlement → ExDate - 1). Same convention as BuildExportDataAsync.</summary>
    DateOnly LastBuyDate,
    DateOnly? PaymentDate,
    decimal Dps,
    int DaysUntilEx,
    /// <summary>True when the DPS is a scraper estimate; UI renders an "EST" badge.</summary>
    bool IsEstimated);
