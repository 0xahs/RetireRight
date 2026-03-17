using RetireRight.Core.Constants;
using RetireRight.Core.Enums;

namespace RetireRight.Scrapers.Clients.Sarmaaya;

/// <summary>
/// Maps Sarmaaya API field names to standard <see cref="FundamentalKeys"/> vocabulary
/// and Sarmaaya sector strings to <see cref="Sector"/> enum values.
/// </summary>
public static class SarmaayaKeyMapper
{
    // ── Fundamental Key Mapping ──────────────────────────────

    private static readonly Dictionary<string, string> KeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Income statement
        ["income_revenue"] = FundamentalKeys.Revenue,
        ["income_net_revenue"] = FundamentalKeys.Revenue,
        ["income_gross_profit"] = FundamentalKeys.GrossProfit,
        ["income_operating_profit"] = FundamentalKeys.OperatingIncome,
        ["income_operating_income"] = FundamentalKeys.OperatingIncome,
        ["income_profit_before_taxation"] = FundamentalKeys.PretaxIncome,
        ["income_profit_before_tax"] = FundamentalKeys.PretaxIncome,
        ["income_net_income"] = FundamentalKeys.NetIncome,
        ["income_profit_after_taxation"] = FundamentalKeys.NetIncome,
        ["income_profit_after_tax"] = FundamentalKeys.NetIncome,
        ["income_earnings_per_share"] = FundamentalKeys.Eps,
        ["income_eps"] = FundamentalKeys.Eps,
        ["income_earnings_per_share_basic"] = FundamentalKeys.Eps,
        ["income_dividend_per_share"] = FundamentalKeys.Dps,
        ["income_dps"] = FundamentalKeys.Dps,
        ["income_ebit"] = FundamentalKeys.Ebit,
        ["income_ebitda"] = FundamentalKeys.Ebitda,
        ["income_interest_expense"] = FundamentalKeys.InterestExpense,
        ["income_finance_cost"] = FundamentalKeys.InterestExpense,
        ["income_depreciation"] = FundamentalKeys.DepreciationAmortization,
        ["income_depreciation_and_amortization"] = FundamentalKeys.DepreciationAmortization,
        ["income_cost_of_sales"] = FundamentalKeys.Cogs,
        ["income_cost_of_goods_sold"] = FundamentalKeys.Cogs,
        ["income_taxation"] = FundamentalKeys.TaxExpense,
        ["income_total_dividends"] = FundamentalKeys.TotalDividendsPaid,

        // Cash flow
        ["income_operating_cash_flow"] = FundamentalKeys.OperatingCashFlow,
        ["income_cash_from_operations"] = FundamentalKeys.OperatingCashFlow,
        ["income_capital_expenditure"] = FundamentalKeys.CapitalExpenditures,
        ["income_capex"] = FundamentalKeys.CapitalExpenditures,
        ["income_free_cash_flow"] = FundamentalKeys.FreeCashFlow,

        // Balance sheet
        ["bs_total_assets"] = FundamentalKeys.TotalAssets,
        ["bs_total_equity"] = FundamentalKeys.TotalEquity,
        ["bs_shareholders_equity"] = FundamentalKeys.TotalEquity,
        ["bs_total_debt"] = FundamentalKeys.TotalDebt,
        ["bs_total_liabilities"] = FundamentalKeys.TotalLiabilities,
        ["bs_current_assets"] = FundamentalKeys.CurrentAssets,
        ["bs_current_liabilities"] = FundamentalKeys.CurrentLiabilities,
        ["bs_long_term_debt"] = FundamentalKeys.LongTermDebt,
        ["bs_non_current_liabilities"] = FundamentalKeys.LongTermDebt,
        ["bs_cash_and_bank_balances"] = FundamentalKeys.CashAndEquivalents,
        ["bs_cash_and_equivalents"] = FundamentalKeys.CashAndEquivalents,
        ["bs_cash_and_cash_equivalents"] = FundamentalKeys.CashAndEquivalents,
        ["bs_inventories"] = FundamentalKeys.Inventory,
        ["bs_inventory"] = FundamentalKeys.Inventory,
        ["bs_stores_and_spares"] = FundamentalKeys.Inventory,
        ["bs_goodwill"] = FundamentalKeys.Goodwill,
        ["bs_intangible_assets"] = FundamentalKeys.IntangibleAssets,
        ["bs_trade_debts"] = FundamentalKeys.AccountsReceivable,
        ["bs_trade_receivables"] = FundamentalKeys.AccountsReceivable,
        ["bs_trade_and_other_payables"] = FundamentalKeys.AccountsPayable,
        ["bs_trade_payables"] = FundamentalKeys.AccountsPayable,
        ["bs_retained_earnings"] = FundamentalKeys.RetainedEarnings,
        ["bs_unappropriated_profit"] = FundamentalKeys.RetainedEarnings,

        // Per-share
        ["bs_book_value_per_share"] = FundamentalKeys.BookValuePerShare,
        ["income_book_value_per_share"] = FundamentalKeys.BookValuePerShare,
        ["bs_shares_outstanding"] = FundamentalKeys.SharesOutstanding,
        ["income_shares_outstanding"] = FundamentalKeys.SharesOutstanding,
        ["income_number_of_shares"] = FundamentalKeys.SharesOutstanding,

        // Market
        ["income_market_capitalization"] = FundamentalKeys.MarketCap,
        ["bs_market_capitalization"] = FundamentalKeys.MarketCap,
    };

    /// <summary>
    /// Maps raw Sarmaaya fundamental keys to standard vocabulary.
    /// Strips ltm_ prefix, skips ratio_* and industry_* keys (computed/stored separately).
    /// </summary>
    public static Dictionary<string, decimal?> MapToStandard(Dictionary<string, decimal?> raw)
    {
        var result = new Dictionary<string, decimal?>();

        foreach (var (rawKey, value) in raw)
        {
            if (value is null) continue;

            // Skip ratio keys — MetricsEngine computes its own ratios
            if (rawKey.StartsWith("ratio_", StringComparison.OrdinalIgnoreCase) ||
                rawKey.StartsWith("ltm_ratio_", StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip industry keys — stored separately for benchmarking
            if (rawKey.StartsWith("industry_", StringComparison.OrdinalIgnoreCase))
                continue;

            // Strip ltm_ prefix for lookup
            var lookupKey = rawKey.StartsWith("ltm_", StringComparison.OrdinalIgnoreCase)
                ? rawKey[4..]
                : rawKey;

            if (KeyMap.TryGetValue(lookupKey, out var standardKey))
            {
                // Don't overwrite with a less precise value
                if (!result.ContainsKey(standardKey))
                    result[standardKey] = value;
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

    // ── Sector Mapping ───────────────────────────────────────

    private static readonly Dictionary<string, Sector> SectorMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Commercial Banks"] = Sector.Banking,
        ["Banks"] = Sector.Banking,
        ["Islamic Banking"] = Sector.IslamicBanking,
        ["Oil & Gas Exploration Companies"] = Sector.OilAndGas,
        ["Oil & Gas Marketing Companies"] = Sector.OilAndGas,
        ["Exploration & Production"] = Sector.OilAndGas,
        ["Refinery"] = Sector.OilAndGas,
        ["Power Generation & Distribution"] = Sector.Power,
        ["Power Generation"] = Sector.Power,
        ["Independent Power Producers"] = Sector.Power,
        ["Fertilizer"] = Sector.Fertilizer,
        ["Cement"] = Sector.Cement,
        ["Technology & Communication"] = Sector.Technology,
        ["Technology"] = Sector.Technology,
        ["Automobile Assembler"] = Sector.Automobile,
        ["Automobile Parts & Accessories"] = Sector.Automobile,
        ["Pharmaceuticals"] = Sector.Pharmaceutical,
        ["Pharmaceutical"] = Sector.Pharmaceutical,
        ["Textile Composite"] = Sector.Textile,
        ["Textile Spinning"] = Sector.Textile,
        ["Textile Weaving"] = Sector.Textile,
        ["Sugar Allied Industries"] = Sector.Food,
        ["Food & Personal Care Products"] = Sector.Food,
        ["Chemical"] = Sector.Chemical,
        ["Chemicals"] = Sector.Chemical,
        ["Insurance"] = Sector.Insurance,
        ["Real Estate Investment Trust"] = Sector.RealEstate,
        ["Telecommunication"] = Sector.Telecom,
    };

    public static Sector MapSector(string? sectorName)
    {
        if (sectorName is null) return Sector.Other;
        return SectorMap.GetValueOrDefault(sectorName, Sector.Other);
    }
}
