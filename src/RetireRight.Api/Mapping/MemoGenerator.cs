using RetireRight.Core.DTOs;
using RetireRight.Core.Models;

namespace RetireRight.Api.Mapping;

/// <summary>
/// Generates a per-stock memo from computed metrics using a deterministic template.
/// No LLM call. Same inputs → same memo. Every claim cites a metric key.
///
/// Output is a 5-section document (Snapshot, Thesis, Risks, Valuation, Verdict)
/// of short, scannable lines suitable for either UI rendering or Claude context.
/// </summary>
public static class MemoGenerator
{
    public static StockMemoDto Generate(
        Stock stock,
        DailyPrice? price,
        ComputedMetrics? computed)
    {
        var m = computed?.Metrics ?? new Dictionary<string, decimal?>();
        var sectorStr = stock.Sector;
        var grammar = SignalGrammar.Derive(m, computed, price?.UpdatedAt, sectorStr);
        var sourceConfidence = MetricsMapper.ComputeSourceConfidence(price?.UpdatedAt);

        var snapshot = BuildSnapshot(stock, price, computed, m, grammar);
        var thesis = BuildThesis(stock, computed, m, grammar);
        var risks = BuildRisks(m, grammar);
        var valuation = BuildValuation(m, grammar);
        var verdict = BuildVerdict(stock, computed, m, grammar);

        var envelope = BuildClaudeEnvelope(stock, price, computed, m, grammar);

        return new StockMemoDto(
            Symbol: stock.Symbol,
            Verdict: grammar.Verdict,
            GeneratedAt: DateTime.UtcNow,
            DataAsOf: price?.UpdatedAt,
            SourceConfidence: sourceConfidence,
            MetricsPopulated: m.Count(kv => kv.Value is not null),
            MetricsTotal: grammar.Total,
            Snapshot: snapshot,
            Thesis: thesis,
            Risks: risks,
            Valuation: valuation,
            VerdictSection: verdict,
            ClaudeEnvelope: envelope);
    }

    // ── SECTION BUILDERS ───────────────────────────────────────────────────────

    private static MemoSection BuildSnapshot(
        Stock stock, DailyPrice? price, ComputedMetrics? computed,
        Dictionary<string, decimal?> m, SignalGrammar.Output g)
    {
        var lines = new List<MemoLine>();

        var changeBlurb = price?.ChangePct switch
        {
            null => "no recent move",
            >= 0 => $"up {price.ChangePct:F2}% today",
            _    => $"down {Math.Abs(price.ChangePct!.Value):F2}% today",
        };

        lines.Add(Cite(
            $"{stock.Symbol} ({stock.Sector}) trades at Rs.{price?.Close:F2}, {changeBlurb}.",
            ["price.close"]));

        if (computed?.CompositeScore is { } comp)
        {
            lines.Add(Cite(
                $"Composite score is {comp:F0}/100 \u2014 verdict: {g.Verdict}.",
                ["composite_score"]));
        }

        if (g.NetYield is { } ny)
        {
            lines.Add(Cite(
                $"Dividend yield is {m.GetValueOrDefault("c9_dividend_yield"):F1}% gross / {ny:F1}% net after 15% WHT.",
                ["c9_dividend_yield"]));
        }

        var conv = g.ConvictionLevel switch
        {
            "HIGH"   => "high",
            "MEDIUM" => "medium",
            _        => "low",
        };
        lines.Add(Opinion(
            $"Conviction is {conv} ({(g.Confidence * 100):F0}% confidence) on {g.Completeness}% metric completeness."));

        return new MemoSection("Snapshot", lines.ToArray());
    }

    private static MemoSection BuildThesis(
        Stock stock, ComputedMetrics? computed,
        Dictionary<string, decimal?> m, SignalGrammar.Output g)
    {
        var lines = new List<MemoLine>();

        var cov = m.GetValueOrDefault("a4_dividend_coverage");
        if (cov is >= 1.5m)
        {
            lines.Add(Cite(
                $"Dividend coverage of {cov:F2}x signals the payout is well-supported by earnings.",
                ["a4_dividend_coverage"]));
        }
        else if (cov is >= 1.0m)
        {
            lines.Add(Cite(
                $"Dividend coverage of {cov:F2}x is adequate but not comfortable.",
                ["a4_dividend_coverage"]));
        }
        else if (cov is not null)
        {
            lines.Add(Cite(
                $"Dividend coverage of {cov:F2}x is thin \u2014 earnings are not fully funding the payout.",
                ["a4_dividend_coverage"]));
        }

        var years = m.GetValueOrDefault("a12_consecutive_div_years");
        if (years is >= 10)
            lines.Add(Cite(
                $"Track record of {years:F0}+ consecutive dividend years is the strongest reliability anchor.",
                ["a12_consecutive_div_years"]));
        else if (years is >= 5)
            lines.Add(Cite(
                $"{years:F0} consecutive dividend years suggests a maturing payout history.",
                ["a12_consecutive_div_years"]));

        var safetyScore = computed?.DividendSafetyScore;
        if (safetyScore is >= 70)
            lines.Add(Cite(
                $"Dividend safety score of {safetyScore:F0}/100 places this in tier A or better.",
                ["safety_score"]));
        else if (safetyScore is < 40)
            lines.Add(Cite(
                $"Dividend safety score of {safetyScore:F0}/100 is in the C/D tier \u2014 dependability questionable.",
                ["safety_score"]));

        // Sector-specific signal
        var nim = m.GetValueOrDefault("h1_nim");
        if (nim is >= 4m)
            lines.Add(Cite(
                $"Banking margin (NIM) of {nim:F2}% is above peer median \u2014 earnings engine intact.",
                ["h1_nim"]));

        var car = m.GetValueOrDefault("h5_car");
        if (car is >= 17m)
            lines.Add(Cite(
                $"Capital adequacy of {car:F1}% gives a wide cushion above regulatory floor.",
                ["h5_car"]));

        if (lines.Count == 0)
            lines.Add(Opinion("Insufficient signal to construct a positive thesis; rely on Risks and Valuation sections."));

        return new MemoSection("Thesis", lines.ToArray());
    }

    private static MemoSection BuildRisks(
        Dictionary<string, decimal?> m, SignalGrammar.Output g)
    {
        var lines = new List<MemoLine>();

        var payout = m.GetValueOrDefault("a1_earnings_payout_ratio");
        if (payout is > 1.0m)
            lines.Add(Cite(
                $"Payout ratio of {(payout * 100m):F0}% exceeds 100% \u2014 the dividend is not earned and is at acute cut risk.",
                ["a1_earnings_payout_ratio"]));
        else if (payout is > 0.85m)
            lines.Add(Cite(
                $"Payout ratio of {(payout * 100m):F0}% leaves little buffer for an earnings shock.",
                ["a1_earnings_payout_ratio"]));

        var cov = m.GetValueOrDefault("a4_dividend_coverage");
        if (cov is < 1.0m)
            lines.Add(Cite(
                $"Coverage of {cov:F2}x is below 1x \u2014 past earnings did not cover the past payout.",
                ["a4_dividend_coverage"]));

        var de = m.GetValueOrDefault("a7_debt_to_equity");
        if (de is > 2m)
            lines.Add(Cite(
                $"Debt-to-equity of {de:F2}x is elevated \u2014 interest burden may pressure dividends in a rate cycle.",
                ["a7_debt_to_equity"]));

        var npl = m.GetValueOrDefault("h3_npl_ratio");
        if (npl is > 6m)
            lines.Add(Cite(
                $"Non-performing loans ratio of {npl:F1}% is in stress territory.",
                ["h3_npl_ratio"]));

        var rsi = m.GetValueOrDefault("f10_rsi_14");
        if (rsi is > 70)
            lines.Add(Cite(
                $"RSI of {rsi:F0} indicates overbought conditions \u2014 entries here pay a premium.",
                ["f10_rsi_14"]));

        var mos = m.GetValueOrDefault("c24_margin_of_safety");
        if (mos is < -20)
            lines.Add(Cite(
                $"Trades {Math.Abs(mos.Value):F0}% above the Graham Number \u2014 overvalued relative to book and earnings.",
                ["c24_margin_of_safety"]));

        var stale = g.Confidence < 0.5m;
        if (stale)
            lines.Add(Opinion("Underlying data is stale or incomplete \u2014 refresh the pipeline before relying on this memo."));

        if (lines.Count == 0)
            lines.Add(Opinion("No structural red flags surfaced from current metrics."));

        return new MemoSection("Risks", lines.ToArray());
    }

    private static MemoSection BuildValuation(
        Dictionary<string, decimal?> m, SignalGrammar.Output g)
    {
        var lines = new List<MemoLine>();

        var pe = m.GetValueOrDefault("c1_pe_ratio");
        if (pe is > 0)
        {
            var verdict = pe switch
            {
                < 6m  => "very cheap",
                < 10m => "cheap",
                < 15m => "fair",
                < 20m => "premium",
                _     => "expensive",
            };
            lines.Add(Cite(
                $"P/E of {pe:F1}x reads as {verdict} on an absolute basis.",
                ["c1_pe_ratio"]));
        }

        var pb = m.GetValueOrDefault("c3_pb_ratio");
        if (pb is > 0)
        {
            var verdict = pb switch
            {
                < 1m   => "trading below book \u2014 deep value setup",
                < 1.5m => "modest premium to book",
                < 3m   => "fair multiple of book",
                _      => "expensive on book value",
            };
            lines.Add(Cite($"P/B of {pb:F2}x \u2014 {verdict}.", ["c3_pb_ratio"]));
        }

        var mos = m.GetValueOrDefault("c24_margin_of_safety");
        if (mos is >= 20)
            lines.Add(Cite(
                $"Margin of safety vs Graham Number is {mos:F0}% \u2014 substantial downside cushion.",
                ["c24_margin_of_safety"]));
        else if (mos is >= 0)
            lines.Add(Cite(
                $"Margin of safety vs Graham Number is {mos:F0}% \u2014 modest cushion.",
                ["c24_margin_of_safety"]));

        var ey = m.GetValueOrDefault("c11_earnings_yield");
        if (ey is > 0)
            lines.Add(Cite(
                $"Earnings yield is {ey:F1}% \u2014 compare to the 11.5% SBP policy rate for the opportunity-cost reading.",
                ["c11_earnings_yield"]));

        if (lines.Count == 0)
            lines.Add(Opinion("Valuation metrics unavailable \u2014 cannot anchor entry pricing."));

        return new MemoSection("Valuation", lines.ToArray());
    }

    private static MemoSection BuildVerdict(
        Stock stock, ComputedMetrics? computed,
        Dictionary<string, decimal?> m, SignalGrammar.Output g)
    {
        var lines = new List<MemoLine>();

        var headline = g.Verdict switch
        {
            "ACCUMULATE" => $"Verdict: ACCUMULATE \u2014 high-conviction setup with composite {computed?.CompositeScore:F0}/100 and {g.SafetyTier} safety tier.",
            "WATCH"      => $"Verdict: WATCH \u2014 improving signals but composite {computed?.CompositeScore:F0}/100 not yet at action threshold.",
            "HOLD"       => $"Verdict: HOLD \u2014 stable but unremarkable; do not add or trim on current data.",
            "REVIEW"     => $"Verdict: REVIEW \u2014 data quality is below threshold; refresh before deciding.",
            "AVOID"      => $"Verdict: AVOID \u2014 a dividend-investor red line was crossed (payout/coverage/safety).",
            _            => $"Verdict: {g.Verdict}.",
        };
        lines.Add(Cite(headline, ["composite_score", "safety_score"]));

        if (g.WhyLine is not null)
            lines.Add(Opinion($"Bull case (signal grammar): {g.WhyLine}."));
        if (g.Risks is not null)
            lines.Add(Opinion($"Bear case (signal grammar): {g.Risks}."));

        lines.Add(Opinion(
            "This memo is a structured reading of computed metrics. It does NOT forecast prices, recommend position sizes, or evaluate events outside the dataset."));

        return new MemoSection("Verdict", lines.ToArray());
    }

    // ── CLAUDE ENVELOPE ────────────────────────────────────────────────────────

    private static ClaudeEnvelopeDto BuildClaudeEnvelope(
        Stock stock, DailyPrice? price, ComputedMetrics? computed,
        Dictionary<string, decimal?> m, SignalGrammar.Output g)
    {
        // Tight envelope: identity + verdict grammar + the ~20 metrics that
        // drive the verdict, NOT the full 92-metric dump.
        var envelope = new
        {
            symbol = stock.Symbol,
            name = stock.Name,
            sector = stock.Sector,
            asOf = price?.UpdatedAt,
            dataConfidence = MetricsMapper.ComputeSourceConfidence(price?.UpdatedAt),
            price = price?.Close,
            changePct = price?.ChangePct,
            verdict = g.Verdict,
            convictionLevel = g.ConvictionLevel,
            confidence = g.Confidence,
            safetyTier = g.SafetyTier,
            scores = new
            {
                composite = computed?.CompositeScore,
                safety = computed?.DividendSafetyScore,
                growth = computed?.DividendGrowthScore,
                valuation = computed?.ValuationScore,
                opportunity = m.GetValueOrDefault("o6_opportunity_score"),
            },
            keyMetrics = new
            {
                dividendYield = m.GetValueOrDefault("c9_dividend_yield"),
                netYieldAfterFilerWht = g.NetYield,
                dividendCoverage = m.GetValueOrDefault("a4_dividend_coverage"),
                earningsPayoutRatio = m.GetValueOrDefault("a1_earnings_payout_ratio"),
                consecutiveDivYears = m.GetValueOrDefault("a12_consecutive_div_years"),
                peRatio = m.GetValueOrDefault("c1_pe_ratio"),
                pbRatio = m.GetValueOrDefault("c3_pb_ratio"),
                marginOfSafety = m.GetValueOrDefault("c24_margin_of_safety"),
                earningsYield = m.GetValueOrDefault("c11_earnings_yield"),
                debtToEquity = m.GetValueOrDefault("a7_debt_to_equity"),
                rsi14 = m.GetValueOrDefault("f10_rsi_14"),
                priceVs200dma = m.GetValueOrDefault("f9_price_vs_200dma"),
                drawdownFrom52wHigh = m.GetValueOrDefault("c20_price_vs_52w_high"),
                nim = m.GetValueOrDefault("h1_nim"),
                car = m.GetValueOrDefault("h5_car"),
                nplRatio = m.GetValueOrDefault("h3_npl_ratio"),
            },
            signalGrammar = new
            {
                whyLine = g.WhyLine,
                risks = g.Risks,
                opportunityFlags = MetricsEngine.OpportunityScorer.DecodeFlags(m.GetValueOrDefault("o7_flags")),
            },
            disclaimer = "Generated from RetireRight computed metrics. Verdict thresholds are deterministic \u2014 not investment advice.",
        };

        var json = System.Text.Json.JsonSerializer.Serialize(envelope,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            });

        var bytes = System.Text.Encoding.UTF8.GetByteCount(json);
        var tokens = (int)Math.Round(json.Length / 3.8);

        return new ClaudeEnvelopeDto(json, tokens, bytes);
    }

    // ── HELPERS ────────────────────────────────────────────────────────────────

    private static MemoLine Cite(string text, params string[] citations) =>
        new(text, citations, false);

    private static MemoLine Opinion(string text) =>
        new(text, Array.Empty<string>(), true);
}
