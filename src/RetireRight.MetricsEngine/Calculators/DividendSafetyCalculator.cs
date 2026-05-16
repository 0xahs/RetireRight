namespace RetireRight.MetricsEngine.Calculators;

/// <summary>
/// Category A: Dividend Safety & Sustainability (A1-A16).
/// Answers: "Will this company keep paying its dividend?"
/// </summary>
public class DividendSafetyCalculator : IMetricCalculator
{
    public string Category => "A_DividendSafety";

    public Task<Dictionary<string, decimal?>> ComputeAsync(MetricContext ctx, CancellationToken ct = default)
    {
        var m = new Dictionary<string, decimal?>();
        var f = ctx.Fundamentals;

        // A1. Earnings Payout Ratio = DPS / EPS
        var dps = f.GetValueOrDefault("dps");
        var eps = f.GetValueOrDefault("eps");
        m["a1_earnings_payout_ratio"] = (dps is not null && eps is not null && eps != 0)
            ? dps / eps : null;

        // A2. FCF Payout Ratio = Dividends Paid / FCF
        var divPaid = f.GetValueOrDefault("total_dividends_paid");
        var ocf = f.GetValueOrDefault("operating_cash_flow");
        var capex = f.GetValueOrDefault("capital_expenditures");
        var fcf = (ocf is not null && capex is not null) ? ocf - Math.Abs(capex.Value) : f.GetValueOrDefault("free_cash_flow");
        m["a2_fcf_payout_ratio"] = (divPaid is not null && fcf is not null && fcf != 0)
            ? divPaid / fcf : null;

        // A3. OCF Payout Ratio = Dividends Paid / OCF
        m["a3_ocf_payout_ratio"] = (divPaid is not null && ocf is not null && ocf != 0)
            ? divPaid / ocf : null;

        // A4. Dividend Coverage = EPS / DPS
        m["a4_dividend_coverage"] = (eps is not null && dps is not null && dps != 0)
            ? eps / dps : null;

        // A5. FCF Coverage = FCF / Dividends Paid
        m["a5_fcf_coverage"] = (fcf is not null && divPaid is not null && divPaid != 0)
            ? fcf / divPaid : null;

        // A6. Interest Coverage = EBIT / Interest Expense (fallback to source-provided)
        var ebit = f.GetValueOrDefault("ebit") ?? f.GetValueOrDefault("operating_income");
        var interest = f.GetValueOrDefault("interest_expense");
        m["a6_interest_coverage"] = (ebit is not null && interest is not null && interest != 0)
            ? ebit / Math.Abs(interest.Value)
            : f.GetValueOrDefault("interest_coverage_ratio");

        // A7. Debt-to-Equity = Total Debt / Total Equity (fallback to source-provided)
        var totalDebt = f.GetValueOrDefault("total_debt");
        var totalEquity = f.GetValueOrDefault("total_equity");
        m["a7_debt_to_equity"] = (totalDebt is not null && totalEquity is not null && totalEquity != 0)
            ? totalDebt / totalEquity
            : f.GetValueOrDefault("debt_to_equity_ratio") is { } deRatio ? deRatio / 100m : null;

        // A8. Debt-to-EBITDA (can derive from EV/EBITDA if we have EV)
        var ebitda = f.GetValueOrDefault("ebitda");
        m["a8_debt_to_ebitda"] = (totalDebt is not null && ebitda is not null && ebitda != 0)
            ? totalDebt / ebitda : null;

        // A9. Net Debt-to-EBITDA = (Total Debt - Cash) / EBITDA
        var cash = f.GetValueOrDefault("cash_and_equivalents");
        m["a9_net_debt_to_ebitda"] = (totalDebt is not null && cash is not null && ebitda is not null && ebitda != 0)
            ? (totalDebt - cash) / ebitda : null;

        // A10. Debt-to-FCF
        m["a10_debt_to_fcf"] = (totalDebt is not null && fcf is not null && fcf != 0)
            ? totalDebt / fcf : null;

        // A12. Consecutive Years of Dividend Payment
        m["a12_consecutive_div_years"] = ComputeConsecutiveDividendYears(ctx.DividendHistory);

        // A13. Consecutive Years of Dividend Growth
        m["a13_consecutive_growth_years"] = ComputeConsecutiveGrowthYears(ctx.DividendHistory);

        // A14. Dividend Yield = Annual DPS / Current Price
        m["a14_dividend_yield"] = (dps is not null && ctx.CurrentPrice is not null && ctx.CurrentPrice != 0)
            ? (dps / ctx.CurrentPrice) * 100 : null;

        // A15. Cost of Equity vs Yield (simplified: yield above SBP rate is attractive)
        // Left as null — requires macro data injection in future phase

        // A16. Altman Z-Score (simplified — requires working capital, retained earnings, etc.)
        // Left as null for now — complex formula needing balance sheet data

        return Task.FromResult(m);
    }

    private static decimal? ComputeConsecutiveDividendYears(IReadOnlyList<DividendPoint> history)
    {
        if (history.Count == 0) return 0;

        var yearlyDps = history
            .GroupBy(d => d.ExDate.Year)
            .OrderByDescending(g => g.Key)
            .Select(g => new { Year = g.Key, TotalDps = g.Sum(d => d.Dps) })
            .ToList();

        var count = 0;
        for (var i = 0; i < yearlyDps.Count; i++)
        {
            if (yearlyDps[i].TotalDps <= 0) break;
            if (i > 0 && yearlyDps[i - 1].Year - yearlyDps[i].Year > 1) break;
            count++;
        }

        return count;
    }

    private static decimal? ComputeConsecutiveGrowthYears(IReadOnlyList<DividendPoint> history)
    {
        if (history.Count == 0) return 0;

        var yearlyDps = history
            .GroupBy(d => d.ExDate.Year)
            .OrderByDescending(g => g.Key)
            .Select(g => new { Year = g.Key, TotalDps = g.Sum(d => d.Dps) })
            .ToList();

        if (yearlyDps.Count < 2) return 0;

        var count = 0;
        for (var i = 0; i < yearlyDps.Count - 1; i++)
        {
            if (yearlyDps[i].Year - yearlyDps[i + 1].Year > 1) break;
            if (yearlyDps[i].TotalDps <= yearlyDps[i + 1].TotalDps) break;
            count++;
        }

        return count;
    }
}
