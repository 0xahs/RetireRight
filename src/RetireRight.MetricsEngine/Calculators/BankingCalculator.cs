using RetireRight.Core.Constants;

namespace RetireRight.MetricsEngine.Calculators;

/// <summary>
/// Category H: Banking Sector Metrics (H1-H15).
/// Only runs for Banking and IslamicBanking sectors.
/// Replaces meaningless CF/debt metrics with bank-appropriate KPIs.
/// </summary>
public class BankingCalculator : IMetricCalculator
{
    public string Category => "H_Banking";

    private static bool IsBankingSector(string? sector) =>
        sector is not null &&
        (sector.Contains("Bank", StringComparison.OrdinalIgnoreCase) ||
         sector.Contains("Islamic", StringComparison.OrdinalIgnoreCase));

    public Task<Dictionary<string, decimal?>> ComputeAsync(MetricContext ctx, CancellationToken ct = default)
    {
        if (!IsBankingSector(ctx.Sector))
            return Task.FromResult(new Dictionary<string, decimal?>());

        var m = new Dictionary<string, decimal?>();
        var f = ctx.Fundamentals;

        var nii = f.GetValueOrDefault(FundamentalKeys.NetInterestIncome);
        var nonIntIncome = f.GetValueOrDefault(FundamentalKeys.NonInterestIncome);
        var nonIntExpense = f.GetValueOrDefault(FundamentalKeys.NonInterestExpense);
        var totalAssets = f.GetValueOrDefault(FundamentalKeys.TotalAssets);
        var totalEquity = f.GetValueOrDefault(FundamentalKeys.TotalEquity);
        var netLoans = f.GetValueOrDefault(FundamentalKeys.NetLoans);
        var totalDeposits = f.GetValueOrDefault(FundamentalKeys.TotalDeposits);
        var totalInvestments = f.GetValueOrDefault(FundamentalKeys.TotalInvestments);

        // Total operating revenue = NII + non-interest income (denominator for several ratios)
        var operatingRevenue = nii is not null
            ? nii.Value + (nonIntIncome ?? 0)
            : (decimal?)null;

        // H1. Net Interest Margin = NII / Total Assets × 100
        m["h1_nim"] = (nii is not null && totalAssets is not null and not 0)
            ? nii / totalAssets * 100 : null;

        // H2. Cost-to-Income = Non-Interest Expense / Operating Revenue × 100
        m["h2_cost_to_income"] = (nonIntExpense is not null && operatingRevenue is not null and not 0)
            ? nonIntExpense / operatingRevenue * 100 : null;

        // H3. NPL Ratio (source-provided %)
        var nplRatio = f.GetValueOrDefault(FundamentalKeys.NplRatio);
        m["h3_npl_ratio"] = nplRatio;

        // H4. NPL to Equity = (NPL Ratio / 100) × Net Loans / Total Equity × 100
        //     Falls back to source-provided npl_to_equity if available
        m["h4_npl_to_equity"] = f.GetValueOrDefault("npl_to_equity")
            ?? ((nplRatio is not null && netLoans is not null && totalEquity is not null and not 0)
                ? nplRatio.Value / 100m * netLoans.Value / totalEquity.Value * 100
                : null);

        // H5. Capital Adequacy Ratio (source-provided %)
        m["h5_car"] = f.GetValueOrDefault(FundamentalKeys.CarRatio);

        // H6. Tier 1 Capital Ratio (source-provided %)
        m["h6_tier1_ratio"] = f.GetValueOrDefault(FundamentalKeys.Tier1Ratio);

        // H7. Liquidity Coverage Ratio (source-provided %)
        m["h7_lcr"] = f.GetValueOrDefault("lcr");

        // H8. Advance-to-Deposit Ratio = Net Loans / Total Deposits × 100 (or source-provided)
        m["h8_adr"] = f.GetValueOrDefault("loans_to_deposits")
            ?? ((netLoans is not null && totalDeposits is not null and not 0)
                ? netLoans / totalDeposits * 100 : null);

        // H9. Loans YoY Growth % (source-provided)
        m["h9_loans_growth"] = f.GetValueOrDefault("loans_yoy_growth");

        // H10. Fee Income Ratio = Non-Interest Income / Operating Revenue × 100
        m["h10_fee_income_ratio"] = (nonIntIncome is not null && operatingRevenue is not null and not 0)
            ? nonIntIncome / operatingRevenue * 100 : null;

        // H11. Return on Assets (source-provided %)
        m["h11_roa"] = f.GetValueOrDefault(FundamentalKeys.RoaPct);

        // H12. Return on Equity (source-provided %)
        m["h12_roe"] = f.GetValueOrDefault(FundamentalKeys.RoePct);

        // H13. Leverage Ratio Basel III (source-provided %)
        m["h13_leverage_ratio"] = f.GetValueOrDefault("leverage_ratio_basel3");

        // H14. Invested Assets to Deposits = Total Investments / Total Deposits × 100 (or source-provided)
        m["h14_invested_assets_to_deposits"] = f.GetValueOrDefault("invested_assets_to_deposits")
            ?? ((totalInvestments is not null && totalDeposits is not null and not 0)
                ? totalInvestments / totalDeposits * 100 : null);

        // H15. Forex Income as % of NII = Forex Income / NII × 100
        var forexIncome = f.GetValueOrDefault("forex_income");
        m["h15_forex_income_pct"] = (forexIncome is not null && nii is not null and not 0)
            ? forexIncome / nii * 100 : null;

        return Task.FromResult(m);
    }
}
