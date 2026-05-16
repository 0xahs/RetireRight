namespace RetireRight.MetricsEngine.Calculators;

/// <summary>
/// Category L: Composite Scores (L1-L3).
/// Combines individual metric categories into actionable scores.
/// These are the headline numbers Claude uses for quick stock ranking.
/// </summary>
public class CompositeScoreCalculator : IMetricCalculator
{
    public string Category => "L_Composite";

    public Task<Dictionary<string, decimal?>> ComputeAsync(MetricContext ctx, CancellationToken ct = default)
    {
        // Composite scores are computed AFTER all other calculators.
        // This calculator is a post-processor that reads already-computed metrics
        // from the MetricsResult and derives composite scores.
        // For Phase 1, we return empty — the engine will wire this up in Phase 2
        // when we add a second pass after all calculators complete.
        return Task.FromResult(new Dictionary<string, decimal?>());
    }

    /// <summary>
    /// Called by the engine after all other calculators, with the merged metrics dictionary.
    /// Returns dividend safety score (0-100), dividend growth score (0-100),
    /// valuation score (0-100), and overall composite (weighted average).
    /// </summary>
    public static (decimal? safety, decimal? growth, decimal? valuation, decimal? composite) ComputeScores(
        Dictionary<string, decimal?> metrics)
    {
        var safety = ScoreDividendSafety(metrics);
        var growth = ScoreDividendGrowth(metrics);
        var valuation = ScoreValuation(metrics);

        // Weighted: 40% safety, 30% growth, 30% valuation
        decimal? composite = null;
        var parts = new List<(decimal? score, decimal weight)>
        {
            (safety, 0.40m),
            (growth, 0.30m),
            (valuation, 0.30m)
        };

        var totalWeight = 0m;
        var weightedSum = 0m;
        foreach (var (score, weight) in parts)
        {
            if (score is null) continue;
            weightedSum += score.Value * weight;
            totalWeight += weight;
        }

        if (totalWeight > 0)
            composite = weightedSum / totalWeight;

        return (safety, growth, valuation, composite);
    }

    private static decimal? ScoreDividendSafety(Dictionary<string, decimal?> m)
    {
        var score = 0m;
        var count = 0;

        // Payout ratio: < 50% = 100, 50-70% = 70, 70-90% = 40, >90% = 10
        if (m.GetValueOrDefault("a1_earnings_payout_ratio") is { } payout)
        {
            score += payout switch { <= 0.50m => 100, <= 0.70m => 70, <= 0.90m => 40, _ => 10 };
            count++;
        }

        // Dividend coverage: >2x = 100, 1.5-2x = 70, 1-1.5x = 40, <1x = 10
        if (m.GetValueOrDefault("a4_dividend_coverage") is { } cover)
        {
            score += cover switch { >= 2m => 100, >= 1.5m => 70, >= 1m => 40, _ => 10 };
            count++;
        }

        // Consecutive dividend years: 10+ = 100, 5-9 = 70, 3-4 = 40, <3 = 20
        if (m.GetValueOrDefault("a12_consecutive_div_years") is { } yrs)
        {
            score += yrs switch { >= 10 => 100, >= 5 => 70, >= 3 => 40, _ => 20 };
            count++;
        }

        // Banking gate: if H metrics are present, use bank-appropriate safety inputs
        // instead of D/E and interest coverage (both meaningless for banks)
        var isBanking = m.ContainsKey("h5_car");

        if (isBanking)
        {
            // CAR: >20% = 100, 17-20% = 80, 14-17% = 60, 12-14% = 40, <12% = 10
            if (m.GetValueOrDefault("h5_car") is { } car)
            {
                score += car switch { >= 20 => 100, >= 17 => 80, >= 14 => 60, >= 12 => 40, _ => 10 };
                count++;
            }

            // Cost-to-Income: <40% = 100, 40-50% = 70, 50-60% = 40, >60% = 10
            if (m.GetValueOrDefault("h2_cost_to_income") is { } cir)
            {
                score += cir switch { < 40 => 100, < 50 => 70, < 60 => 40, _ => 10 };
                count++;
            }

            // NPL Ratio: <3% = 100, 3-6% = 70, 6-10% = 40, >10% = 10
            if (m.GetValueOrDefault("h3_npl_ratio") is { } npl)
            {
                score += npl switch { < 3 => 100, < 6 => 70, < 10 => 40, _ => 10 };
                count++;
            }
        }
        else
        {
            // Non-banking: interest coverage and D/E apply normally
            // Interest coverage: >5x = 100, 3-5x = 70, 1.5-3x = 40, <1.5x = 10
            if (m.GetValueOrDefault("a6_interest_coverage") is { } intCover)
            {
                score += intCover switch { >= 5m => 100, >= 3m => 70, >= 1.5m => 40, _ => 10 };
                count++;
            }

            // D/E: <0.5 = 100, 0.5-1 = 70, 1-2 = 40, >2 = 10
            if (m.GetValueOrDefault("a7_debt_to_equity") is { } de)
            {
                score += de switch { <= 0.5m => 100, <= 1m => 70, <= 2m => 40, _ => 10 };
                count++;
            }
        }

        return count > 0 ? score / count : null;
    }

    private static decimal? ScoreDividendGrowth(Dictionary<string, decimal?> m)
    {
        var score = 0m;
        var count = 0;

        // 5Y CAGR: >10% = 100, 5-10% = 70, 0-5% = 40, negative = 10
        if (m.GetValueOrDefault("b3_dps_cagr_5y") is { } cagr5)
        {
            score += cagr5 switch { >= 10 => 100, >= 5 => 70, >= 0 => 40, _ => 10 };
            count++;
        }

        // Consecutive growth years: 5+ = 100, 3-4 = 70, 1-2 = 40, 0 = 10
        if (m.GetValueOrDefault("a13_consecutive_growth_years") is { } growthYrs)
        {
            score += growthYrs switch { >= 5 => 100, >= 3 => 70, >= 1 => 40, _ => 10 };
            count++;
        }

        // Chowder number: >15 = 100, 12-15 = 70, 8-12 = 40, <8 = 10
        if (m.GetValueOrDefault("b12_chowder_number") is { } chowder)
        {
            score += chowder switch { >= 15 => 100, >= 12 => 70, >= 8 => 40, _ => 10 };
            count++;
        }

        return count > 0 ? score / count : null;
    }

    private static decimal? ScoreValuation(Dictionary<string, decimal?> m)
    {
        var score = 0m;
        var count = 0;

        // P/E: <8 = 100, 8-12 = 80, 12-20 = 50, >20 = 20
        if (m.GetValueOrDefault("c1_pe_ratio") is { } pe and > 0)
        {
            score += pe switch { < 8 => 100, <= 12 => 80, <= 20 => 50, _ => 20 };
            count++;
        }

        // Dividend yield: >7% = 100, 5-7% = 80, 3-5% = 50, <3% = 20
        if (m.GetValueOrDefault("c9_dividend_yield") is { } yield)
        {
            score += yield switch { >= 7 => 100, >= 5 => 80, >= 3 => 50, _ => 20 };
            count++;
        }

        // P/B: <1 = 100, 1-2 = 70, 2-3 = 40, >3 = 20
        if (m.GetValueOrDefault("c3_pb_ratio") is { } pb and > 0)
        {
            score += pb switch { < 1 => 100, <= 2 => 70, <= 3 => 40, _ => 20 };
            count++;
        }

        // ROE: >20% = 100, 15-20% = 70, 10-15% = 50, <10% = 20
        if (m.GetValueOrDefault("c12_roe") is { } roe)
        {
            score += roe switch { >= 20 => 100, >= 15 => 70, >= 10 => 50, _ => 20 };
            count++;
        }

        return count > 0 ? score / count : null;
    }
}
