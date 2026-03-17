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

    // ── Per-Share ────────────────────────────────────────────
    public const string BookValuePerShare = "book_value_per_share";
    public const string SharesOutstanding = "shares_outstanding";
    public const string FcfPerShare = "fcf_per_share";
    public const string ForwardEps = "forward_eps";

    // ── Market / Derived ─────────────────────────────────────
    public const string MarketCap = "market_cap";
    public const string EnterpriseValue = "enterprise_value";
    public const string InvestedCapital = "invested_capital";
    public const string Nopat = "nopat";
    public const string ShareBuybacks = "share_buybacks";

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
