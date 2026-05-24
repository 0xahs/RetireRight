namespace RetireRight.MetricsEngine.Calculators;

/// <summary>
/// Category E: Growth Quality (E1-E20).
/// Answers: "Is the company growing profitably and sustainably — i.e. can it fund
/// a rising dividend out of its own returns without raising external capital?"
///
/// Several canonical E metrics are already emitted by other calculators (each
/// calculator only sees raw fundamentals, not its siblings' output, so they are NOT
/// re-derived here to avoid duplicate keys):
///   E1  ROE            → c12_roe   (ValuationCalculator)
///   E2  ROA            → c13_roa
///   E3  ROIC           → c14_roic
///   E6  Gross Margin   → c15_gross_margin
///   E7  Operating Mgn  → c16_operating_margin
///   E8  Net Margin     → c17_net_margin
///   E10 FCF Margin     → c18_fcf_margin
///   E11 Asset Turnover → d7_asset_turnover (FinancialHealthCalculator)
/// E5 (ROIC − WACC spread) is omitted: WACC has no data source yet.
///
/// Unit conventions match the rest of the engine: returns/margins are percentages
/// (×100), payout/retention are fractions (0..1), turnovers are raw ratios.
/// All balance-sheet inputs use the ending (point-in-time) value — multi-year
/// averages aren't available until the multi-year financials pipeline lands.
/// </summary>
public class GrowthQualityCalculator : IMetricCalculator
{
    public string Category => "E_GrowthQuality";

    public Task<Dictionary<string, decimal?>> ComputeAsync(MetricContext ctx, CancellationToken ct = default)
    {
        var m = new Dictionary<string, decimal?>();
        var f = ctx.Fundamentals;

        var netIncome = f.GetValueOrDefault("net_income");
        var revenue = f.GetValueOrDefault("revenue");
        var totalAssets = f.GetValueOrDefault("total_assets");
        var equity = f.GetValueOrDefault("total_equity");
        var currentLiabilities = f.GetValueOrDefault("current_liabilities");
        var ebit = f.GetValueOrDefault("ebit") ?? f.GetValueOrDefault("operating_income");
        var ebitda = f.GetValueOrDefault("ebitda");
        var pretax = f.GetValueOrDefault("pretax_income");
        var cogs = f.GetValueOrDefault("cost_of_goods_sold");
        var inventory = f.GetValueOrDefault("inventory");
        var employees = f.GetValueOrDefault("employees");
        var dps = f.GetValueOrDefault("dps");
        var eps = f.GetValueOrDefault("eps");
        var ocf = f.GetValueOrDefault("operating_cash_flow");
        var capex = f.GetValueOrDefault("capital_expenditures");
        var capexAbs = capex is not null ? Math.Abs(capex.Value) : (decimal?)null;

        // E4. Return on Capital Employed = EBIT / (Total Assets − Current Liabilities) × 100
        var capitalEmployed = (totalAssets is not null && currentLiabilities is not null)
            ? totalAssets - currentLiabilities : null;
        m["e4_roce"] = (ebit is not null && capitalEmployed is > 0)
            ? (ebit / capitalEmployed) * 100 : null;

        // E9. EBITDA Margin = EBITDA / Revenue × 100
        m["e9_ebitda_margin"] = (ebitda is not null && revenue is > 0)
            ? (ebitda / revenue) * 100 : null;

        // E12. Inventory Turnover = COGS / Inventory (ending)
        m["e12_inventory_turnover"] = (cogs is > 0 && inventory is > 0)
            ? cogs / inventory : null;

        // E13. Revenue Per Employee (revenue is in millions PKR → result in M PKR/employee)
        m["e13_revenue_per_employee"] = (revenue is not null && employees is > 0)
            ? revenue / employees : null;

        // E14. Profit Per Employee
        m["e14_profit_per_employee"] = (netIncome is not null && employees is > 0)
            ? netIncome / employees : null;

        // E15. DuPont (3-factor): ROE reconstructed = Net Margin × Asset Turnover × Equity Multiplier × 100.
        // Emitted as a cross-check on c12_roe and to confirm all three drivers are present.
        m["e15_dupont_roe"] = (netIncome is not null && revenue is > 0 && totalAssets is > 0 && equity is not null && equity != 0)
            ? (netIncome.Value / revenue.Value)        // net margin (fraction)
              * (revenue.Value / totalAssets.Value)    // asset turnover
              * (totalAssets.Value / equity.Value)     // equity multiplier
              * 100m
            : null;

        // E16. DuPont (5-factor) — the pieces not already available elsewhere.
        //   Tax burden = Net Income / Pretax Income   (lower = heavier tax drag)
        //   Interest burden = Pretax Income / EBIT     (lower = heavier interest drag)
        //   EBIT margin = EBIT / Revenue × 100
        // (Asset turnover and equity multiplier reuse d7/d8.)
        m["e16_tax_burden"] = (netIncome is not null && pretax is not null && pretax != 0)
            ? netIncome / pretax : null;
        m["e16_interest_burden"] = (pretax is not null && ebit is not null && ebit != 0)
            ? pretax / ebit : null;
        m["e16_ebit_margin"] = (ebit is not null && revenue is > 0)
            ? (ebit / revenue) * 100 : null;

        // Payout ratio as a fraction — DPS/EPS (matches a1), falling back to the
        // source-provided percentage. Used by SGR and plowback.
        var payout = (dps is not null && eps is not null && eps != 0)
            ? dps / eps
            : f.GetValueOrDefault("dividend_payout_ratio") is { } pr ? pr / 100m : (decimal?)null;

        // ROE as a fraction for SGR — net_income/equity, fallback to source roe_pct.
        var roeFraction = (netIncome is not null && equity is not null && equity != 0)
            ? netIncome / equity
            : f.GetValueOrDefault("roe_pct") is { } rp ? rp / 100m : (decimal?)null;

        // E17. Sustainable Growth Rate = ROE × (1 − Payout) × 100  (max self-funded growth, %)
        m["e17_sustainable_growth_rate"] = (roeFraction is not null && payout is not null)
            ? roeFraction * (1m - payout) * 100m : null;

        // E18. Reinvestment / Plowback Ratio = 1 − Payout  (fraction of earnings retained)
        m["e18_plowback_ratio"] = payout is not null ? 1m - payout : null;

        // E19. CapEx as % of Revenue (capital intensity)
        m["e19_capex_to_revenue"] = (capexAbs is not null && revenue is > 0)
            ? (capexAbs / revenue) * 100 : null;

        // E20. CapEx as % of Operating Cash Flow (how much OCF is eaten before dividends)
        m["e20_capex_to_ocf"] = (capexAbs is not null && ocf is not null && ocf != 0)
            ? (capexAbs / ocf) * 100 : null;

        return Task.FromResult(m);
    }
}
