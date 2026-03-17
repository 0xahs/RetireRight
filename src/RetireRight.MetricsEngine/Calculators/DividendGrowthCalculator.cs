namespace RetireRight.MetricsEngine.Calculators;

/// <summary>
/// Category B: Dividend Growth Analysis (B1-B12).
/// Answers: "Is this company growing its dividend over time?"
/// </summary>
public class DividendGrowthCalculator : IMetricCalculator
{
    public string Category => "B_DividendGrowth";

    public Task<Dictionary<string, decimal?>> ComputeAsync(MetricContext ctx, CancellationToken ct = default)
    {
        var m = new Dictionary<string, decimal?>();

        var yearlyDps = ctx.DividendHistory
            .GroupBy(d => d.ExDate.Year)
            .OrderByDescending(g => g.Key)
            .Select(g => new YearlyDps(g.Key, g.Sum(d => d.Dps)))
            .ToList();

        // B1. 1-Year DPS Growth Rate
        m["b1_dps_growth_1y"] = ComputeGrowthRate(yearlyDps, 1);

        // B2. 3-Year DPS CAGR
        m["b2_dps_cagr_3y"] = ComputeCagr(yearlyDps, 3);

        // B3. 5-Year DPS CAGR
        m["b3_dps_cagr_5y"] = ComputeCagr(yearlyDps, 5);

        // B4. 10-Year DPS CAGR
        m["b4_dps_cagr_10y"] = ComputeCagr(yearlyDps, 10);

        // B5. Payout Growth Stability (standard deviation of annual growth rates)
        m["b5_payout_growth_stability"] = ComputeGrowthStability(yearlyDps);

        // B6. Revenue Growth (1Y)
        var f = ctx.Fundamentals;
        var rev = f.GetValueOrDefault("revenue");
        var revPrev = f.GetValueOrDefault("revenue_prev_year");
        m["b6_revenue_growth_1y"] = (rev is not null && revPrev is not null && revPrev != 0)
            ? ((rev - revPrev) / Math.Abs(revPrev.Value)) * 100 : null;

        // B7. EPS Growth (1Y)
        var eps = f.GetValueOrDefault("eps");
        var epsPrev = f.GetValueOrDefault("eps_prev_year");
        m["b7_eps_growth_1y"] = (eps is not null && epsPrev is not null && epsPrev != 0)
            ? ((eps - epsPrev) / Math.Abs(epsPrev.Value)) * 100 : null;

        // B8. FCF Growth (1Y)
        var fcf = f.GetValueOrDefault("free_cash_flow");
        var fcfPrev = f.GetValueOrDefault("fcf_prev_year");
        m["b8_fcf_growth_1y"] = (fcf is not null && fcfPrev is not null && fcfPrev != 0)
            ? ((fcf - fcfPrev) / Math.Abs(fcfPrev.Value)) * 100 : null;

        // B9. Book Value Growth (1Y)
        var bvps = f.GetValueOrDefault("book_value_per_share");
        var bvpsPrev = f.GetValueOrDefault("bvps_prev_year");
        m["b9_bvps_growth_1y"] = (bvps is not null && bvpsPrev is not null && bvpsPrev != 0)
            ? ((bvps - bvpsPrev) / Math.Abs(bvpsPrev.Value)) * 100 : null;

        // B10. Dividend Frequency (times per year)
        m["b10_dividend_frequency"] = yearlyDps.Count > 0
            ? ctx.DividendHistory.Count(d => d.ExDate.Year == yearlyDps[0].Year)
            : null;

        // B11. Special Dividend Flag (any non-regular dividends in last year)
        m["b11_has_special_dividend"] = ctx.DividendHistory
            .Any(d => d.DividendType?.Contains("Special", StringComparison.OrdinalIgnoreCase) == true)
            ? 1 : 0;

        // B12. Chowder Rule (Yield + 5yr CAGR >= 12 for mature, >= 15 for low-yield)
        var yield = (ctx.Fundamentals.GetValueOrDefault("dps") is { } dps && ctx.CurrentPrice is > 0)
            ? (dps / ctx.CurrentPrice.Value) * 100 : (decimal?)null;
        var cagr5 = m["b3_dps_cagr_5y"];
        m["b12_chowder_number"] = (yield is not null && cagr5 is not null)
            ? yield + cagr5 : null;

        return Task.FromResult(m);
    }

    private static decimal? ComputeGrowthRate(List<YearlyDps> yearlyDps, int yearsBack)
    {
        if (yearlyDps.Count <= yearsBack) return null;
        decimal current = yearlyDps[0].TotalDps;
        decimal previous = yearlyDps[yearsBack].TotalDps;
        if (previous == 0) return null;
        return ((current - previous) / Math.Abs(previous)) * 100;
    }

    private static decimal? ComputeCagr(List<YearlyDps> yearlyDps, int years)
    {
        if (yearlyDps.Count <= years) return null;
        decimal current = yearlyDps[0].TotalDps;
        decimal past = yearlyDps[years].TotalDps;
        if (past <= 0 || current <= 0) return null;
        var ratio = (double)(current / past);
        var cagr = Math.Pow(ratio, 1.0 / years) - 1;
        return (decimal)(cagr * 100);
    }

    private static decimal? ComputeGrowthStability(List<YearlyDps> yearlyDps)
    {
        if (yearlyDps.Count < 3) return null;

        var growthRates = new List<double>();
        for (var i = 0; i < yearlyDps.Count - 1; i++)
        {
            var current = yearlyDps[i].TotalDps;
            var previous = yearlyDps[i + 1].TotalDps;
            if (previous == 0) continue;
            growthRates.Add((double)((current - previous) / Math.Abs(previous)));
        }

        if (growthRates.Count < 2) return null;

        var avg = growthRates.Average();
        var variance = growthRates.Sum(r => Math.Pow(r - avg, 2)) / growthRates.Count;
        return (decimal)(Math.Sqrt(variance) * 100);
    }
}

internal record YearlyDps(int Year, decimal TotalDps);
