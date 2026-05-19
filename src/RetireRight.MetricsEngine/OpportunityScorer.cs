using RetireRight.Core.Constants;

namespace RetireRight.MetricsEngine;

/// <summary>
/// Post-processor that combines quality, value, and technical signals
/// into an opportunity score (0-100). Called after all calculators finish.
///
/// Answers: "Is this a good stock that is CHEAP RIGHT NOW?"
///
/// Three pillars:
///   1. Quality (30%) — Is the company fundamentally sound? (from composite score)
///   2. Value (40%)   — Is it cheap vs intrinsic value and peers? (Graham, PE vs industry)
///   3. Technical (30%) — Is the market overselling it? (RSI, 52W position, MAs)
/// </summary>
public static class OpportunityScorer
{
    /// <summary>
    /// Computes opportunity metrics from already-computed metrics + context.
    /// Returns a dictionary of o-prefixed keys to merge into the metrics JSONB.
    /// </summary>
    public static Dictionary<string, decimal?> Score(
        Dictionary<string, decimal?> metrics,
        MetricContext context)
    {
        var o = new Dictionary<string, decimal?>();

        // ── Standalone metrics ──────────────────────────────────

        // O1. 52-Week Range Position (0 = at 52W low, 100 = at 52W high)
        // Primary: derive from price history (OHLC). Fallback: use pre-computed 52W values from fundamentals.
        var price = context.CurrentPrice;
        decimal? rangePosition = null;

        if (context.PriceHistory.Count > 0 && price is not null)
        {
            var oneYearAgo = DateOnly.FromDateTime(DateTime.Now.AddYears(-1));
            var yearPrices = context.PriceHistory.Where(p => p.Date >= oneYearAgo).ToList();
            if (yearPrices.Count > 0)
            {
                var high52w = yearPrices.Max(p => p.High ?? p.Close);
                var low52w = yearPrices.Min(p => p.Low ?? p.Close);
                if (high52w != low52w)
                {
                    rangePosition = ((price.Value - low52w) / (high52w - low52w)) * 100;
                    rangePosition = Math.Clamp(rangePosition.Value, 0, 100);
                }
            }
        }

        if (rangePosition is null && price is not null)
        {
            var low52w  = context.Fundamentals.GetValueOrDefault(FundamentalKeys.Week52Low);
            var high52w = context.Fundamentals.GetValueOrDefault(FundamentalKeys.Week52High);
            if (low52w is not null && high52w is not null && high52w != low52w)
            {
                rangePosition = ((price.Value - low52w.Value) / (high52w.Value - low52w.Value)) * 100;
                rangePosition = Math.Clamp(rangePosition.Value, 0, 100);
            }
        }

        o["o1_52w_range_position"] = rangePosition;

        // O2. Drawdown from 52W High (how far has it fallen?)
        // Primary: computed metric from price history. Fallback: w52_high_chg_pct from fundamentals (negative = below high).
        o["o2_drawdown_from_52w_high"] = metrics.GetValueOrDefault("c20_price_vs_52w_high")
            ?? context.Fundamentals.GetValueOrDefault(FundamentalKeys.Week52HighChgPct);

        // ── Component Scores ────────────────────────────────────

        var qualityScore = ScoreQuality(metrics);
        var valueScore = ScoreValue(metrics);
        var technicalScore = ScoreTechnical(metrics, rangePosition);

        o["o3_quality_signal"] = qualityScore;
        o["o4_value_signal"] = valueScore;
        o["o5_technical_signal"] = technicalScore;

        // ── Composite Opportunity Score ─────────────────────────
        // Weighted: 30% quality, 40% value, 30% technical
        var parts = new List<(decimal? score, decimal weight)>
        {
            (qualityScore, 0.30m),
            (valueScore, 0.40m),
            (technicalScore, 0.30m)
        };

        var totalWeight = 0m;
        var weightedSum = 0m;
        foreach (var (score, weight) in parts)
        {
            if (score is null) continue;
            weightedSum += score.Value * weight;
            totalWeight += weight;
        }

        o["o6_opportunity_score"] = totalWeight > 0 ? weightedSum / totalWeight : null;

        // ── Opportunity Flags (encoded as bitmask for compact storage) ──
        o["o7_flags"] = ComputeFlags(metrics, rangePosition);

        return o;
    }

    /// <summary>
    /// Returns human-readable flag names for a given bitmask.
    /// </summary>
    public static List<string> DecodeFlags(decimal? flagValue)
    {
        if (flagValue is null) return [];
        var flags = (int)flagValue.Value;
        var names = new List<string>();

        if ((flags & 1) != 0) names.Add("OVERSOLD");
        if ((flags & 2) != 0) names.Add("DEEP_VALUE");
        if ((flags & 4) != 0) names.Add("GRAHAM_DISCOUNT");
        if ((flags & 8) != 0) names.Add("HIGH_QUALITY_DIP");
        if ((flags & 16) != 0) names.Add("YIELD_ATTRACTIVE");
        if ((flags & 32) != 0) names.Add("BELOW_200DMA");
        if ((flags & 64) != 0) names.Add("SECTOR_CHEAP");
        if ((flags & 128) != 0) names.Add("OVERBOUGHT");
        if ((flags & 256) != 0) names.Add("OVERVALUED");

        return names;
    }

    // ── Quality: Is the company fundamentally sound? ────────────

    private static decimal? ScoreQuality(Dictionary<string, decimal?> m)
    {
        var score = 0m;
        var count = 0;

        // Dividend safety score (from composite calculator, 0-100)
        // We approximate from individual metrics since composite scores
        // are computed separately
        if (m.GetValueOrDefault("a4_dividend_coverage") is { } coverage)
        {
            score += coverage switch { >= 2m => 100, >= 1.5m => 80, >= 1m => 50, _ => 20 };
            count++;
        }

        // Consecutive dividend years
        if (m.GetValueOrDefault("a12_consecutive_div_years") is { } divYrs)
        {
            score += divYrs switch { >= 10 => 100, >= 7 => 80, >= 5 => 60, >= 3 => 40, _ => 20 };
            count++;
        }

        // ROE strength
        if (m.GetValueOrDefault("c12_roe") is { } roe)
        {
            score += roe switch { >= 25 => 100, >= 20 => 85, >= 15 => 70, >= 10 => 50, _ => 25 };
            count++;
        }

        // Capital structure — banking gate mirrors CompositeScoreCalculator:
        // Banks have structurally high D/E; substitute CAR as the capital quality signal.
        var isBanking = m.ContainsKey("h5_car");
        if (isBanking)
        {
            // CAR: >20% = 100, 17-20% = 80, 14-17% = 60, 12-14% = 40, <12% = 10
            if (m.GetValueOrDefault("h5_car") is { } car)
            {
                score += car switch { >= 20 => 100, >= 17 => 80, >= 14 => 60, >= 12 => 40, _ => 10 };
                count++;
            }
        }
        else
        {
            // Debt-to-equity (lower is better)
            if (m.GetValueOrDefault("a7_debt_to_equity") is { } de)
            {
                score += de switch { <= 0.3m => 100, <= 0.5m => 80, <= 1m => 60, <= 2m => 30, _ => 10 };
                count++;
            }
        }

        return count > 0 ? score / count : null;
    }

    // ── Value: Is the stock cheap vs intrinsic value and peers? ──

    private static decimal? ScoreValue(Dictionary<string, decimal?> m)
    {
        var score = 0m;
        var count = 0;

        // Graham Number margin of safety
        if (m.GetValueOrDefault("c24_margin_of_safety") is { } mos)
        {
            // mos > 0 means stock trades below Graham Number
            score += mos switch { >= 30 => 100, >= 15 => 85, >= 0 => 70, >= -15 => 40, _ => 15 };
            count++;
        }

        // PE ratio (lower is cheaper, but not negative)
        if (m.GetValueOrDefault("c1_pe_ratio") is { } pe and > 0)
        {
            score += pe switch { < 6 => 100, < 8 => 85, < 10 => 70, < 15 => 50, < 20 => 30, _ => 10 };
            count++;
        }

        // PE vs industry (negative = cheaper than sector peers)
        if (m.GetValueOrDefault("c25_pe_vs_industry") is { } peVsInd)
        {
            score += peVsInd switch { < -30 => 100, < -15 => 85, < 0 => 70, < 15 => 50, _ => 20 };
            count++;
        }

        // Dividend yield (higher is more attractive)
        if (m.GetValueOrDefault("c9_dividend_yield") is { } yield)
        {
            score += yield switch { >= 10 => 100, >= 7 => 85, >= 5 => 70, >= 3 => 50, _ => 20 };
            count++;
        }

        // Yield vs industry (positive = higher than peers)
        if (m.GetValueOrDefault("c26_yield_vs_industry") is { } yieldVsInd)
        {
            score += yieldVsInd switch { >= 30 => 100, >= 15 => 80, >= 0 => 60, _ => 30 };
            count++;
        }

        // Earnings yield (inverse of PE, > SBP rate means attractive)
        if (m.GetValueOrDefault("c11_earnings_yield") is { } earningsYield)
        {
            score += earningsYield switch { >= 15 => 100, >= 12 => 80, >= 8 => 60, >= 5 => 40, _ => 15 };
            count++;
        }

        return count > 0 ? score / count : null;
    }

    // ── Technical: Is the market overselling it? ─────────────────

    private static decimal? ScoreTechnical(Dictionary<string, decimal?> m, decimal? rangePosition)
    {
        var score = 0m;
        var count = 0;

        // RSI — below 30 = oversold (high opportunity), above 70 = overbought (low opportunity)
        if (m.GetValueOrDefault("f10_rsi_14") is { } rsi)
        {
            score += rsi switch { < 25 => 100, < 30 => 90, < 40 => 70, < 50 => 55, < 60 => 40, < 70 => 25, _ => 10 };
            count++;
        }

        // 52W range position — bottom of range = higher opportunity
        if (rangePosition is not null)
        {
            score += rangePosition.Value switch { < 15 => 100, < 25 => 85, < 40 => 70, < 60 => 50, < 80 => 30, _ => 10 };
            count++;
        }

        // Price vs 200DMA — below = potential reversion opportunity
        if (m.GetValueOrDefault("f9_price_vs_200dma") is { } vs200)
        {
            score += vs200 switch { < -20 => 100, < -10 => 85, < -5 => 70, < 0 => 55, < 10 => 35, _ => 10 };
            count++;
        }

        // Price vs 50DMA — short-term momentum
        if (m.GetValueOrDefault("f8_price_vs_50dma") is { } vs50)
        {
            score += vs50 switch { < -15 => 100, < -8 => 80, < -3 => 65, < 0 => 50, < 5 => 35, _ => 15 };
            count++;
        }

        // Max drawdown — bigger recent drawdown = more potential recovery
        if (m.GetValueOrDefault("f3_max_drawdown") is { } dd)
        {
            var absDd = Math.Abs(dd);
            score += absDd switch { >= 30 => 90, >= 20 => 75, >= 10 => 55, >= 5 => 35, _ => 20 };
            count++;
        }

        return count > 0 ? score / count : null;
    }

    // ── Flag Computation ─────────────────────────────────────────

    private static decimal ComputeFlags(Dictionary<string, decimal?> m, decimal? rangePosition)
    {
        var flags = 0;

        // OVERSOLD: RSI < 30
        if (m.GetValueOrDefault("f10_rsi_14") is < 30) flags |= 1;

        // DEEP_VALUE: PE < 6 or earnings yield > 15%
        if (m.GetValueOrDefault("c1_pe_ratio") is > 0 and < 6) flags |= 2;
        else if (m.GetValueOrDefault("c11_earnings_yield") is > 15) flags |= 2;

        // GRAHAM_DISCOUNT: Trading below Graham Number (margin of safety > 0)
        if (m.GetValueOrDefault("c24_margin_of_safety") is > 0) flags |= 4;

        // HIGH_QUALITY_DIP: Good dividend coverage + price significantly below 52W high
        if (m.GetValueOrDefault("a4_dividend_coverage") is >= 1.5m
            && m.GetValueOrDefault("c20_price_vs_52w_high") is < -15) flags |= 8;

        // YIELD_ATTRACTIVE: Yield > 7% with sustainable payout (<80%)
        if (m.GetValueOrDefault("c9_dividend_yield") is > 7
            && m.GetValueOrDefault("a1_earnings_payout_ratio") is < 0.80m) flags |= 16;

        // BELOW_200DMA: Price below 200-day moving average
        if (m.GetValueOrDefault("f9_price_vs_200dma") is < 0) flags |= 32;

        // SECTOR_CHEAP: PE more than 15% below industry average
        if (m.GetValueOrDefault("c25_pe_vs_industry") is < -15) flags |= 64;

        // OVERBOUGHT: RSI > 70 (caution signal)
        if (m.GetValueOrDefault("f10_rsi_14") is > 70) flags |= 128;

        // OVERVALUED: Trading above Graham Number by >20%
        if (m.GetValueOrDefault("c24_margin_of_safety") is < -20) flags |= 256;

        return flags;
    }
}
