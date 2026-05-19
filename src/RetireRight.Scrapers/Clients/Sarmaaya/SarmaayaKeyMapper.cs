using RetireRight.Core.Constants;

namespace RetireRight.Scrapers.Clients.Sarmaaya;

/// <summary>
/// Maps Sarmaaya API field names to standard <see cref="FundamentalKeys"/> vocabulary.
///
/// The API returns display names like "Basic EPS" which get normalized by SarmaayaApi
/// into prefixed snake_case keys: income_basic_eps, bs_total_assets, ratio_price_to_earnings.
/// This mapper translates those into our standard vocabulary.
/// </summary>
public static class SarmaayaKeyMapper
{
    // ── Fundamental Key Mapping ──────────────────────────────

    private static readonly Dictionary<string, string> KeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Income Statement (prefix: income_) ───────────────
        ["income_revenue"] = FundamentalKeys.Revenue,
        ["income_net_revenue"] = FundamentalKeys.Revenue,
        ["income_gross_profit"] = FundamentalKeys.GrossProfit,
        ["income_operating_profit"] = FundamentalKeys.OperatingIncome,
        ["income_operating_profitebit"] = FundamentalKeys.OperatingIncome, // "Operating Profit(EBIT)"
        ["income_operating_income"] = FundamentalKeys.OperatingIncome,
        ["income_profit_before_taxation"] = FundamentalKeys.PretaxIncome,
        ["income_profit_before_tax"] = FundamentalKeys.PretaxIncome,
        ["income_net_income"] = FundamentalKeys.NetIncome,
        ["income_profit_after_taxation"] = FundamentalKeys.NetIncome,
        ["income_profit_after_tax"] = FundamentalKeys.NetIncome,
        ["income_basic_eps"] = FundamentalKeys.Eps,
        ["income_earnings_per_share"] = FundamentalKeys.Eps,
        ["income_eps"] = FundamentalKeys.Eps,
        ["income_earnings_per_share_basic"] = FundamentalKeys.Eps,
        ["income_diluted_eps"] = FundamentalKeys.DilutedEps,
        ["income_dividend_per_share"] = FundamentalKeys.Dps,
        ["income_dps"] = FundamentalKeys.Dps,
        ["income_ebit"] = FundamentalKeys.Ebit,
        ["income_ebitda"] = FundamentalKeys.Ebitda,
        ["income_interest_expense"] = FundamentalKeys.InterestExpense,
        ["income_interest_income"] = FundamentalKeys.InterestIncome,
        ["income_net_interest_income"] = FundamentalKeys.NetInterestIncome,
        ["income_non_interest_income"] = FundamentalKeys.NonInterestIncome,
        ["income_non_interest_expense"] = FundamentalKeys.NonInterestExpense,
        ["income_finance_cost"] = FundamentalKeys.InterestExpense,
        ["income_depreciation_and_amortization"] = FundamentalKeys.DepreciationAmortization,
        ["income_depreciation"] = FundamentalKeys.DepreciationAmortization,
        ["income_cost_of_sales"] = FundamentalKeys.Cogs,
        ["income_cost_of_goods_sold"] = FundamentalKeys.Cogs,
        ["income_taxation"] = FundamentalKeys.TaxExpense,
        ["income_tax_rate"] = FundamentalKeys.TaxRate,
        ["income_total_dividends"] = FundamentalKeys.TotalDividendsPaid,
        ["income_preferred_dividends"] = "preferred_dividends",
        ["income_provision_for_loan_losses"] = FundamentalKeys.ProvisionForLoanLosses,
        ["income_foreign_exchange_income"] = "forex_income",

        // Cash flow (sometimes in income-statement endpoint)
        ["income_operating_cash_flow"] = FundamentalKeys.OperatingCashFlow,
        ["income_cash_from_operations"] = FundamentalKeys.OperatingCashFlow,
        ["income_capital_expenditure"] = FundamentalKeys.CapitalExpenditures,
        ["income_capex"] = FundamentalKeys.CapitalExpenditures,
        ["income_free_cash_flow"] = FundamentalKeys.FreeCashFlow,

        // ── Balance Sheet (prefix: bs_) ──────────────────────
        ["bs_total_assets"] = FundamentalKeys.TotalAssets,
        ["bs_total_equity"] = FundamentalKeys.TotalEquity,
        ["bs_shareholders_equity"] = FundamentalKeys.TotalEquity,
        ["bs_total_debt"] = FundamentalKeys.TotalDebt,
        ["bs_total_liabilities"] = FundamentalKeys.TotalLiabilities,
        ["bs_current_assets"] = FundamentalKeys.CurrentAssets,
        ["bs_total_current_assets"] = FundamentalKeys.CurrentAssets,
        ["bs_current_liabilities"] = FundamentalKeys.CurrentLiabilities,
        ["bs_long_term_debt"] = FundamentalKeys.LongTermDebt,
        ["bs_long_term_debt_excluding_lease_liabilities"] = FundamentalKeys.LongTermDebt,
        ["bs_non_current_liabilities"] = FundamentalKeys.LongTermDebt,
        ["bs_short_term_debt"] = FundamentalKeys.ShortTermDebt,
        ["bs_cash_and_due_from_banks"] = FundamentalKeys.CashAndEquivalents,
        ["bs_cash_and_bank_balances"] = FundamentalKeys.CashAndEquivalents,
        ["bs_cash_and_equivalents"] = FundamentalKeys.CashAndEquivalents,
        ["bs_cash_and_cash_equivalents"] = FundamentalKeys.CashAndEquivalents,
        ["bs_inventories"] = FundamentalKeys.Inventory,
        ["bs_inventory"] = FundamentalKeys.Inventory,
        ["bs_total_inventory"] = FundamentalKeys.Inventory,
        ["bs_stores_and_spares"] = FundamentalKeys.Inventory,
        ["bs_goodwill"] = FundamentalKeys.Goodwill,
        ["bs_intangible_assets"] = FundamentalKeys.IntangibleAssets,
        ["bs_trade_debts"] = FundamentalKeys.AccountsReceivable,
        ["bs_trade_receivables"] = FundamentalKeys.AccountsReceivable,
        ["bs_net_receivables"] = FundamentalKeys.AccountsReceivable,
        ["bs_interest_receivables"] = FundamentalKeys.AccountsReceivable,
        ["bs_trade_and_other_payables"] = FundamentalKeys.AccountsPayable,
        ["bs_trade_payables"] = FundamentalKeys.AccountsPayable,
        ["bs_accounts_payable"] = FundamentalKeys.AccountsPayable,
        ["bs_retained_earnings"] = FundamentalKeys.RetainedEarnings,
        ["bs_unappropriated_profit"] = FundamentalKeys.RetainedEarnings,
        ["bs_common_shares_outstanding"] = FundamentalKeys.SharesOutstanding,
        ["bs_shares_outstanding"] = FundamentalKeys.SharesOutstanding,
        ["bs_number_of_shares"] = FundamentalKeys.SharesOutstanding,
        ["bs_book_value_per_share"] = FundamentalKeys.BookValuePerShare,
        ["bs_market_capitalization"] = FundamentalKeys.MarketCap,
        // Banking balance sheet
        ["bs_deposits_total"] = FundamentalKeys.TotalDeposits,
        ["bs_loans_net"] = FundamentalKeys.NetLoans,
        ["bs_total_investments"] = FundamentalKeys.TotalInvestments,
        ["bs_net_ppe"] = "net_ppe",
        ["bs_minority_interest"] = "minority_interest",

        // ── Ratios (prefix: ratio_) ──────────────────────────
        // Per-share data (critical — these are raw values, not computed)
        ["ratio_earnings_per_share"] = FundamentalKeys.Eps,
        ["ratio_basic_eps"] = FundamentalKeys.Eps,
        ["ratio_book_value_per_share"] = FundamentalKeys.BookValuePerShare,
        ["ratio_common_shares_outstanding"] = FundamentalKeys.SharesOutstanding,
        ["ratio_float_shares"] = FundamentalKeys.FloatShares,
        ["ratio_cash_flow_per_share"] = FundamentalKeys.CashFlowPerShare,
        ["ratio_cash_per_share"] = FundamentalKeys.CashPerShare,
        ["ratio_dividends_per_share"] = FundamentalKeys.Dps,
        ["ratio_enterprise_value"] = FundamentalKeys.EnterpriseValue,

        // Source-provided ratios (use as fallback when we can't compute our own)
        ["ratio_price_to_earnings"] = FundamentalKeys.PeRatio,
        ["ratio_price_to_book_value"] = FundamentalKeys.PbRatio,
        ["ratio_price_to_free_cash_flow"] = FundamentalKeys.PFcfRatio,
        ["ratio_price_to_sales"] = FundamentalKeys.PsRatio,
        ["ratio_price_to_tangible_book_value"] = FundamentalKeys.PtbvRatio,
        ["ratio_enterprise_value_to_sales"] = FundamentalKeys.EvToSales,
        ["ratio_enterprise_value_to_ebitda"] = FundamentalKeys.EvToEbitda,
        ["ratio_dividend_yield_pct"] = FundamentalKeys.DividendYieldPct,
        ["ratio_dividend_payout_ratio"] = FundamentalKeys.DividendPayoutRatio,
        ["ratio_return_on_average_total_equity"] = FundamentalKeys.RoePct,
        ["ratio_return_on_common_equity"] = FundamentalKeys.RoePct,
        ["ratio_return_on_average_assets"] = FundamentalKeys.RoaPct,
        ["ratio_return_on_average_invested_capital"] = FundamentalKeys.RoicPct,
        ["ratio_return_on_average_total_capital"] = FundamentalKeys.RoicPct,
        ["ratio_debt_to_equity"] = FundamentalKeys.DebtToEquityRatio,
        ["ratio_debt_to_asset"] = FundamentalKeys.DebtToAssetsRatio,
        ["ratio_debt_to_capital"] = FundamentalKeys.DebtToCapitalRatio,
        ["ratio_equity_to_assets_pct"] = FundamentalKeys.EquityToAssetsPct,
        ["ratio_tax_rate"] = FundamentalKeys.TaxRate,
        ["ratio_total_capital_expenditures_to_sales"] = "capex_to_sales",

        // Financial health ratios
        ["ratio_current_ratio"] = FundamentalKeys.CurrentRatio,
        ["ratio_quick_ratio"] = FundamentalKeys.QuickRatio,
        ["ratio_interest_coverage"] = FundamentalKeys.InterestCoverageRatio,
        ["ratio_cash_to_debt_ratio"] = FundamentalKeys.CashToDebtRatio,

        // Margin ratios (pre-computed by source — fallback for calculators)
        ["ratio_gross_income_margin"] = FundamentalKeys.GrossMarginPct,
        ["ratio_operating_margin"] = FundamentalKeys.OperatingMarginPct,
        ["ratio_net_income_margin"] = FundamentalKeys.NetMarginPct,
        ["ratio_ebitda_margin"] = FundamentalKeys.EbitdaMarginPct,
        ["ratio_free_cash_flow_per_sales"] = FundamentalKeys.FcfMarginPct,

        // Growth ratios (YoY from source — direct growth data)
        ["ratio_eps_basic_yoy_growth"] = FundamentalKeys.EpsGrowth1yPct,
        ["ratio_net_sales_yoy_growth"] = FundamentalKeys.RevenueGrowth1yPct,
        ["ratio_equity_yoy_growth"] = FundamentalKeys.EquityGrowth1yPct,

        // Other valuation
        ["ratio_earning_yield"] = FundamentalKeys.EarningYieldPct,
        ["ratio_cash_flow_return_on_invested_capital"] = FundamentalKeys.CfroicPct,

        // Efficiency metrics
        ["ratio_asset_turnover"] = FundamentalKeys.AssetTurnover,
        ["ratio_inventory_turnover"] = FundamentalKeys.InventoryTurnover,
        ["ratio_days_sales_outstanding"] = FundamentalKeys.DaysSalesOutstanding,
        ["ratio_days_sales_in_inventory"] = FundamentalKeys.DaysSalesInInventory,
        ["ratio_days_of_payables_outstanding"] = FundamentalKeys.DaysPayablesOutstanding,
        ["ratio_cash_conversion_cycle"] = FundamentalKeys.CashConversionCycle,
        ["ratio_interest_rate_effective"] = FundamentalKeys.EffectiveInterestRate,
        ["ratio_cost_of_goods_sold_to_sales"] = FundamentalKeys.CostOfGoodsSoldToSales,
        ["ratio_graham_value"] = FundamentalKeys.GrahamValue,
        ["ratio_invested_assets_to_liabilities"] = "invested_assets_to_liabilities",
        ["ratio_sales_to_inventory_turnover"] = "sales_to_inventory_turnover",
        ["ratio_free_cash_flow_per_cfo"] = "fcf_to_cfo_ratio",

        // Banking-specific ratios
        ["ratio_non-performing_loans_to_total_loans"] = FundamentalKeys.NplRatio,
        ["ratio_non_performing_loans_to_total_loans"] = FundamentalKeys.NplRatio,
        ["ratio_tier_1_capital_ratio"] = FundamentalKeys.Tier1Ratio,
        ["ratio_common_equity_tier_1_ratio"] = FundamentalKeys.Tier1Ratio,
        ["ratio_total_capital_ratio"] = FundamentalKeys.CarRatio,
        ["ratio_leverage_ratio_-_basel_iii"] = "leverage_ratio_basel3",
        ["ratio_leverage_ratio_basel_iii"] = "leverage_ratio_basel3",
        ["ratio_total_loans_to_total_deposits"] = "loans_to_deposits",
        ["ratio_total_loans_yoy_growth"] = "loans_yoy_growth",
        ["ratio_loan_losses_provision_to_total_loans"] = "loan_loss_provision_pct",
        ["ratio_non-performing_loans_to_common_equity"] = "npl_to_equity",
        ["ratio_non_performing_loans_to_common_equity"] = "npl_to_equity",
        ["ratio_invested_assets_to_deposits"] = "invested_assets_to_deposits",
        ["ratio_liquidity_coverage_ratio_lcr"] = "lcr",
        ["ratio_number_of_branches"] = "number_of_branches",

        // Per-share from income endpoint
        ["income_market_capitalization"] = FundamentalKeys.MarketCap,
        ["income_shares_outstanding"] = FundamentalKeys.SharesOutstanding,
        ["income_number_of_shares"] = FundamentalKeys.SharesOutstanding,
    };

    /// <summary>
    /// Maps raw Sarmaaya fundamental keys to standard vocabulary.
    /// Strips ltm_ prefix. Maps ratio_*, income_*, and bs_* keys.
    /// </summary>
    public static Dictionary<string, decimal?> MapToStandard(Dictionary<string, decimal?> raw)
    {
        var result = new Dictionary<string, decimal?>();

        foreach (var (rawKey, value) in raw)
        {
            if (value is null) continue;

            // Skip industry keys — stored separately for benchmarking
            if (rawKey.StartsWith("industry_", StringComparison.OrdinalIgnoreCase))
                continue;

            // Strip ltm_ prefix for lookup
            var lookupKey = rawKey.StartsWith("ltm_", StringComparison.OrdinalIgnoreCase)
                ? rawKey[4..]
                : rawKey;

            // Handle _prev_year suffix: map the base key, then append _prev_year
            var isPrevYear = lookupKey.EndsWith("_prev_year", StringComparison.OrdinalIgnoreCase);
            var baseKey = isPrevYear ? lookupKey[..^"_prev_year".Length] : lookupKey;

            if (KeyMap.TryGetValue(baseKey, out var standardKey))
            {
                var outputKey = isPrevYear ? $"{standardKey}_prev_year" : standardKey;
                // Don't overwrite with a less precise value
                if (!result.ContainsKey(outputKey))
                    result[outputKey] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts industry average keys from raw fundamentals.
    /// </summary>
    public static Dictionary<string, decimal?> ExtractIndustryAverages(Dictionary<string, decimal?> raw)
    {
        var result = new Dictionary<string, decimal?>();

        foreach (var (rawKey, value) in raw)
        {
            if (value is null) continue;
            if (!rawKey.StartsWith("industry_", StringComparison.OrdinalIgnoreCase)) continue;

            // Map known industry averages to standard keys
            var suffix = rawKey["industry_".Length..].ToLowerInvariant();
            var standardKey = suffix switch
            {
                "pe_ratio" or "price_to_earnings" => FundamentalKeys.IndustryPe,
                "pb_ratio" or "price_to_book" => FundamentalKeys.IndustryPb,
                "dividend_yield" => FundamentalKeys.IndustryDividendYield,
                "roe" or "return_on_equity" => FundamentalKeys.IndustryRoe,
                "debt_to_equity" => FundamentalKeys.IndustryDebtEquity,
                _ => $"industry_{suffix}" // Preserve unmapped industry metrics
            };

            result[standardKey] = value;
        }

        return result;
    }

}
