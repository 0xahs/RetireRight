namespace RetireRight.MetricsEngine.Calculators;

/// <summary>
/// Category C: Valuation Metrics (C1-C26).
/// Answers: "Is this stock cheap or expensive relative to its fundamentals?"
/// </summary>
public class ValuationCalculator : IMetricCalculator
{
    public string Category => "C_Valuation";

    public Task<Dictionary<string, decimal?>> ComputeAsync(MetricContext ctx, CancellationToken ct = default)
    {
        var m = new Dictionary<string, decimal?>();
        var f = ctx.Fundamentals;
        var price = ctx.CurrentPrice;

        var eps = f.GetValueOrDefault("eps");
        var bvps = f.GetValueOrDefault("book_value_per_share");
        var dps = f.GetValueOrDefault("dps");
        var revenue = f.GetValueOrDefault("revenue");
        var sharesOut = f.GetValueOrDefault("shares_outstanding");

        // C1. P/E Ratio
        m["c1_pe_ratio"] = (price is not null && eps is not null && eps != 0)
            ? price / eps : null;

        // C2. Forward P/E
        var fwdEps = f.GetValueOrDefault("forward_eps");
        m["c2_forward_pe"] = (price is not null && fwdEps is not null && fwdEps != 0)
            ? price / fwdEps : null;

        // C3. P/B Ratio
        m["c3_pb_ratio"] = (price is not null && bvps is not null && bvps != 0)
            ? price / bvps : null;

        // C4. P/S Ratio (Price / Revenue per share)
        var rps = (revenue is not null && sharesOut is not null && sharesOut != 0)
            ? revenue / sharesOut : null;
        m["c4_ps_ratio"] = (price is not null && rps is not null && rps != 0)
            ? price / rps : null;

        // C5. P/FCF Ratio
        var fcfPerShare = f.GetValueOrDefault("fcf_per_share");
        m["c5_p_fcf_ratio"] = (price is not null && fcfPerShare is not null && fcfPerShare != 0)
            ? price / fcfPerShare : null;

        // C6. EV/EBITDA
        var ev = f.GetValueOrDefault("enterprise_value");
        var ebitda = f.GetValueOrDefault("ebitda");
        m["c6_ev_ebitda"] = (ev is not null && ebitda is not null && ebitda != 0)
            ? ev / ebitda : null;

        // C7. EV/Revenue
        m["c7_ev_revenue"] = (ev is not null && revenue is not null && revenue != 0)
            ? ev / revenue : null;

        // C8. EV/FCF
        var fcf = f.GetValueOrDefault("free_cash_flow");
        m["c8_ev_fcf"] = (ev is not null && fcf is not null && fcf != 0)
            ? ev / fcf : null;

        // C9. Dividend Yield
        m["c9_dividend_yield"] = (dps is not null && price is not null && price != 0)
            ? (dps / price) * 100 : null;

        // C10. Shareholder Yield = (Dividends + Buybacks - Issuance) / Market Cap
        var buybacks = f.GetValueOrDefault("share_buybacks");
        var marketCap = f.GetValueOrDefault("market_cap");
        var divPaid = f.GetValueOrDefault("total_dividends_paid");
        m["c10_shareholder_yield"] = (divPaid is not null && buybacks is not null && marketCap is not null && marketCap != 0)
            ? ((divPaid + buybacks) / marketCap) * 100 : null;

        // C11. Earnings Yield = EPS / Price (inverse of P/E)
        m["c11_earnings_yield"] = (eps is not null && price is not null && price != 0)
            ? (eps / price) * 100 : null;

        // C12. ROE
        var netIncome = f.GetValueOrDefault("net_income");
        var equity = f.GetValueOrDefault("total_equity");
        m["c12_roe"] = (netIncome is not null && equity is not null && equity != 0)
            ? (netIncome / equity) * 100 : null;

        // C13. ROA
        var totalAssets = f.GetValueOrDefault("total_assets");
        m["c13_roa"] = (netIncome is not null && totalAssets is not null && totalAssets != 0)
            ? (netIncome / totalAssets) * 100 : null;

        // C14. ROIC
        var investedCapital = f.GetValueOrDefault("invested_capital");
        var nopat = f.GetValueOrDefault("nopat") ?? (f.GetValueOrDefault("operating_income") is { } opInc
            ? opInc * 0.71m  // Approximate after-tax (29% PKR corporate tax)
            : null);
        m["c14_roic"] = (nopat is not null && investedCapital is not null && investedCapital != 0)
            ? (nopat / investedCapital) * 100 : null;

        // C15. Gross Margin
        var grossProfit = f.GetValueOrDefault("gross_profit");
        m["c15_gross_margin"] = (grossProfit is not null && revenue is not null && revenue != 0)
            ? (grossProfit / revenue) * 100 : null;

        // C16. Operating Margin
        var opIncome = f.GetValueOrDefault("operating_income");
        m["c16_operating_margin"] = (opIncome is not null && revenue is not null && revenue != 0)
            ? (opIncome / revenue) * 100 : null;

        // C17. Net Margin
        m["c17_net_margin"] = (netIncome is not null && revenue is not null && revenue != 0)
            ? (netIncome / revenue) * 100 : null;

        // C18. FCF Margin
        m["c18_fcf_margin"] = (fcf is not null && revenue is not null && revenue != 0)
            ? (fcf / revenue) * 100 : null;

        // C19. PEG Ratio (P/E / EPS Growth Rate)
        var epsGrowth = f.GetValueOrDefault("eps_growth_rate");
        var pe = m["c1_pe_ratio"];
        m["c19_peg_ratio"] = (pe is not null && epsGrowth is not null && epsGrowth != 0)
            ? pe / epsGrowth : null;

        // C20. Price vs 52-Week High
        if (ctx.PriceHistory.Count > 0)
        {
            var oneYearAgo = DateOnly.FromDateTime(DateTime.Now.AddYears(-1));
            var yearPrices = ctx.PriceHistory.Where(p => p.Date >= oneYearAgo).ToList();
            if (yearPrices.Count > 0)
            {
                var high52w = yearPrices.Max(p => p.High ?? p.Close);
                var low52w = yearPrices.Min(p => p.Low ?? p.Close);
                m["c20_price_vs_52w_high"] = (price is not null && high52w != 0)
                    ? ((price - high52w) / high52w) * 100 : null;
                m["c21_price_vs_52w_low"] = (price is not null && low52w != 0)
                    ? ((price - low52w) / low52w) * 100 : null;
            }
        }

        // C22. Market Cap
        m["c22_market_cap"] = marketCap;

        return Task.FromResult(m);
    }
}
