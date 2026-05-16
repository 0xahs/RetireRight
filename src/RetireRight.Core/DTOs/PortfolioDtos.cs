namespace RetireRight.Core.DTOs;

public record HoldingDto(
    int Id,
    string Symbol,
    string Name,
    string Sector,
    int Shares,
    decimal AvgCost,
    decimal? CurrentPrice,
    decimal? MarketValue,
    decimal? CostBasis,
    decimal? GainLoss,
    decimal? GainLossPct,
    decimal? YieldOnCost,
    decimal? AnnualIncome,
    decimal? SafetyScore,
    DateOnly? NextExDate,
    string? Platform,
    DateOnly? PurchaseDate);

public record TransactionDto(
    int Id,
    string Symbol,
    string Name,
    string Action,
    int Shares,
    decimal Price,
    decimal TotalCost,
    decimal? Commission,
    DateOnly TransactionDate,
    string? Platform);

public record PortfolioSummaryDto(
    decimal TotalCostBasis,
    decimal? TotalMarketValue,
    decimal? TotalGainLoss,
    decimal? TotalGainLossPct,
    decimal? ProjectedAnnualIncome,
    decimal? PortfolioYield,
    int HoldingCount);
