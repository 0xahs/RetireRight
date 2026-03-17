namespace RetireRight.MetricsEngine.Calculators;

/// <summary>
/// Category F: Risk Metrics (F1-F18).
/// Answers: "How volatile/risky is this stock for a retirement portfolio?"
/// </summary>
public class RiskCalculator : IMetricCalculator
{
    public string Category => "F_Risk";

    public Task<Dictionary<string, decimal?>> ComputeAsync(MetricContext ctx, CancellationToken ct = default)
    {
        var m = new Dictionary<string, decimal?>();

        if (ctx.PriceHistory.Count < 20)
            return Task.FromResult(m);

        var prices = ctx.PriceHistory.OrderBy(p => p.Date).ToList();

        // Compute daily returns
        var returns = new List<double>();
        for (var i = 1; i < prices.Count; i++)
        {
            if (prices[i - 1].Close == 0) continue;
            returns.Add((double)((prices[i].Close - prices[i - 1].Close) / prices[i - 1].Close));
        }

        if (returns.Count < 10)
            return Task.FromResult(m);

        // F1. Daily Volatility (StdDev of daily returns)
        var avgReturn = returns.Average();
        var variance = returns.Sum(r => Math.Pow(r - avgReturn, 2)) / returns.Count;
        var dailyVol = Math.Sqrt(variance);
        m["f1_daily_volatility"] = (decimal)(dailyVol * 100);

        // F2. Annualized Volatility
        m["f2_annual_volatility"] = (decimal)(dailyVol * Math.Sqrt(252) * 100);

        // F3. Maximum Drawdown
        var maxDrawdown = ComputeMaxDrawdown(prices);
        m["f3_max_drawdown"] = maxDrawdown;

        // F4. Beta (vs KSE-100) — requires index data, left null for now

        // F5. Sharpe Ratio (simplified: using SBP rate as risk-free)
        var annualReturn = avgReturn * 252;
        var riskFreeRate = 0.12; // Approximate SBP rate — will be dynamic in Phase 2
        var annualVol = dailyVol * Math.Sqrt(252);
        m["f5_sharpe_ratio"] = annualVol > 0
            ? (decimal)((annualReturn - riskFreeRate) / annualVol) : null;

        // F6. Sortino Ratio (using downside deviation only)
        var downsideReturns = returns.Where(r => r < 0).ToList();
        if (downsideReturns.Count > 0)
        {
            var downsideVar = downsideReturns.Sum(r => Math.Pow(r, 2)) / downsideReturns.Count;
            var downsideDev = Math.Sqrt(downsideVar) * Math.Sqrt(252);
            m["f6_sortino_ratio"] = downsideDev > 0
                ? (decimal)((annualReturn - riskFreeRate) / downsideDev) : null;
        }

        // F7. Average Volume (last 20 days)
        var last20 = prices.TakeLast(20).ToList();
        m["f7_avg_volume_20d"] = last20.Where(p => p.Volume.HasValue).Any()
            ? (decimal)last20.Where(p => p.Volume.HasValue).Average(p => p.Volume!.Value) : null;

        // F8. Price-to-50DMA
        if (prices.Count >= 50)
        {
            var ma50 = prices.TakeLast(50).Average(p => p.Close);
            m["f8_price_vs_50dma"] = (ctx.CurrentPrice is not null && ma50 != 0)
                ? ((ctx.CurrentPrice - ma50) / ma50) * 100 : null;
        }

        // F9. Price-to-200DMA
        if (prices.Count >= 200)
        {
            var ma200 = prices.TakeLast(200).Average(p => p.Close);
            m["f9_price_vs_200dma"] = (ctx.CurrentPrice is not null && ma200 != 0)
                ? ((ctx.CurrentPrice - ma200) / ma200) * 100 : null;
        }

        return Task.FromResult(m);
    }

    private static decimal ComputeMaxDrawdown(List<PricePoint> prices)
    {
        decimal peak = 0;
        decimal maxDd = 0;

        foreach (var p in prices)
        {
            if (p.Close > peak) peak = p.Close;
            if (peak > 0)
            {
                var dd = (peak - p.Close) / peak;
                if (dd > maxDd) maxDd = dd;
            }
        }

        return maxDd * -100; // Return as negative percentage
    }
}
