using RetireRight.Core.Models;

namespace RetireRight.Api.Mapping;

/// <summary>
/// Derives the CapyTrade-evolution signal grammar (verdict, conviction, safety tier,
/// why-line, risks) from a stock's computed metrics + freshness context.
///
/// This is presentation-layer logic — no DB writes, no metric computation. All inputs
/// come from `computed_metrics.Metrics` (JSONB) + price `UpdatedAt` + sector enum.
/// </summary>
public static class SignalGrammar
{
    public sealed record Output(
        string Verdict,
        decimal Confidence,
        string ConvictionLevel,
        string SafetyTier,
        decimal? NetYield,
        string? WhyLine,
        string? Risks,
        int Completeness,
        int Total);

    /// <summary>Filer WHT rate on PSX dividends.</summary>
    public const decimal FilerWhtRate = 0.15m;

    public static Output Derive(
        Dictionary<string, decimal?> metrics,
        ComputedMetrics? computed,
        DateTime? scrapedAt,
        string sector)
    {
        var isBanking = IsBanking(sector);
        var total = isBanking ? 92 : 77;
        var populated = metrics.Count(kv => kv.Value is not null);
        var completeness = total > 0 ? (int)Math.Round(populated * 100.0 / total) : 0;
        if (completeness > 100) completeness = 100;

        var composite = computed?.CompositeScore;
        var safety = computed?.DividendSafetyScore;
        var opportunity = metrics.GetValueOrDefault("o6_opportunity_score");
        var flags = RetireRight.MetricsEngine.OpportunityScorer
            .DecodeFlags(metrics.GetValueOrDefault("o7_flags"));

        var verdict = DeriveVerdict(metrics, composite, safety, opportunity, scrapedAt, populated);
        var confidence = DeriveConfidence(completeness, scrapedAt, isBanking, metrics);
        var conviction = DeriveConviction(confidence, composite);
        var safetyTier = DeriveSafetyTier(safety);
        var grossYield = metrics.GetValueOrDefault("c9_dividend_yield");
        var netYield = grossYield is null ? null : (decimal?)(grossYield.Value * (1m - FilerWhtRate));
        var whyLine = DeriveWhyLine(metrics, flags, verdict);
        var risks = DeriveRisks(metrics, flags);

        return new Output(
            verdict,
            Math.Round(confidence, 2),
            conviction,
            safetyTier,
            netYield is null ? null : Math.Round(netYield.Value, 2),
            whyLine,
            risks,
            completeness,
            total);
    }

    // ── Verdict ────────────────────────────────────────────────

    private static string DeriveVerdict(
        Dictionary<string, decimal?> m,
        decimal? composite,
        decimal? safety,
        decimal? opportunity,
        DateTime? scrapedAt,
        int populated)
    {
        // No data at all → WATCH (not enough to decide)
        if (composite is null || populated < 10) return "WATCH";

        // REVIEW: data quality questions trump everything else
        var ageHours = scrapedAt is null
            ? double.MaxValue
            : (DateTime.UtcNow - scrapedAt.Value).TotalHours;
        if (ageHours > 24) return "REVIEW";

        // AVOID: dividend-investor red lines
        var payout = m.GetValueOrDefault("a1_earnings_payout_ratio");
        var coverage = m.GetValueOrDefault("a4_dividend_coverage");
        if (payout is > 1.0m) return "AVOID";
        if (coverage is < 1.0m) return "AVOID";
        if (safety is < 30) return "AVOID";

        // ACCUMULATE: composite + safety + opportunity all healthy
        if (composite >= 70 && safety >= 60 && opportunity >= 60) return "ACCUMULATE";

        // WATCH vs HOLD: improving (high opp but mid composite) = WATCH; otherwise HOLD
        if (composite < 50 && opportunity >= 70) return "WATCH";

        return "HOLD";
    }

    // ── Confidence (0..1) ──────────────────────────────────────

    private static decimal DeriveConfidence(
        int completeness,
        DateTime? scrapedAt,
        bool isBanking,
        Dictionary<string, decimal?> m)
    {
        decimal score = 0m;

        // Completeness component (0..0.25)
        score += (decimal)completeness / 100m * 0.25m;

        // Freshness component (0..0.25)
        var ageHours = scrapedAt is null
            ? double.MaxValue
            : (DateTime.UtcNow - scrapedAt.Value).TotalHours;
        score += ageHours switch
        {
            < 4 => 0.25m,
            < 12 => 0.18m,
            < 24 => 0.10m,
            < 72 => 0.05m,
            _ => 0m,
        };

        // Sector correctness — bank with banking metrics (or non-bank without) (0..0.25)
        var hasBankingMetrics = m.ContainsKey("h1_nim") || m.ContainsKey("h5_car");
        if (isBanking == hasBankingMetrics) score += 0.25m;

        // Metric breadth — opportunity score present (signals full pipeline ran) (0..0.25)
        if (m.GetValueOrDefault("o6_opportunity_score") is not null
            && m.GetValueOrDefault("a4_dividend_coverage") is not null
            && m.GetValueOrDefault("c1_pe_ratio") is not null)
        {
            score += 0.25m;
        }
        else if (m.GetValueOrDefault("o6_opportunity_score") is not null)
        {
            score += 0.12m;
        }

        return Math.Clamp(score, 0m, 1m);
    }

    // ── Conviction (●●● display) ───────────────────────────────

    private static string DeriveConviction(decimal confidence, decimal? composite)
    {
        if (confidence >= 0.75m && composite >= 70m) return "HIGH";
        if (confidence >= 0.5m && composite >= 50m) return "MEDIUM";
        return "LOW";
    }

    // ── Safety Tier (letter grade) ─────────────────────────────

    private static string DeriveSafetyTier(decimal? safety) => safety switch
    {
        >= 85 => "S",
        >= 70 => "A",
        >= 55 => "B",
        >= 40 => "C",
        not null => "D",
        _ => "-",
    };

    // ── Why-line (template-driven, ≤80 chars) ──────────────────

    private static string? DeriveWhyLine(
        Dictionary<string, decimal?> m,
        List<string> flags,
        string verdict)
    {
        if (verdict is "AVOID") return null;

        var parts = new List<string>();
        var flagSet = flags.ToHashSet();

        // Pick at most 2 positive flags, in order of decision weight
        if (flagSet.Contains("DEEP_VALUE") && m.GetValueOrDefault("c1_pe_ratio") is { } pe)
            parts.Add($"cheap (PE {pe:F1})");
        else if (flagSet.Contains("GRAHAM_DISCOUNT") && m.GetValueOrDefault("c24_margin_of_safety") is { } mos)
            parts.Add($"{mos:F0}% below Graham");

        if (flagSet.Contains("HIGH_QUALITY_DIP") && m.GetValueOrDefault("a4_dividend_coverage") is { } cov)
            parts.Add($"quality dip (cov {cov:F1}x)");
        else if (flagSet.Contains("YIELD_ATTRACTIVE") && m.GetValueOrDefault("c9_dividend_yield") is { } y)
            parts.Add($"yield {y:F1}% sustainable");

        if (parts.Count == 0 && flagSet.Contains("OVERSOLD") && m.GetValueOrDefault("f10_rsi_14") is { } rsi)
            parts.Add($"oversold (RSI {rsi:F0})");

        if (parts.Count == 0 && flagSet.Contains("SECTOR_CHEAP"))
            parts.Add("cheap vs sector peers");

        // Fallback to safety-driven framing for HOLD/ACCUMULATE without flag wins
        if (parts.Count == 0)
        {
            var fallbackCov = m.GetValueOrDefault("a4_dividend_coverage");
            if (fallbackCov is >= 1.5m) parts.Add($"safe dividend (cov {fallbackCov:F1}x)");
        }

        if (parts.Count == 0) return null;
        var line = string.Join(" | ", parts);
        return line.Length > 80 ? line[..77] + "..." : line;
    }

    // ── Risks (max 80 chars) ───────────────────────────────────

    private static string? DeriveRisks(Dictionary<string, decimal?> m, List<string> flags)
    {
        var parts = new List<string>();
        var flagSet = flags.ToHashSet();

        var payout = m.GetValueOrDefault("a1_earnings_payout_ratio");
        if (payout is > 1.0m) parts.Add($"payout {payout * 100m:F0}% (>100%)");
        else if (payout is > 0.85m) parts.Add($"payout {payout * 100m:F0}% (rising)");

        var coverage = m.GetValueOrDefault("a4_dividend_coverage");
        if (coverage is < 1.0m) parts.Add($"coverage {coverage:F2}x (thin)");

        if (flagSet.Contains("OVERBOUGHT") && m.GetValueOrDefault("f10_rsi_14") is { } rsi)
            parts.Add($"overbought (RSI {rsi:F0})");

        if (flagSet.Contains("OVERVALUED")) parts.Add("trades above Graham");

        if (flagSet.Contains("BELOW_200DMA") && !flagSet.Contains("HIGH_QUALITY_DIP"))
            parts.Add("below 200DMA");

        if (parts.Count == 0) return null;
        var line = string.Join(" | ", parts.Take(2));
        return line.Length > 80 ? line[..77] + "..." : line;
    }

    // ── Helpers ────────────────────────────────────────────────

    private static bool IsBanking(string sector)
    {
        var s = sector?.ToLowerInvariant() ?? string.Empty;
        return s.Contains("banking") || s.Contains("islamic");
    }
}
