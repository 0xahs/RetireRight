using RetireRight.Core.DTOs;
using RetireRight.Core.Models;
using RetireRight.MetricsEngine;

namespace RetireRight.Api.Mapping;

public static class MetricsMapper
{
    public static StockMetricsDto ToDto(Dictionary<string, decimal?> m)
    {
        return new StockMetricsDto(
            DividendSafety: new DividendSafetyMetrics(
                EarningsPayoutRatio: m.GetValueOrDefault("a1_earnings_payout_ratio"),
                DividendCoverage: m.GetValueOrDefault("a4_dividend_coverage"),
                InterestCoverage: m.GetValueOrDefault("a6_interest_coverage"),
                DebtToEquity: m.GetValueOrDefault("a7_debt_to_equity"),
                DebtToEbitda: m.GetValueOrDefault("a8_debt_to_ebitda"),
                EquityRatio: m.GetValueOrDefault("a9_equity_ratio"),
                AltmanZ: m.GetValueOrDefault("a11_altman_z"),
                ConsecutiveDivYears: m.GetValueOrDefault("a12_consecutive_div_years"),
                RetainedEarningsRatio: m.GetValueOrDefault("a13_retained_earnings_ratio"),
                PayoutStability: m.GetValueOrDefault("a14_payout_stability")),

            DividendGrowth: new DividendGrowthMetrics(
                DpsYoyGrowth: m.GetValueOrDefault("b1_dps_growth_1y"),
                DpsCagr3y: m.GetValueOrDefault("b2_dps_cagr_3y"),
                DpsCagr5y: m.GetValueOrDefault("b3_dps_cagr_5y"),
                EpsGrowth1y: m.GetValueOrDefault("b7_eps_growth_1y"),
                RevenueGrowth1y: m.GetValueOrDefault("b6_revenue_growth_1y"),
                BookValueGrowth: m.GetValueOrDefault("b9_book_value_growth"),
                RoeTrend: m.GetValueOrDefault("b10_roe_trend")),

            Valuation: new ValuationMetrics(
                PeRatio: m.GetValueOrDefault("c1_pe_ratio"),
                PbRatio: m.GetValueOrDefault("c3_pb_ratio"),
                PsRatio: m.GetValueOrDefault("c4_ps_ratio"),
                DividendYield: m.GetValueOrDefault("c9_dividend_yield"),
                EarningsYield: m.GetValueOrDefault("c11_earnings_yield"),
                GrahamNumber: m.GetValueOrDefault("c23_graham_number"),
                MarginOfSafety: m.GetValueOrDefault("c24_margin_of_safety"),
                Roe: m.GetValueOrDefault("c12_roe"),
                Roa: m.GetValueOrDefault("c13_roa"),
                PeVsIndustry: m.GetValueOrDefault("c25_pe_vs_industry"),
                YieldVsIndustry: m.GetValueOrDefault("c26_yield_vs_industry"),
                PriceVs52wHigh: m.GetValueOrDefault("c20_price_vs_52w_high"),
                PriceVs52wLow: m.GetValueOrDefault("c21_price_vs_52w_low")),

            GrowthQuality: new GrowthQualityMetrics(
                SustainableGrowthRate: m.GetValueOrDefault("e17_sustainable_growth_rate"),
                Roce: m.GetValueOrDefault("e4_roce"),
                EbitdaMargin: m.GetValueOrDefault("e9_ebitda_margin"),
                EbitMargin: m.GetValueOrDefault("e16_ebit_margin"),
                PlowbackRatio: m.GetValueOrDefault("e18_plowback_ratio"),
                CapexToRevenue: m.GetValueOrDefault("e19_capex_to_revenue"),
                CapexToOcf: m.GetValueOrDefault("e20_capex_to_ocf"),
                InventoryTurnover: m.GetValueOrDefault("e12_inventory_turnover"),
                RevenuePerEmployee: m.GetValueOrDefault("e13_revenue_per_employee"),
                ProfitPerEmployee: m.GetValueOrDefault("e14_profit_per_employee"),
                DupontRoe: m.GetValueOrDefault("e15_dupont_roe"),
                TaxBurden: m.GetValueOrDefault("e16_tax_burden"),
                InterestBurden: m.GetValueOrDefault("e16_interest_burden")),

            Risk: new RiskMetrics(
                Volatility1y: m.GetValueOrDefault("f2_annual_volatility"),
                MaxDrawdown: m.GetValueOrDefault("f3_max_drawdown"),
                Beta: m.GetValueOrDefault("f4_beta"),
                Rsi14: m.GetValueOrDefault("f10_rsi_14"),
                PriceVs50dma: m.GetValueOrDefault("f8_price_vs_50dma"),
                PriceVs200dma: m.GetValueOrDefault("f9_price_vs_200dma"),
                CurrentRatio: m.GetValueOrDefault("d1_current_ratio"),
                QuickRatio: m.GetValueOrDefault("d2_quick_ratio")),

            Banking: IsBankingStock(m) ? new BankingMetrics(
                Nim: m.GetValueOrDefault("h1_nim"),
                CostToIncome: m.GetValueOrDefault("h2_cost_to_income"),
                NplRatio: m.GetValueOrDefault("h3_npl_ratio"),
                NplToEquity: m.GetValueOrDefault("h4_npl_to_equity"),
                Car: m.GetValueOrDefault("h5_car"),
                Tier1Ratio: m.GetValueOrDefault("h6_tier1_ratio"),
                Lcr: m.GetValueOrDefault("h7_lcr"),
                Adr: m.GetValueOrDefault("h8_adr"),
                LoansGrowth: m.GetValueOrDefault("h9_loans_growth"),
                FeeIncomeRatio: m.GetValueOrDefault("h10_fee_income_ratio"),
                Roa: m.GetValueOrDefault("h11_roa"),
                Roe: m.GetValueOrDefault("h12_roe"),
                LeverageRatio: m.GetValueOrDefault("h13_leverage_ratio"),
                InvestedAssetsToDeposits: m.GetValueOrDefault("h14_invested_assets_to_deposits"),
                ForexIncomePct: m.GetValueOrDefault("h15_forex_income_pct")) : null,

            Opportunity: new OpportunityMetrics(
                RangePosition52w: m.GetValueOrDefault("o1_52w_range_position"),
                DrawdownFrom52wHigh: m.GetValueOrDefault("o2_drawdown_from_52w_high"),
                QualitySignal: m.GetValueOrDefault("o3_quality_signal"),
                ValueSignal: m.GetValueOrDefault("o4_value_signal"),
                TechnicalSignal: m.GetValueOrDefault("o5_technical_signal"),
                OpportunityScore: m.GetValueOrDefault("o6_opportunity_score"),
                Flags: OpportunityScorer.DecodeFlags(m.GetValueOrDefault("o7_flags")).ToArray()),

            Scores: new ScoreBreakdown(null, null, null, null));
    }

    public static StockMetricsDto ToDtoWithScores(
        Dictionary<string, decimal?> m,
        ComputedMetrics computed)
    {
        var dto = ToDto(m);
        return dto with
        {
            Scores = new ScoreBreakdown(
                computed.CompositeScore,
                computed.DividendSafetyScore,
                computed.DividendGrowthScore,
                computed.ValuationScore)
        };
    }

    public static string ComputeSourceConfidence(DateTime? scrapedAt)
    {
        if (scrapedAt is null) return "unknown";
        var age = DateTime.UtcNow - scrapedAt.Value;
        return age.TotalHours switch
        {
            < 4 => "high",
            < 24 => "partial",
            _ => "low"
        };
    }

    private static bool IsBankingStock(Dictionary<string, decimal?> m)
        => m.ContainsKey("h1_nim") || m.ContainsKey("h5_car");
}
