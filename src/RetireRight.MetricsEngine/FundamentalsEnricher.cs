using RetireRight.Core.Constants;
using RetireRight.Core.Enums;
using RetireRight.MetricsEngine.Calculators;

namespace RetireRight.MetricsEngine;

/// <summary>
/// Pre-processing step that computes derived fundamental values before calculators run.
/// Handles banking-sector adaptations (revenue = NII + non-interest income),
/// fills gaps computable from other available data (DPS from dividends, market cap
/// from price × shares), and fixes data quality issues (LTM enterprise_value = 0).
/// </summary>
public static class FundamentalsEnricher
{
    private static readonly HashSet<Sector> BankingSectors =
        [Sector.Banking, Sector.IslamicBanking];

    /// <summary>
    /// Enriches the merged fundamentals dictionary in-place with derived values.
    /// Call this AFTER merging ANN + LTM but BEFORE passing to calculators.
    /// </summary>
    public static void Enrich(
        Dictionary<string, decimal?> fundamentals,
        IReadOnlyList<DividendPoint> dividendHistory,
        decimal? currentPrice,
        Sector sector)
    {
        var f = fundamentals;
        var isBanking = BankingSectors.Contains(sector);

        // 1. Compute DPS from dividend history when not in fundamentals
        ComputeDps(f, dividendHistory);

        // 2. Compute market cap = price × shares outstanding
        ComputeMarketCap(f, currentPrice);

        // 3. Fix enterprise value (LTM often has EV=0 which overwrites valid ANN value)
        FixEnterpriseValue(f);

        // 4. Banking-specific: compute revenue, operating income, gross profit proxy
        if (isBanking)
            ComputeBankingDerivedValues(f);

        // 5. Compute total dividends paid = DPS × shares outstanding
        ComputeTotalDividendsPaid(f);
    }

    /// <summary>
    /// Computes annual DPS from dividend history for the most recent complete fiscal year.
    /// Only sets the value if "dps" is not already in fundamentals.
    /// </summary>
    private static void ComputeDps(Dictionary<string, decimal?> f, IReadOnlyList<DividendPoint> dividendHistory)
    {
        if (f.ContainsKey(FundamentalKeys.Dps) && f[FundamentalKeys.Dps] is not null)
            return;

        if (dividendHistory.Count == 0)
            return;

        // Group by year, pick the most recent COMPLETE year
        // (current year may be incomplete — e.g., Q1 only)
        var currentYear = DateTime.UtcNow.Year;
        var yearlyDps = dividendHistory
            .GroupBy(d => d.ExDate.Year)
            .Select(g => new { Year = g.Key, TotalDps = g.Sum(d => d.Dps) })
            .OrderByDescending(g => g.Year)
            .ToList();

        // Prefer last complete year; fall back to current year if it's the only one
        var bestYear = yearlyDps.FirstOrDefault(y => y.Year < currentYear)
                       ?? yearlyDps.FirstOrDefault();

        if (bestYear is not null && bestYear.TotalDps > 0)
            f[FundamentalKeys.Dps] = bestYear.TotalDps;
    }

    /// <summary>
    /// market_cap = current price × shares outstanding (in same units).
    /// Sarmaaya reports shares in millions, so market cap will be in millions too.
    /// </summary>
    private static void ComputeMarketCap(Dictionary<string, decimal?> f, decimal? currentPrice)
    {
        if (f.ContainsKey(FundamentalKeys.MarketCap) && f[FundamentalKeys.MarketCap] is not null and not 0)
            return;

        var shares = f.GetValueOrDefault(FundamentalKeys.SharesOutstanding);
        if (currentPrice is not null and not 0 && shares is not null and not 0)
            f[FundamentalKeys.MarketCap] = currentPrice.Value * shares.Value;
    }

    /// <summary>
    /// LTM data from Sarmaaya often has enterprise_value = 0 (computed with 0 market cap).
    /// If EV is 0 or missing, recompute: EV = market_cap + total_debt - cash.
    /// </summary>
    private static void FixEnterpriseValue(Dictionary<string, decimal?> f)
    {
        var ev = f.GetValueOrDefault(FundamentalKeys.EnterpriseValue);
        if (ev is not null and not 0)
            return;

        var marketCap = f.GetValueOrDefault(FundamentalKeys.MarketCap);
        var totalDebt = f.GetValueOrDefault(FundamentalKeys.TotalDebt);
        var cash = f.GetValueOrDefault(FundamentalKeys.CashAndEquivalents);

        if (marketCap is not null)
        {
            // EV = Market Cap + Total Debt - Cash
            f[FundamentalKeys.EnterpriseValue] = marketCap.Value
                + (totalDebt ?? 0)
                - (cash ?? 0);
        }
    }

    /// <summary>
    /// For banking sector, derives standard accounting values from banking-specific fields:
    /// - Revenue = Net Interest Income + Non-Interest Income
    /// - Operating Income = Revenue - Non-Interest Expense - Provision for Loan Losses
    /// - Gross Profit = Net Interest Income (the "spread" — closest banking analog)
    /// - EBIT ≈ Pre-tax Income (for banks, interest IS the business)
    /// </summary>
    private static void ComputeBankingDerivedValues(Dictionary<string, decimal?> f)
    {
        var nii = f.GetValueOrDefault(FundamentalKeys.NetInterestIncome);
        var nonIntIncome = f.GetValueOrDefault(FundamentalKeys.NonInterestIncome);
        var nonIntExpense = f.GetValueOrDefault(FundamentalKeys.NonInterestExpense);
        var provisions = f.GetValueOrDefault(FundamentalKeys.ProvisionForLoanLosses);

        // Revenue = NII + Non-Interest Income
        if (!HasValue(f, FundamentalKeys.Revenue) && nii is not null)
        {
            var bankRevenue = nii.Value + (nonIntIncome ?? 0);
            f[FundamentalKeys.Revenue] = bankRevenue;
        }

        // Gross Profit for banks = Net Interest Income (the spread earned on loans vs deposits)
        if (!HasValue(f, FundamentalKeys.GrossProfit) && nii is not null)
            f[FundamentalKeys.GrossProfit] = nii.Value;

        // Operating Income = Revenue - Non-Interest Expense - Provisions
        if (!HasValue(f, FundamentalKeys.OperatingIncome))
        {
            var revenue = f.GetValueOrDefault(FundamentalKeys.Revenue);
            if (revenue is not null && nonIntExpense is not null)
            {
                // Provisions can be negative (write-backs) — use absolute value as expense
                var provisionExpense = provisions is not null ? Math.Abs(provisions.Value) : 0;
                f[FundamentalKeys.OperatingIncome] = revenue.Value - nonIntExpense.Value - provisionExpense;
            }
        }

        // EBIT for banks ≈ Pre-tax Income
        // (Interest is revenue, not expense, so traditional EBIT = earnings + interest + tax doesn't apply)
        if (!HasValue(f, FundamentalKeys.Ebit))
        {
            var pretax = f.GetValueOrDefault(FundamentalKeys.PretaxIncome);
            if (pretax is not null)
                f[FundamentalKeys.Ebit] = pretax.Value;
        }
    }

    /// <summary>
    /// total_dividends_paid = DPS × shares outstanding.
    /// Used by payout ratio and retained earnings calculators.
    /// </summary>
    private static void ComputeTotalDividendsPaid(Dictionary<string, decimal?> f)
    {
        if (HasValue(f, FundamentalKeys.TotalDividendsPaid))
            return;

        var dps = f.GetValueOrDefault(FundamentalKeys.Dps);
        var shares = f.GetValueOrDefault(FundamentalKeys.SharesOutstanding);

        if (dps is not null && shares is not null)
            f[FundamentalKeys.TotalDividendsPaid] = dps.Value * shares.Value;
    }

    private static bool HasValue(Dictionary<string, decimal?> f, string key)
        => f.TryGetValue(key, out var val) && val is not null;
}
