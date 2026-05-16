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
    DateOnly? PaymentDate,
    decimal Dps,
    int DaysUntilEx);
