using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RetireRight.Core.Attributes;
using RetireRight.Core.Configuration;
using RetireRight.Core.Constants;
using RetireRight.Core.Enums;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Scrapers.AntiBot;
using RetireRight.Scrapers.Base;

namespace RetireRight.Scrapers.Sources;

/// <summary>
/// Scrapes the StockAnalysis PSX exchange listing page to populate the full stock universe.
/// Visits 4 tabs in a single Playwright session (Overview, Performance, Price, Profile),
/// merges all data by symbol, then upserts stocks + stores fundamentals.
/// Ignores ScrapeRequest.Symbols — always scrapes the full universe.
/// </summary>
// Order=10: runs FIRST — seeds the Stock universe so per-stock scrapers have rows to update.
[DataSource("PsxUniverse", PerSymbol = false, Order = 10)]
public class PsxUniverseScraper : BaseScraper
{
    private const string ListUrl = "https://stockanalysis.com/list/pakistan-stock-exchange/";
    private const string FundamentalsSource = "StockAnalysisList";
    private const string FundamentalsPeriodicity = "TTM";

    private readonly IStockRepository _stockRepo;
    private readonly IFundamentalsRepository _fundamentalsRepo;
    private readonly IPriceRepository _priceRepo;
    private Guid? _currentScrapeRunId;

    // Symbols whose Dividends tab shows a future ex-date. Surfaced via GetFollowUp() so
    // the orchestrator enqueues a per-stock StockAnalysisDividends pass for them, filling
    // the calendar with correct per-event DPS without the user having to add each to watchlist.
    private readonly List<string> _pendingDividendSymbols = [];

    public override ScrapingMethod Method => ScrapingMethod.Browser;

    public PsxUniverseScraper(
        ILogger<PsxUniverseScraper> logger,
        RequestThrottler throttler,
        RetryPolicy retryPolicy,
        DataSourceOptions sourceConfig,
        IStockRepository stockRepo,
        IFundamentalsRepository fundamentalsRepo,
        IPriceRepository priceRepo)
        : base(logger, throttler, retryPolicy, sourceConfig)
    {
        _stockRepo = stockRepo;
        _fundamentalsRepo = fundamentalsRepo;
        _priceRepo = priceRepo;
    }

    protected override async Task<(int scraped, int failed, List<string> errors)> ExecuteScrapeAsync(
        ScrapeRequest request, CancellationToken ct)
    {
        _currentScrapeRunId = request.ScrapeRunId;
        var errors = new List<string>();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = ["--disable-blink-features=AutomationControlled"]
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = UserAgentRotator.GetRandom(),
            ViewportSize = new ViewportSize { Width = 1600, Height = 900 }
        });

        var page = await context.NewPageAsync();

        try
        {
            Logger.LogInformation("[PsxUniverse] Navigating to listing page");
            await page.GotoAsync(ListUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30_000
            });

            // Wait until table has rows (React renders asynchronously)
            await page.WaitForFunctionAsync(
                "() => document.querySelectorAll('table tbody tr').length > 50",
                null, new PageWaitForFunctionOptions { Timeout = 20_000 });

            // ── Enable all free indicators ────────────────────────
            await EnableAllFreeIndicatorsAsync(page, ct);

            // ── Scrape each tab ───────────────────────────────────
            var overview     = await ScrapeTabAsync(page, "Overview",     ct);
            var performance  = await ScrapeTabAsync(page, "Performance",  ct);
            var price        = await ScrapeTabAsync(page, "Price",        ct);
            var profile      = await ScrapeTabAsync(page, "Profile",      ct);
            var dividends    = await ScrapeTabAsync(page, "Dividends",    ct);

            Logger.LogInformation("[PsxUniverse] Rows — Overview:{O} Perf:{P} Price:{Pr} Profile:{Pf} Div:{D}",
                overview.Count, performance.Count, price.Count, profile.Count, dividends.Count);

            // ── Map headers to column indices for each tab ────────
            var overviewIdx    = MapHeaders(overview);
            var performanceIdx = MapHeaders(performance);
            var priceIdx       = MapHeaders(price);
            var profileIdx     = MapHeaders(profile);
            var dividendsIdx   = MapHeaders(dividends);

            // First row is headers, data starts at index 1
            var overviewRows    = overview.Skip(1).ToList();
            var performanceRows = performance.Skip(1).ToList();
            var priceRows       = price.Skip(1).ToList();
            var profileRows     = profile.Skip(1).ToList();
            var dividendsRows   = dividends.Skip(1).ToList();

            // Index by symbol for O(1) merge
            var perfBySymbol    = IndexBySymbol(performanceRows, performanceIdx);
            var priceBySymbol   = IndexBySymbol(priceRows,       priceIdx);
            var profileBySymbol = IndexBySymbol(profileRows,     profileIdx);
            var divBySymbol     = IndexBySymbol(dividendsRows,   dividendsIdx);

            var scraped = 0;
            var failed  = 0;
            var today   = DateOnly.FromDateTime(DateTime.UtcNow);

            foreach (var row in overviewRows)
            {
                ct.ThrowIfCancellationRequested();

                var symbol = Cell(row, overviewIdx, "Symbol");
                if (string.IsNullOrWhiteSpace(symbol)) continue;
                symbol = symbol.Trim().ToUpperInvariant();

                try
                {
                    var name     = Cell(row, overviewIdx, "Company Name");
                    var industry = Cell(row, overviewIdx, "Industry");
                    var sector   = Cell(row, overviewIdx, "Sector");

                    // Use Profile name/industry if richer
                    if (profileBySymbol.TryGetValue(symbol, out var profRow))
                    {
                        name     = Cell(profRow, profileIdx, "Company Name") ?? name;
                        industry = Cell(profRow, profileIdx, "Industry")     ?? industry;
                    }

                    // Prefer industry (more specific) over sector; both are StockAnalysis strings
                    var sectorStr = !string.IsNullOrWhiteSpace(industry) ? industry
                                  : !string.IsNullOrWhiteSpace(sector)   ? sector
                                  : "Other";

                    // ── Upsert stock ──────────────────────────────────
                    var existing = await _stockRepo.GetBySymbolAsync(symbol, ct);
                    if (existing is null)
                    {
                        await _stockRepo.UpsertAsync(new Stock
                        {
                            Symbol    = symbol,
                            Name      = name ?? symbol,
                            Sector    = sectorStr,
                            Industry  = industry,
                            IsActive  = true,
                            IsWatchlist = false,
                        }, ct);
                    }
                    else
                    {
                        // Update name/sector/industry — PsxUniverse is the sector authority
                        existing.Name     = name ?? existing.Name;
                        existing.Sector   = sectorStr;
                        existing.Industry = industry ?? existing.Industry;
                        await _stockRepo.UpsertAsync(existing, ct);
                    }

                    // ── Parse current price from Overview tab ─────────
                    var priceRaw     = Cell(row, overviewIdx, "Price")
                                    ?? Cell(row, overviewIdx, "Stock Price");
                    var changePctRaw = Cell(row, overviewIdx, "Change")
                                    ?? Cell(row, overviewIdx, "% Change")
                                    ?? Cell(row, overviewIdx, "Chg %");
                    var currentPrice  = ParseNum(priceRaw);
                    var currentChgPct = ParsePct(changePctRaw);

                    // ── Build fundamentals dict ───────────────────────
                    var data = new Dictionary<string, decimal?>();

                    // Overview
                    TryAddPkrM(data, FundamentalKeys.MarketCap,      Cell(row, overviewIdx, "Market Cap"));
                    TryAddPkrM(data, FundamentalKeys.Revenue,         Cell(row, overviewIdx, "Revenue"));
                    TryAddPkrM(data, FundamentalKeys.NetIncome,       Cell(row, overviewIdx, "Net Income"));
                    TryAddPkrM(data, FundamentalKeys.FreeCashFlow,    Cell(row, overviewIdx, "FCF"));
                    TryAddPkrM(data, FundamentalKeys.NetCashDebt,     Cell(row, overviewIdx, "Net Cash"));
                    TryAddPct (data, FundamentalKeys.RevenueGrowth1yPct, Cell(row, overviewIdx, "Rev. Growth"));

                    // Performance
                    if (perfBySymbol.TryGetValue(symbol, out var perfRow))
                    {
                        TryAddPct(data, FundamentalKeys.TotalReturn1M,   Cell(perfRow, performanceIdx, "Return 1M"));
                        TryAddPct(data, FundamentalKeys.TotalReturn6M,   Cell(perfRow, performanceIdx, "Return 6M"));
                        TryAddPct(data, FundamentalKeys.TotalReturnYtd,  Cell(perfRow, performanceIdx, "Return YTD"));
                        TryAddPct(data, FundamentalKeys.TotalReturn1Y,   Cell(perfRow, performanceIdx, "Return 1Y"));
                        TryAddPct(data, FundamentalKeys.TotalReturn5Y,   Cell(perfRow, performanceIdx, "Return 5Y"));
                        TryAddPct(data, FundamentalKeys.TotalReturn10Y,  Cell(perfRow, performanceIdx, "Return 10Y"));
                    }

                    // Price
                    if (priceBySymbol.TryGetValue(symbol, out var prRow))
                    {
                        TryAddNum(data, FundamentalKeys.Week52Low,        Cell(prRow, priceIdx, "52W Low"));
                        TryAddNum(data, FundamentalKeys.Week52High,       Cell(prRow, priceIdx, "52W High"));
                        TryAddPct(data, FundamentalKeys.Week52LowChgPct,  Cell(prRow, priceIdx, "52W Low Chg"));
                        TryAddPct(data, FundamentalKeys.Week52HighChgPct, Cell(prRow, priceIdx, "52W High Chg"));
                    }

                    // Profile
                    if (profileBySymbol.TryGetValue(symbol, out var prfRow))
                    {
                        TryAddNum(data, FundamentalKeys.Employees,   Cell(prfRow, profileIdx, "Employees"));
                        TryAddNum(data, FundamentalKeys.FoundedYear, Cell(prfRow, profileIdx, "Founded"));
                    }

                    // Dividends tab — fundamentals enrichment only. We deliberately do NOT
                    // write the dividends table here: the universe "Div. ($)" column is the
                    // trailing-12mo annual total, not the per-event DPS (verified live for
                    // AABS: universe shows 50.00, actual upcoming is 7.5 PKR). Writing it
                    // poisoned the calendar with wrong per-event amounts. StockAnalysisDividends
                    // (per-stock) is now the sole writer of dividend rows.
                    if (divBySymbol.TryGetValue(symbol, out var divRow))
                    {
                        TryAddNum (data, FundamentalKeys.Dps,                  Cell(divRow, dividendsIdx, "Div. ($)"));
                        TryAddPct (data, FundamentalKeys.DividendYieldPct,     Cell(divRow, dividendsIdx, "Div. Yield"));
                        TryAddPct (data, FundamentalKeys.DividendPayoutRatio,  Cell(divRow, dividendsIdx, "Payout Ratio"));
                        TryAddNum (data, FundamentalKeys.DividendPaymentYears, Cell(divRow, dividendsIdx, "Div. Payment Years")
                                                                            ?? Cell(divRow, dividendsIdx, "Payment Years"));
                        TryAddNum (data, FundamentalKeys.DividendGrowthYears,  Cell(divRow, dividendsIdx, "Div. Growth Years")
                                                                            ?? Cell(divRow, dividendsIdx, "Growth Years"));

                        var freqRaw = Cell(divRow, dividendsIdx, "Payout Freq.")
                                   ?? Cell(divRow, dividendsIdx, "Payout Frequency");
                        var ppy = PayoutsPerYear(freqRaw);
                        if (ppy.HasValue) data[FundamentalKeys.PayoutsPerYear] = ppy;

                        // Record any stock with a future-dated ex-date — orchestrator will
                        // enqueue a per-stock StockAnalysisDividends pass to fill correct DPS.
                        var futureEx = ParseUsDate(Cell(divRow, dividendsIdx, "Ex-Div Date"));
                        if (futureEx is { } fx && fx > today)
                            _pendingDividendSymbols.Add(symbol);
                    }

                    var stock = await _stockRepo.GetBySymbolAsync(symbol, ct);
                    if (stock is not null)
                    {
                        if (data.Count > 0)
                        {
                            await _fundamentalsRepo.UpsertAsync(new StockFundamental
                            {
                                StockId     = stock.Id,
                                ScrapedDate = today,
                                Periodicity = FundamentalsPeriodicity,
                                Source      = FundamentalsSource,
                                Data        = data,
                                ScrapeRunId = _currentScrapeRunId
                            }, ct);
                        }

                        if (currentPrice is not null)
                        {
                            await _priceRepo.UpsertAsync(new DailyPrice
                            {
                                StockId     = stock.Id,
                                Date        = today,
                                Close       = currentPrice.Value,
                                ChangePct   = currentChgPct,
                                Source      = FundamentalsSource,
                                ScrapeRunId = _currentScrapeRunId
                            }, ct);
                        }

                    }

                    scraped++;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "[PsxUniverse] Failed to process {Symbol}", symbol);
                    errors.Add($"{symbol}: {ex.Message}");
                    failed++;
                }
            }

            Logger.LogInformation("[PsxUniverse] Done — {Scraped} upserted, {Failed} failed",
                scraped, failed);
            return (scraped, failed, errors);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ── Browser helpers ───────────────────────────────────────────────────────

    private async Task EnableAllFreeIndicatorsAsync(IPage page, CancellationToken ct)
    {
        // Open the Indicators dropdown
        await page.EvaluateAsync("""
            () => {
                const btn = Array.from(document.querySelectorAll('button'))
                    .find(b => b.textContent?.includes('Indicators'));
                btn?.click();
            }
        """);

        await Task.Delay(600, ct);

        // Tick all unchecked free checkboxes
        var ticked = await page.EvaluateAsync<int>("""
            () => {
                const cbs = Array.from(document.querySelectorAll('input[type="checkbox"]'));
                const unchecked = cbs.filter(cb => !cb.checked);
                unchecked.forEach(cb => cb.click());
                return unchecked.length;
            }
        """);

        if (ticked > 0)
            await Task.Delay(400, ct);

        // Close dropdown by clicking heading
        await page.EvaluateAsync("() => document.querySelector('h1')?.click()");
        await Task.Delay(300, ct);

        Logger.LogDebug("[PsxUniverse] Enabled {Count} additional indicator(s)", ticked);
    }

    private async Task<List<string[]>> ScrapeTabAsync(IPage page, string tabName, CancellationToken ct)
    {
        // Switch to tab — pass tabName as JS arg to avoid raw-string interpolation conflicts
        await page.EvaluateAsync("""
            (name) => {
                const btn = Array.from(document.querySelectorAll('button'))
                    .find(b => b.textContent?.trim() === name);
                btn?.click();
            }
        """, tabName);

        await Task.Delay(500, ct);

        // Wait for table to render
        await page.WaitForFunctionAsync(
            "() => document.querySelectorAll('table tbody tr').length > 50",
            null, new PageWaitForFunctionOptions { Timeout = 15_000 });

        // Also enable indicators for this tab
        await EnableAllFreeIndicatorsAsync(page, ct);

        // Extract headers + all rows
        var raw = await page.EvaluateAsync<string[][]>("""
            () => {
                const headers = Array.from(document.querySelectorAll('table thead th'))
                    .map(th => th.textContent?.trim() ?? '');
                const rows = Array.from(document.querySelectorAll('table tbody tr')).map(tr =>
                    Array.from(tr.querySelectorAll('td')).map(td => td.textContent?.trim() ?? '')
                );
                return [headers, ...rows];
            }
        """);

        Logger.LogDebug("[PsxUniverse] Tab '{Tab}': {Rows} rows, {Cols} columns",
            tabName, raw.Length - 1, raw.Length > 0 ? raw[0].Length : 0);

        return raw.Select(r => r).ToList();
    }

    // ── Data mapping helpers ──────────────────────────────────────────────────

    private static Dictionary<string, int> MapHeaders(List<string[]> rows)
    {
        if (rows.Count == 0) return [];
        var headers = rows[0];
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
        {
            var h = headers[i].Trim();
            if (!string.IsNullOrEmpty(h) && !map.ContainsKey(h))
                map[h] = i;
        }
        return map;
    }

    private static Dictionary<string, string[]> IndexBySymbol(
        List<string[]> rows, Dictionary<string, int> headerIdx)
    {
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (!headerIdx.TryGetValue("Symbol", out var symIdx)) return result;
        foreach (var row in rows)
        {
            if (row.Length <= symIdx) continue;
            var sym = row[symIdx].Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(sym))
                result[sym] = row;
        }
        return result;
    }

    private static string? Cell(string[] row, Dictionary<string, int> idx, string col)
    {
        if (!idx.TryGetValue(col, out var i) || i >= row.Length) return null;
        var v = row[i].Trim();
        return v is "" or "-" or "N/A" ? null : v;
    }

    // ── Number parsers ────────────────────────────────────────────────────────

    /// <summary>Parse "390.40B", "1.33T", "-12.99B" → value in PKR millions.</summary>
    private static void TryAddPkrM(Dictionary<string, decimal?> data, string key, string? raw)
    {
        var v = ParsePkrMillions(raw);
        if (v.HasValue) data[key] = v;
    }

    private static decimal? ParsePkrMillions(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw is "-" or "N/A") return null;
        var s = raw.Trim();
        var multiplier = 1m;
        if (s.EndsWith("T", StringComparison.OrdinalIgnoreCase)) { multiplier = 1_000_000m; s = s[..^1]; }
        else if (s.EndsWith("B", StringComparison.OrdinalIgnoreCase)) { multiplier = 1_000m;   s = s[..^1]; }
        else if (s.EndsWith("M", StringComparison.OrdinalIgnoreCase)) { multiplier = 1m;        s = s[..^1]; }
        else if (s.EndsWith("K", StringComparison.OrdinalIgnoreCase)) { multiplier = 0.001m;    s = s[..^1]; }
        s = s.Replace(",", "");
        return decimal.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? Math.Round(v * multiplier, 4)
            : null;
    }

    /// <summary>Parse "-3.54%" → -3.54 (stored as percentage, not fraction).</summary>
    private static void TryAddPct(Dictionary<string, decimal?> data, string key, string? raw)
    {
        var v = ParsePct(raw);
        if (v.HasValue) data[key] = v;
    }

    private static decimal? ParsePct(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw is "-" or "N/A") return null;
        var s = raw.Trim().TrimEnd('%').Replace(",", "");
        return decimal.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? Math.Round(v, 4)
            : null;
    }

    /// <summary>Parse plain numbers like "9,968", "310.37", "1961".</summary>
    private static void TryAddNum(Dictionary<string, decimal?> data, string key, string? raw)
    {
        var v = ParseNum(raw);
        if (v.HasValue) data[key] = v;
    }

    private static decimal? ParseNum(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw is "-" or "N/A") return null;
        var s = raw.Trim().Replace(",", "");
        return decimal.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? v : null;
    }

    /// <summary>Payout-frequency text → payments per year. Powers expected-next-ex-date forecasts.</summary>
    private static decimal? PayoutsPerYear(string? raw) => (raw?.Trim().ToLowerInvariant()) switch
    {
        "monthly"     => 12m,
        "quarterly"   => 4m,
        "semi-annual" => 2m,
        "semiannual"  => 2m,
        "annual"      => 1m,
        "yearly"      => 1m,
        _             => null,
    };

    /// <summary>StockAnalysis renders ex-dates as US-style "May 26, 2026".</summary>
    private static DateOnly? ParseUsDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw is "-" or "N/A") return null;
        return DateOnly.TryParseExact(
                raw.Trim(),
                ["MMM d, yyyy", "MMMM d, yyyy", "MMM dd, yyyy"],
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var d)
            ? d : null;
    }

    /// <summary>Surface the universe's discoveries — orchestrator enqueues a per-stock
    /// StockAnalysisDividends pass for any symbol whose Dividends tab shows a future
    /// ex-date. Dedupe + cap at 50 to bound the follow-up scrape's runtime.</summary>
    protected override (string? source, string[] symbols) GetFollowUp()
    {
        if (_pendingDividendSymbols.Count == 0) return (null, []);
        var dedup = _pendingDividendSymbols.Distinct(StringComparer.OrdinalIgnoreCase).Take(50).ToArray();
        return ("StockAnalysisDividends", dedup);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
