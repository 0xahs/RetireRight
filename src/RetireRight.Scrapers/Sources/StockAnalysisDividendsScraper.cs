using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RetireRight.Core.Attributes;
using RetireRight.Core.Configuration;
using RetireRight.Core.Enums;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Scrapers.AntiBot;
using RetireRight.Scrapers.Base;

namespace RetireRight.Scrapers.Sources;

/// <summary>
/// Per-stock dividend history from StockAnalysis.com (/quote/psx/{SYMBOL}/dividend/).
///
/// The page has a single table with Ex-Dividend Date · Cash Amount · Record Date · Pay Date.
/// It includes forward-announced rows when present, so this scraper does two jobs at once:
///   1. Fills in the correct DPS / PaymentDate / RecordDate for upcoming dividends that
///      the universe sweep (PsxUniverseScraper) only knows the ex-date for.
///      (The universe "Div. ($)" column shows last-paid amount, not the upcoming announcement —
///      see the merge upsert in DividendRepository for how multi-source rows reconcile.)
///   2. Captures multi-year dividend history (5–21+ rows) for ANY of the 481 PSX stocks,
///      not just the 8 PsxTerminal currently covers.
///
/// Cash amounts on this page are local currency despite the column being labeled "Cash Amount"
/// — for PSX they render as "9.000 PKR". We parse the leading number; no unit conversion needed.
/// </summary>
[DataSource("StockAnalysisDividends", PerSymbol = true)]
public class StockAnalysisDividendsScraper : BaseScraper
{
    private const string BaseUrl = "https://stockanalysis.com";

    private readonly IStockRepository _stockRepo;
    private readonly IDividendRepository _dividendRepo;
    private Guid? _currentScrapeRunId;

    public override ScrapingMethod Method => ScrapingMethod.Browser;

    public StockAnalysisDividendsScraper(
        ILogger<StockAnalysisDividendsScraper> logger,
        RequestThrottler throttler,
        RetryPolicy retryPolicy,
        DataSourceOptions sourceConfig,
        IStockRepository stockRepo,
        IDividendRepository dividendRepo)
        : base(logger, throttler, retryPolicy, sourceConfig)
    {
        _stockRepo = stockRepo;
        _dividendRepo = dividendRepo;
    }

    protected override async Task<(int scraped, int failed, List<string> errors)> ExecuteScrapeAsync(
        ScrapeRequest request, CancellationToken ct)
    {
        var scraped = 0;
        var failed = 0;
        var errors = new List<string>();
        _currentScrapeRunId = request.ScrapeRunId;

        var symbols = request.Symbols.Length > 0
            ? request.Symbols
            : (await _stockRepo.GetWatchlistAsync(ct)).Select(s => s.Symbol).ToArray();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = ["--disable-blink-features=AutomationControlled"]
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = UserAgentRotator.GetRandom(),
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 }
        });

        foreach (var symbol in symbols)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await ThrottleAsync(ct);

                var rows = await FetchHistoryAsync(context, symbol, ct);
                if (rows is null)
                {
                    // No table / page missing — not a hard failure; many stocks don't pay dividends.
                    Logger.LogDebug("[StockAnalysisDividends] {Symbol}: no dividend history", symbol);
                    continue;
                }

                var stock = await _stockRepo.GetBySymbolAsync(symbol, ct);
                if (stock is null)
                {
                    errors.Add($"{symbol}: not in DB — run universe scrape first");
                    failed++;
                    continue;
                }

                var upserted = 0;
                foreach (var row in rows)
                {
                    await _dividendRepo.UpsertAsync(new Dividend
                    {
                        StockId      = stock.Id,
                        ExDate       = row.ExDate,
                        RecordDate   = row.RecordDate,
                        PaymentDate  = row.PaymentDate,
                        Dps          = row.Dps,
                        DividendType = "regular",
                        Source       = Source,
                        ScrapeRunId  = _currentScrapeRunId,
                    }, ct);
                    upserted++;
                }

                Logger.LogInformation("[StockAnalysisDividends] {Symbol}: {Count} rows upserted", symbol, upserted);
                scraped++;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to scrape {Symbol} from StockAnalysisDividends", symbol);
                errors.Add($"{symbol}: {ex.Message}");
                failed++;
            }
        }

        return (scraped, failed, errors);
    }

    private async Task<List<DividendRow>?> FetchHistoryAsync(
        IBrowserContext context, string symbol, CancellationToken ct)
    {
        // One retry on render timeout (same pattern as StockAnalysisScraper).
        const int maxAttempts = 2;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var (outcome, data) = await TryFetchAsync(context, symbol, ct);
            if (outcome != FetchOutcome.RetryableTimeout) return data;
            if (attempt < maxAttempts) await Task.Delay(2000, ct);
        }
        Logger.LogWarning("[StockAnalysisDividends] {Symbol}: render timeout after retry", symbol);
        return null;
    }

    private enum FetchOutcome { Success, NoData, RetryableTimeout }

    private async Task<(FetchOutcome outcome, List<DividendRow>? data)> TryFetchAsync(
        IBrowserContext context, string symbol, CancellationToken ct)
    {
        var url = $"{BaseUrl}/quote/psx/{symbol}/dividend/";
        var page = await context.NewPageAsync();
        try
        {
            var resp = await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30_000,
            });

            if (resp is null || resp.Status == 404) return (FetchOutcome.NoData, null);
            if (!resp.Ok)
            {
                Logger.LogWarning("[StockAnalysisDividends] {Symbol}: HTTP {Status}", symbol, resp.Status);
                return (FetchOutcome.NoData, null);
            }

            // Either the dividend table renders, or the page shows a no-dividend message.
            // Wait for one of those signals; treat genuine absence as NoData (not retryable).
            try
            {
                await page.WaitForFunctionAsync(
                    """
                    () => {
                      const tbody = document.querySelector('table tbody');
                      if (tbody && tbody.querySelectorAll('tr').length > 0) return true;
                      const text = document.body.innerText.toLowerCase();
                      return text.includes('does not pay') || text.includes('no dividend');
                    }
                    """,
                    null, new PageWaitForFunctionOptions { Timeout = 25_000 });
            }
            catch (TimeoutException)
            {
                return (FetchOutcome.RetryableTimeout, null);
            }

            // Expand to full history via the "All" button if present.
            await page.EvaluateAsync("""
                () => {
                    const btn = Array.from(document.querySelectorAll('button'))
                        .find(b => b.textContent?.trim() === 'All');
                    if (btn) btn.click();
                }
            """);
            await Task.Delay(400, ct);

            var raw = await page.EvaluateAsync<string[][]>("""
                () => {
                    const tbody = document.querySelector('table tbody');
                    if (!tbody) return [];
                    return Array.from(tbody.querySelectorAll('tr')).map(tr =>
                        Array.from(tr.querySelectorAll('td')).map(td => td.textContent?.trim() ?? '')
                    );
                }
            """);

            if (raw is null || raw.Length == 0) return (FetchOutcome.NoData, null);

            var parsed = new List<DividendRow>(raw.Length);
            foreach (var cells in raw)
            {
                if (cells.Length < 2) continue;
                var ex = ParseUsDate(cells[0]);
                var dps = ParseCashPkr(cells[1]);
                if (ex is null || dps is null || dps <= 0) continue;
                parsed.Add(new DividendRow(
                    ExDate: ex.Value,
                    Dps: dps.Value,
                    RecordDate:  cells.Length > 2 ? ParseUsDate(cells[2]) : null,
                    PaymentDate: cells.Length > 3 ? ParseUsDate(cells[3]) : null));
            }

            return parsed.Count > 0
                ? (FetchOutcome.Success, parsed)
                : (FetchOutcome.NoData,  null);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ── Parsers ───────────────────────────────────────────────────────────────

    /// <summary>"May 11, 2026" → DateOnly. Returns null for empty/"-"/unparseable.</summary>
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

    /// <summary>"9.000 PKR" → 9.000. The "Cash Amount" label is currency-agnostic;
    /// PSX stocks render in PKR. We strip any trailing currency suffix and parse.</summary>
    private static decimal? ParseCashPkr(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw is "-" or "N/A") return null;
        var s = raw.Trim();
        // Strip a trailing 3-letter ISO suffix (PKR, USD, etc.) and any surrounding whitespace.
        var lastSpace = s.LastIndexOf(' ');
        if (lastSpace > 0 && s.Length - lastSpace - 1 == 3 && char.IsLetter(s[lastSpace + 1]))
            s = s[..lastSpace];
        s = s.Replace(",", "").Trim();
        return decimal.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) && v >= 0
            ? Math.Round(v, 4)
            : null;
    }

    private record DividendRow(
        DateOnly ExDate,
        decimal Dps,
        DateOnly? RecordDate,
        DateOnly? PaymentDate);
}
