namespace RetireRight.Screening.Screens;

/// <summary>
/// Pre-configured screen filters for common PSX dividend investing scenarios.
/// These match Ahmad's retirement portfolio strategy.
/// </summary>
public static class ScreenPresets
{
    public static Dictionary<string, (decimal? Min, decimal? Max)> Get(string presetName)
    {
        return presetName.ToLowerInvariant() switch
        {
            "dividend_safety" => new()
            {
                ["a1_earnings_payout_ratio"] = (null, 0.80m),     // Payout under 80%
                ["a4_dividend_coverage"] = (1.2m, null),           // Coverage > 1.2x
                ["a6_interest_coverage"] = (2.0m, null),           // Interest cover > 2x
                // a12_consecutive_div_years omitted — Sarmaaya only returns 2Y history
            },

            "high_yield" => new()
            {
                ["c9_dividend_yield"] = (5.0m, null),              // Yield > 5%
                ["a4_dividend_coverage"] = (1.0m, null),           // Not paying more than they earn
                ["d1_current_ratio"] = (0.8m, null),               // Reasonably liquid
            },

            "growth" => new()
            {
                // b3_dps_cagr_5y omitted — requires 5Y dividend history; only 2Y available from Sarmaaya
                ["b7_eps_growth_1y"] = (0, null),                  // Positive EPS growth
                ["c12_roe"] = (12, null),                          // ROE > 12%
            },

            "value" => new()
            {
                ["c1_pe_ratio"] = (0.01m, 12m),                   // P/E between 0-12
                ["c3_pb_ratio"] = (0.01m, 2.0m),                  // P/B under 2
                ["c9_dividend_yield"] = (3.0m, null),              // Yield > 3%
            },

            "retirement_core" => new()
            {
                // Ahmad's ideal: safe, growing, reasonably priced dividend payers
                ["a1_earnings_payout_ratio"] = (0.15m, 0.75m),    // 15-75% payout
                // a12_consecutive_div_years omitted — Sarmaaya only returns 2Y history
                ["c9_dividend_yield"] = (3.0m, null),              // 3%+ yield
                // b3_dps_cagr_5y omitted — requires 5Y dividend history; only 2Y available
                ["c1_pe_ratio"] = (0.01m, 20m),                   // Not wildly expensive
                ["f2_annual_volatility"] = (null, 50m),            // Not too volatile
            },

            _ => new()
        };
    }

    public static IReadOnlyList<string> AvailablePresets =>
    [
        "dividend_safety",
        "high_yield",
        "growth",
        "value",
        "retirement_core"
    ];
}
