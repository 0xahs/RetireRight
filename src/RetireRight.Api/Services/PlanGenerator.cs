using RetireRight.Api.Mapping;
using RetireRight.Core.DTOs;
using RetireRight.Core.Models;

namespace RetireRight.Api.Services;

/// <summary>
/// Deterministic monthly investment planner.
///
/// Inputs: a monthly budget, the latest computed signal-grammar output for every
/// watchlist stock, and the current portfolio composition.
///
/// Algorithm in plain English:
///   1. Filter to actionable verdicts (ACCUMULATE first, then WATCH, then
///      HIGH/MEDIUM-conviction HOLD). REVIEW and AVOID are never picked.
///   2. Score each candidate: composite + opportunity, adjusted up for
///      upcoming ex-dates and down for sector concentration / stale data.
///   3. Take the top 3 picks (cap for diversification).
///   4. Normalise weights with a 20-60% clamp so no single pick dominates.
///   5. Convert weights to whole-share quantities at current price.
/// </summary>
public static class PlanGenerator
{
    private const decimal ConcentrationThreshold = 0.30m; // 30%

    /// <summary>
    /// Pick count scales with budget — bigger budget, more diversification.
    /// Small budgets focus on highest-conviction names; large budgets spread.
    /// </summary>
    private static int MaxPicksForBudget(decimal budget) => budget switch
    {
        <= 8_000m  => 2,
        <= 15_000m => 3,
        <= 30_000m => 4,
        _          => 5,
    };

    /// <summary>
    /// Weight clamps scale with pick count so the renormalised total stays
    /// achievable. Without scaling, 5 picks under a 20-60% bound can't
    /// produce a valid distribution.
    /// </summary>
    private static (decimal min, decimal max) WeightBoundsForCount(int n) => n switch
    {
        <= 2 => (0.30m, 0.70m),
        3    => (0.20m, 0.60m),
        4    => (0.15m, 0.50m),
        _    => (0.12m, 0.40m),
    };

    public sealed record Candidate(
        Stock Stock,
        DailyPrice? Price,
        ComputedMetrics? Computed,
        SignalGrammar.Output Grammar,
        int? ExDateInDays,
        decimal SectorWeightInPortfolio);

    public static MonthlyPlanDto Generate(decimal budget, IEnumerable<Candidate> candidates)
    {
        var notes = new List<string>();
        var eligible = candidates
            .Where(c => c.Grammar.Verdict is "ACCUMULATE" or "WATCH" or "HOLD")
            .Where(c => c.Grammar.Verdict is not "HOLD" || c.Grammar.ConvictionLevel is "HIGH" or "MEDIUM")
            .Where(c => c.Price?.Close > 0)
            .ToList();

        if (eligible.Count == 0)
        {
            notes.Add("No actionable picks in the current watchlist \u2014 every stock is HOLD low-conviction, REVIEW, or AVOID.");
            return new MonthlyPlanDto(
                Budget: budget,
                GeneratedAt: DateTime.UtcNow,
                Picks: Array.Empty<PlanPickDto>(),
                Summary: new PlanSummaryDto(0m, budget, 0, 0m, null),
                Notes: notes.ToArray());
        }

        var maxPicks = MaxPicksForBudget(budget);
        var scored = eligible.Select(c => new { Candidate = c, Score = ScoreCandidate(c) }).ToList();
        var ranked = scored.OrderByDescending(s => s.Score).Take(maxPicks).ToList();

        if (ranked.Count < maxPicks)
            notes.Add($"Budget would support {maxPicks} picks but only {ranked.Count} watchlist stocks are actionable today — increase coverage to diversify further.");

        // Normalise weights with bounds scaled to pick count
        var totalScore = ranked.Sum(r => r.Score);
        var rawWeights = ranked.Select(r => totalScore > 0 ? r.Score / totalScore : 1m / ranked.Count).ToList();
        var (minW, maxW) = WeightBoundsForCount(ranked.Count);
        var clampedWeights = ClampAndRenormalize(rawWeights, minW, maxW);

        var picks = new List<PlanPickDto>();
        decimal totalAllocated = 0m;
        decimal expectedIncome = 0m;
        var yieldSamples = new List<decimal>();

        for (var i = 0; i < ranked.Count; i++)
        {
            var c = ranked[i].Candidate;
            var weight = clampedWeights[i];
            var target = Math.Round(budget * weight, 2);
            var price = c.Price!.Close;
            var shares = price > 0 ? (int)Math.Floor(target / price) : 0;
            var allocated = shares * price;
            totalAllocated += allocated;

            var dps = c.Computed?.Metrics.GetValueOrDefault("dps");
            var expected = dps.HasValue ? shares * dps.Value : (decimal?)null;
            if (expected.HasValue) expectedIncome += expected.Value;

            if (c.Grammar.NetYield is { } ny) yieldSamples.Add(ny);

            picks.Add(new PlanPickDto(
                Symbol: c.Stock.Symbol,
                Name: c.Stock.Name,
                Sector: c.Stock.Sector.ToString(),
                Verdict: c.Grammar.Verdict,
                ConvictionLevel: c.Grammar.ConvictionLevel,
                CompositeScore: c.Computed?.CompositeScore,
                OpportunityScore: c.Computed?.Metrics.GetValueOrDefault("o6_opportunity_score"),
                CurrentPrice: price,
                Shares: shares,
                AllocatedAmount: allocated,
                TargetAmount: target,
                Weight: Math.Round(weight, 3),
                ExpectedAnnualIncome: expected,
                NetYield: c.Grammar.NetYield,
                ExDateInDays: c.ExDateInDays,
                Reasons: BuildReasons(c)));
        }

        var leftover = budget - totalAllocated;
        if (leftover > 0)
            notes.Add($"Rs.{leftover:F0} leftover after whole-share rounding \u2014 too little for another lot.");

        // Concentration heads-up
        var concentratedPick = picks.FirstOrDefault(p =>
            ranked.First(r => r.Candidate.Stock.Symbol == p.Symbol).Candidate.SectorWeightInPortfolio
                + p.Weight > 0.4m);
        if (concentratedPick is not null)
            notes.Add($"Adding {concentratedPick.Symbol} pushes {concentratedPick.Sector} above 40% of portfolio \u2014 consider diversifying.");

        var summary = new PlanSummaryDto(
            TotalAllocated: totalAllocated,
            Leftover: leftover,
            PickCount: picks.Count,
            ExpectedAnnualIncome: expectedIncome,
            AverageNetYield: yieldSamples.Count > 0 ? Math.Round(yieldSamples.Average(), 2) : null);

        return new MonthlyPlanDto(
            Budget: budget,
            GeneratedAt: DateTime.UtcNow,
            Picks: picks.ToArray(),
            Summary: summary,
            Notes: notes.ToArray());
    }

    // ── SCORING ────────────────────────────────────────────────────────────────

    private static decimal ScoreCandidate(Candidate c)
    {
        var composite = c.Computed?.CompositeScore ?? 0m;
        var opportunity = c.Computed?.Metrics.GetValueOrDefault("o6_opportunity_score") ?? 0m;
        var baseScore = composite * 0.5m + opportunity * 0.5m;

        // Verdict tier multiplier
        var verdictMul = c.Grammar.Verdict switch
        {
            "ACCUMULATE" => 1.3m,
            "WATCH"      => 1.05m,
            "HOLD"       => 0.85m,
            _            => 0.5m,
        };

        // Conviction multiplier
        var convMul = c.Grammar.ConvictionLevel switch
        {
            "HIGH"   => 1.15m,
            "MEDIUM" => 1.0m,
            _        => 0.8m,
        };

        // Sector concentration penalty
        var concMul = c.SectorWeightInPortfolio switch
        {
            >= 0.40m => 0.55m,
            >= ConcentrationThreshold => 0.75m,
            _ => 1.0m,
        };

        // Ex-date bonus (3-14 days) \u2014 gets a small kicker to time the buy
        var exMul = c.ExDateInDays switch
        {
            > 0 and <= 14 => 1.10m,
            > 14 and <= 30 => 1.04m,
            _              => 1.0m,
        };

        // Confidence multiplier \u2014 penalize stale or thin data
        var confMul = c.Grammar.Confidence switch
        {
            >= 0.75m => 1.0m,
            >= 0.5m => 0.85m,
            _ => 0.65m,
        };

        return baseScore * verdictMul * convMul * concMul * exMul * confMul;
    }

    // ── WEIGHT NORMALISATION ───────────────────────────────────────────────────

    private static List<decimal> ClampAndRenormalize(IReadOnlyList<decimal> raw, decimal minW, decimal maxW)
    {
        if (raw.Count == 0) return new List<decimal>();
        if (raw.Count == 1) return new List<decimal> { 1m };

        var clamped = raw.Select(w => Math.Clamp(w, minW, maxW)).ToList();
        var sum = clamped.Sum();
        if (sum <= 0) return raw.Select(_ => 1m / raw.Count).ToList();
        // Re-normalise so weights sum to 1 again
        return clamped.Select(w => w / sum).ToList();
    }

    // ── RATIONALE LINES (max 3) ────────────────────────────────────────────────

    private static string[] BuildReasons(Candidate c)
    {
        var reasons = new List<string>();
        var m = c.Computed?.Metrics ?? new Dictionary<string, decimal?>();
        var flags = MetricsEngine.OpportunityScorer.DecodeFlags(m.GetValueOrDefault("o7_flags"));

        if (c.Grammar.Verdict is "ACCUMULATE")
            reasons.Add($"verdict ACCUMULATE \u00B7 composite {c.Computed?.CompositeScore:F0}");
        else if (c.Grammar.Verdict is "WATCH")
            reasons.Add($"climbing setup \u00B7 opportunity {m.GetValueOrDefault("o6_opportunity_score"):F0}");
        else
            reasons.Add($"stable HOLD \u00B7 conviction {c.Grammar.ConvictionLevel}");

        if (c.Grammar.NetYield is { } ny && ny >= 5m)
            reasons.Add($"yield {ny:F1}% net adds steady income");
        else if (flags.Contains("DEEP_VALUE") && m.GetValueOrDefault("c1_pe_ratio") is { } pe)
            reasons.Add($"cheap (PE {pe:F1})");
        else if (flags.Contains("GRAHAM_DISCOUNT") && m.GetValueOrDefault("c24_margin_of_safety") is { } mos)
            reasons.Add($"{mos:F0}% below Graham");

        if (c.ExDateInDays is > 0 and <= 14)
            reasons.Add($"ex-date in {c.ExDateInDays}d");
        else if (c.SectorWeightInPortfolio < 0.1m)
            reasons.Add("underweight sector in portfolio");

        return reasons.Take(3).ToArray();
    }
}
