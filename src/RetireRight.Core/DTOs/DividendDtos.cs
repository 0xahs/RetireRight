namespace RetireRight.Core.DTOs;

public record DividendSummaryDto(
    DateOnly ExDate,
    DateOnly? PaymentDate,
    decimal Dps,
    decimal? Percentage,
    string DividendType,
    string? Year,
    string Source);

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
    int DaysUntilEx);
