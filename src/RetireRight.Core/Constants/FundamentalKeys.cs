namespace RetireRight.Core.Constants;

/// <summary>
/// Standard vocabulary for fundamental financial data keys.
/// All scrapers map TO these keys. All calculators read FROM these keys.
/// Source-specific key mappers (e.g., SarmaayaKeyMapper) translate raw API
/// field names into these constants.
/// </summary>
public static class FundamentalKeys
{
    // ── Income Statement ─────────────────────────────────────
    public const string Revenue = "revenue";
    public const string GrossProfit = "gross_profit";
    public const string OperatingIncome = "operating_income";
    public const string Ebit = "ebit";
    public const string Ebitda = "ebitda";
    public const string NetIncome = "net_income";
    public const string Eps = "eps";
    public const string Dps = "dps";
    public const string InterestExpense = "interest_expense";
    public const string TotalDividendsPaid = "total_dividends_paid";
    public const string DepreciationAmortization = "depreciation_amortization";
    public const string TaxExpense = "tax_expense";
    public const string PretaxIncome = "pretax_income";
    public const string Cogs = "cost_of_goods_sold";

    // ── Cash Flow ────────────────────────────────────────────
    public const string OperatingCashFlow = "operating_cash_flow";
    public const string CapitalExpenditures = "capital_expenditures";
    public const string FreeCashFlow = "free_cash_flow";

    // ── Balance Sheet ────────────────────────────────────────
    public const string TotalAssets = "total_assets";
    public const string TotalEquity = "total_equity";
    public const string TotalDebt = "total_debt";
    public const string TotalLiabilities = "total_liabilities";
    public const string CurrentAssets = "current_assets";
    public const string CurrentLiabilities = "current_liabilities";
    public const string LongTermDebt = "long_term_debt";
    public const string CashAndEquivalents = "cash_and_equivalents";
    public const string Inventory = "inventory";
    public const string Goodwill = "goodwill";
    public const string IntangibleAssets = "intangible_assets";
    public const string AccountsReceivable = "accounts_receivable";
    public const string AccountsPayable = "accounts_payable";
    public const string RetainedEarnings = "retained_earnings";

    // ── Balance Sheet (additional) ─────────────────────────
    public const string ShortTermDebt = "short_term_debt";

    // ── Per-Share ────────────────────────────────────────────
    public const string BookValuePerShare = "book_value_per_share";
    public const string SharesOutstanding = "shares_outstanding";
    public const string FloatShares = "float_shares";
    public const string FcfPerShare = "fcf_per_share";
    public const string CashFlowPerShare = "cash_flow_per_share";
    public const string CashPerShare = "cash_per_share";
    public const string ForwardEps = "forward_eps";
    public const string DilutedEps = "diluted_eps";

    // ── Market / Derived ─────────────────────────────────────
    public const string MarketCap = "market_cap";
    public const string EnterpriseValue = "enterprise_value";
    public const string InvestedCapital = "invested_capital";
    public const string Nopat = "nopat";
    public const string ShareBuybacks = "share_buybacks";

    // ── Source-Provided Ratios (pre-computed by data source) ─
    public const string PeRatio = "pe_ratio";
    public const string PbRatio = "pb_ratio";
    public const string PFcfRatio = "p_fcf_ratio";
    public const string PsRatio = "ps_ratio";
    public const string PtbvRatio = "ptbv_ratio";
    public const string EvToSales = "ev_to_sales";
    public const string EvToEbitda = "ev_to_ebitda";
    public const string DividendYieldPct = "dividend_yield_pct";
    public const string DividendPayoutRatio = "dividend_payout_ratio";
    public const string RoePct = "roe_pct";
    public const string RoaPct = "roa_pct";
    public const string RoicPct = "roic_pct";
    public const string TaxRate = "tax_rate";
    public const string DebtToEquityRatio = "debt_to_equity_ratio";
    public const string DebtToAssetsRatio = "debt_to_assets_ratio";
    public const string DebtToCapitalRatio = "debt_to_capital_ratio";
    public const string EquityToAssetsPct = "equity_to_assets_pct";
    public const string CurrentRatio = "current_ratio";
    public const string QuickRatio = "quick_ratio";
    public const string InterestCoverageRatio = "interest_coverage_ratio";
    public const string CashToDebtRatio = "cash_to_debt_ratio";

    // ── Source-Provided Margins (pre-computed) ─────────────
    public const string GrossMarginPct = "gross_margin_pct";
    public const string OperatingMarginPct = "operating_margin_pct";
    public const string NetMarginPct = "net_margin_pct";
    public const string EbitdaMarginPct = "ebitda_margin_pct";
    public const string FcfMarginPct = "fcf_margin_pct";

    // ── Source-Provided Growth Rates ───────────────────────
    public const string EpsGrowth1yPct = "eps_growth_1y_pct";
    public const string RevenueGrowth1yPct = "revenue_growth_1y_pct";
    public const string EquityGrowth1yPct = "equity_growth_1y_pct";

    // ── Other Source-Provided ──────────────────────────────
    public const string EarningYieldPct = "earning_yield_pct";
    public const string CfroicPct = "cfroic_pct";

    // ── Efficiency Ratios ────────────────────────────────
    public const string AssetTurnover = "asset_turnover";
    public const string InventoryTurnover = "inventory_turnover";
    public const string DaysSalesOutstanding = "days_sales_outstanding";
    public const string DaysSalesInInventory = "days_sales_in_inventory";
    public const string DaysPayablesOutstanding = "days_payables_outstanding";
    public const string CashConversionCycle = "cash_conversion_cycle";
    public const string EffectiveInterestRate = "effective_interest_rate";
    public const string GrahamValue = "graham_value";
    public const string CostOfGoodsSoldToSales = "cogs_to_sales";

    // ── Banking-Specific ────────────────────────────────────
    public const string InterestIncome = "interest_income";
    public const string NetInterestIncome = "net_interest_income";
    public const string NonInterestIncome = "non_interest_income";
    public const string NonInterestExpense = "non_interest_expense";
    public const string ProvisionForLoanLosses = "provision_for_loan_losses";
    public const string TotalDeposits = "total_deposits";
    public const string NetLoans = "net_loans";
    public const string TotalInvestments = "total_investments";
    public const string NplRatio = "npl_ratio";
    public const string Tier1Ratio = "tier1_ratio";
    public const string CarRatio = "car_ratio";

    // ── Prior Year (for growth calculations) ─────────────────
    public const string RevenuePrevYear = "revenue_prev_year";
    public const string EpsPrevYear = "eps_prev_year";
    public const string FcfPrevYear = "fcf_prev_year";
    public const string BvpsPrevYear = "bvps_prev_year";
    public const string EpsGrowthRate = "eps_growth_rate";

    // ── Industry Averages (for peer comparison) ──────────────
    public const string IndustryPe = "industry_pe";
    public const string IndustryPb = "industry_pb";
    public const string IndustryDividendYield = "industry_dividend_yield";
    public const string IndustryRoe = "industry_roe";
    public const string IndustryDebtEquity = "industry_debt_equity";
}
