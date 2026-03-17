namespace RetireRight.MetricsEngine.Calculators;

/// <summary>
/// Category D: Financial Health (D1-D17).
/// Answers: "Is this company financially strong enough to sustain dividends?"
/// </summary>
public class FinancialHealthCalculator : IMetricCalculator
{
    public string Category => "D_FinancialHealth";

    public Task<Dictionary<string, decimal?>> ComputeAsync(MetricContext ctx, CancellationToken ct = default)
    {
        var m = new Dictionary<string, decimal?>();
        var f = ctx.Fundamentals;

        // D1. Current Ratio
        var currentAssets = f.GetValueOrDefault("current_assets");
        var currentLiabilities = f.GetValueOrDefault("current_liabilities");
        m["d1_current_ratio"] = (currentAssets is not null && currentLiabilities is not null && currentLiabilities != 0)
            ? currentAssets / currentLiabilities : null;

        // D2. Quick Ratio
        var inventory = f.GetValueOrDefault("inventory");
        m["d2_quick_ratio"] = (currentAssets is not null && inventory is not null && currentLiabilities is not null && currentLiabilities != 0)
            ? (currentAssets - inventory) / currentLiabilities : null;

        // D3. Debt-to-Assets
        var totalDebt = f.GetValueOrDefault("total_debt");
        var totalAssets = f.GetValueOrDefault("total_assets");
        m["d3_debt_to_assets"] = (totalDebt is not null && totalAssets is not null && totalAssets != 0)
            ? totalDebt / totalAssets : null;

        // D4. Equity Ratio
        var equity = f.GetValueOrDefault("total_equity");
        m["d4_equity_ratio"] = (equity is not null && totalAssets is not null && totalAssets != 0)
            ? equity / totalAssets : null;

        // D5. Working Capital
        m["d5_working_capital"] = (currentAssets is not null && currentLiabilities is not null)
            ? currentAssets - currentLiabilities : null;

        // D6. Cash-to-Debt
        var cash = f.GetValueOrDefault("cash_and_equivalents");
        m["d6_cash_to_debt"] = (cash is not null && totalDebt is not null && totalDebt != 0)
            ? cash / totalDebt : null;

        // D7. Asset Turnover = Revenue / Total Assets
        var revenue = f.GetValueOrDefault("revenue");
        m["d7_asset_turnover"] = (revenue is not null && totalAssets is not null && totalAssets != 0)
            ? revenue / totalAssets : null;

        // D8. Equity Multiplier = Total Assets / Equity
        m["d8_equity_multiplier"] = (totalAssets is not null && equity is not null && equity != 0)
            ? totalAssets / equity : null;

        // D9. Cash Flow to Debt = OCF / Total Debt
        var ocf = f.GetValueOrDefault("operating_cash_flow");
        m["d9_cf_to_debt"] = (ocf is not null && totalDebt is not null && totalDebt != 0)
            ? ocf / totalDebt : null;

        // D10. Retained Earnings Ratio = (Net Income - Dividends) / Net Income
        var netIncome = f.GetValueOrDefault("net_income");
        var divPaid = f.GetValueOrDefault("total_dividends_paid");
        m["d10_retained_earnings_ratio"] = (netIncome is not null && divPaid is not null && netIncome != 0)
            ? (netIncome - divPaid) / netIncome : null;

        return Task.FromResult(m);
    }
}
