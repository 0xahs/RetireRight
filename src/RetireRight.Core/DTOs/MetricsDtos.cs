namespace RetireRight.Core.DTOs;

public record StockMetricsDto(
    DividendSafetyMetrics DividendSafety,
    DividendGrowthMetrics DividendGrowth,
    ValuationMetrics Valuation,
    RiskMetrics Risk,
    BankingMetrics? Banking,
    OpportunityMetrics Opportunity,
    ScoreBreakdown Scores);

public record DividendSafetyMetrics(
    decimal? EarningsPayoutRatio,
    decimal? DividendCoverage,
    decimal? InterestCoverage,
    decimal? DebtToEquity,
    decimal? DebtToEbitda,
    decimal? EquityRatio,
    decimal? AltmanZ,
    decimal? ConsecutiveDivYears,
    decimal? RetainedEarningsRatio,
    decimal? PayoutStability);

public record DividendGrowthMetrics(
    decimal? DpsYoyGrowth,
    decimal? DpsCagr3y,
    decimal? DpsCagr5y,
    decimal? EpsGrowth1y,
    decimal? RevenueGrowth1y,
    decimal? BookValueGrowth,
    decimal? RoeTrend);

public record ValuationMetrics(
    decimal? PeRatio,
    decimal? PbRatio,
    decimal? PsRatio,
    decimal? DividendYield,
    decimal? EarningsYield,
    decimal? GrahamNumber,
    decimal? MarginOfSafety,
    decimal? Roe,
    decimal? Roa,
    decimal? PeVsIndustry,
    decimal? YieldVsIndustry,
    decimal? PriceVs52wHigh,
    decimal? PriceVs52wLow);

public record RiskMetrics(
    decimal? Volatility1y,
    decimal? MaxDrawdown,
    decimal? Beta,
    decimal? Rsi14,
    decimal? PriceVs50dma,
    decimal? PriceVs200dma,
    decimal? CurrentRatio,
    decimal? QuickRatio);

public record BankingMetrics(
    decimal? Nim,
    decimal? CostToIncome,
    decimal? NplRatio,
    decimal? NplToEquity,
    decimal? Car,
    decimal? Tier1Ratio,
    decimal? Lcr,
    decimal? Adr,
    decimal? LoansGrowth,
    decimal? FeeIncomeRatio,
    decimal? Roa,
    decimal? Roe,
    decimal? LeverageRatio,
    decimal? InvestedAssetsToDeposits,
    decimal? ForexIncomePct);

public record OpportunityMetrics(
    decimal? RangePosition52w,
    decimal? DrawdownFrom52wHigh,
    decimal? QualitySignal,
    decimal? ValueSignal,
    decimal? TechnicalSignal,
    decimal? OpportunityScore,
    string[] Flags);

public record ScoreBreakdown(
    decimal? Composite,
    decimal? Safety,
    decimal? Growth,
    decimal? Valuation);

public record ReturnMetrics(
    decimal? Return1M,
    decimal? Return6M,
    decimal? ReturnYtd,
    decimal? Return1Y,
    decimal? Return5Y,
    decimal? Return10Y);

public record CompanyProfileDto(
    decimal? MarketCapM,
    decimal? NetCashDebtM,
    decimal? Employees,
    decimal? FoundedYear,
    decimal? Week52Low,
    decimal? Week52High);
