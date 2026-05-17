namespace RetireRight.Core.DTOs;

/// <summary>
/// A monthly investment plan that allocates a budget across watchlist picks
/// using the signal grammar + portfolio rebalancing logic.
/// </summary>
public record MonthlyPlanDto(
    decimal Budget,
    DateTime GeneratedAt,
    PlanPickDto[] Picks,
    PlanSummaryDto Summary,
    string[] Notes);

public record PlanPickDto(
    string Symbol,
    string Name,
    string Sector,
    string Verdict,
    string ConvictionLevel,
    decimal? CompositeScore,
    decimal? OpportunityScore,
    decimal? CurrentPrice,
    int Shares,                       // floor of (allocatedAmount / currentPrice)
    decimal AllocatedAmount,          // shares * currentPrice (actual rupees spent, not target)
    decimal TargetAmount,             // budget * weight (before rounding to whole shares)
    decimal Weight,                   // 0..1
    decimal? ExpectedAnnualIncome,    // shares * DPS
    decimal? NetYield,
    int? ExDateInDays,                // null if none within 60 days
    string[] Reasons);                // 1-3 short rationale chips

public record PlanSummaryDto(
    decimal TotalAllocated,
    decimal Leftover,
    int PickCount,
    decimal ExpectedAnnualIncome,
    decimal? AverageNetYield);
