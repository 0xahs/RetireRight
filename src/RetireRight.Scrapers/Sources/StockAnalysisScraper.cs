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
/// StockAnalysis.com scraper — fills the cash flow gap left by Sarmaaya's 404 endpoint.
/// Scrapes only 4 rows from the cash flow statement page, producing 8 keys:
///   operating_cash_flow, capital_expenditures, free_cash_flow, total_dividends_paid
///   + _prev_year variants for each, plus fcf_prev_year alias.
/// All values are in millions PKR — same scale as Sarmaaya. No unit conversion needed.
/// </summary>
[DataSource("StockAnalysis", PerSymbol = true, Order = 30)]
public class StockAnalysisScraper : BaseScraper
{
    private const string BaseUrl = "https://stockanalysis.com";

    private static readonly Dictionary<string, string> TargetRows = new()
    {
        ["Operating Cash Flow"] = "operating_cash_flow",
        ["Capital Expenditures"] = "capital_expenditures",
        ["Free Cash Flow"] = "free_cash_flow",
        ["Common Dividends Paid"] = "total_dividends_paid"
    };

    // Dividends Paid is a cash outflow (negative on page) — store as positive absolute value
    private static readonly HashSet<string> NegateKeys = ["total_dividends_paid"];

    private readonly IStockRepository _stockRepo;
    private readonly IFundamentalsRepository _fundamentalsRepo;
    private Guid? _currentScrapeRunId;

    public override ScrapingMethod Method => ScrapingMethod.Browser;

    public StockAnalysisScraper(
        ILogger<StockAnalysisScraper> logger,
        RequestThrottler throttler,
        RetryPolicy retryPolicy,
        DataSourceOptions sourceConfig,
        IStockRepository stockRepo,
        IFundamentalsRepository fundamentalsRepo)
        : base(logger, throttler, retryPolicy, sourceConfig)
    {
        _stockRepo = stockRepo;
        _fundamentalsRepo = fundamentalsRepo;
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
            Headless = true
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

                var data = await FetchCashFlowAsync(context, symbol, ct);
                if (data is null || data.Count == 0)
                {
                    errors.Add($"{symbol}: no cash flow data found");
                    failed++;
                    continue;
                }

                var stock = await _stockRepo.GetBySymbolAsync(symbol, ct);
                if (stock is null)
                {
                    errors.Add($"{symbol}: not found in DB — run Sarmaaya scrape first");
                    failed++;
                    continue;
                }

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                await _fundamentalsRepo.UpsertAsync(new StockFundamental
                {
                    StockId = stock.Id,
                    ScrapedDate = today,
                    Periodicity = "ANN",
                    Source = Source,
                    Data = data,
                    ScrapeRunId = _currentScrapeRunId
                }, ct);

                Logger.LogInformation("[StockAnalysis] {Symbol}: {Count} CF keys stored", symbol, data.Count);
                scraped++;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to scrape {Symbol} from StockAnalysis", symbol);
                errors.Add($"{symbol}: {ex.Message}");
                failed++;
            }
        }

        return (scraped, failed, errors);
    }

    private async Task<Dictionary<string, decimal?>?> FetchCashFlowAsync(
        IBrowserContext context, string symbol, CancellationToken ct)
    {
        // One retry on timeout — StockAnalysis Svelte pages occasionally render
        // slowly for thinly-covered tickers. A second attempt with a fresh page
        // usually succeeds.
        const int maxAttempts = 2;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var result = await TryFetchCashFlowAsync(context, symbol, attempt, ct);
            if (result.kind != FetchOutcome.RetryableTimeout) return result.data;

            if (attempt < maxAttempts)
            {
                Logger.LogInformation("[StockAnalysis] {Symbol}: timed out, retrying (attempt {Next}/{Max})",
                    symbol, attempt + 1, maxAttempts);
                await Task.Delay(2000, ct);
            }
            else
            {
                Logger.LogWarning("[StockAnalysis] {Symbol}: page timed out waiting for table after {Attempts} attempts",
                    symbol, maxAttempts);
            }
        }
        return null;
    }

    private enum FetchOutcome { Success, NoData, RetryableTimeout }

    private async Task<(FetchOutcome kind, Dictionary<string, decimal?>? data)> TryFetchCashFlowAsync(
        IBrowserContext context, string symbol, int attempt, CancellationToken ct)
    {
        var url = $"{BaseUrl}/quote/psx/{symbol}/financials/cash-flow-statement/";
        var page = await context.NewPageAsync();

        try
        {
            var response = await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30000
            });

            if (response is null || !response.Ok)
            {
                Logger.LogWarning("[StockAnalysis] {Symbol}: HTTP {Status}", symbol, response?.Status);
                return (FetchOutcome.NoData, null);
            }

            // Wait until the cash flow table has rendered its data rows.
            // Svelte renders asynchronously — DOMContentLoaded fires before data appears.
            // 30s ceiling covers slow days; combined with one retry above we get ~2 minutes total.
            await page.WaitForFunctionAsync(
                "() => Array.from(document.querySelectorAll('tr td')).some(td => td.textContent.trim() === 'Operating Cash Flow')",
                null, new PageWaitForFunctionOptions { Timeout = 30000 });

            // EvaluateAsync<T> uses case-sensitive JSON by default — return a string and deserialize ourselves
            var rawJson = await page.EvaluateAsync<string>(ExtractScript);

            if (string.IsNullOrEmpty(rawJson) || rawJson is "{}" or "null")
            {
                Logger.LogWarning("[StockAnalysis] {Symbol}: cash flow table not found or empty", symbol);
                return (FetchOutcome.NoData, null);
            }

            var raw = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, RawCfEntry>>(
                rawJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (raw is null || raw.Count == 0)
            {
                Logger.LogWarning("[StockAnalysis] {Symbol}: failed to parse cash flow JSON", symbol);
                return (FetchOutcome.NoData, null);
            }

            return (FetchOutcome.Success, ParseCashFlow(raw));
        }
        catch (TimeoutException)
        {
            return (FetchOutcome.RetryableTimeout, null);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static Dictionary<string, decimal?> ParseCashFlow(Dictionary<string, RawCfEntry> raw)
    {
        var result = new Dictionary<string, decimal?>();

        foreach (var (key, entry) in raw)
        {
            var current = ParseValue(entry.Current);
            var prev = ParseValue(entry.Prev);

            if (NegateKeys.Contains(key))
            {
                current = current.HasValue ? Math.Abs(current.Value) : null;
                prev = prev.HasValue ? Math.Abs(prev.Value) : null;
            }

            if (current.HasValue) result[key] = current;
            if (prev.HasValue) result[$"{key}_prev_year"] = prev;
        }

        // fcf_prev_year alias used by DividendGrowthCalculator
        if (result.TryGetValue("free_cash_flow_prev_year", out var fcfPrev))
            result["fcf_prev_year"] = fcfPrev;

        return result;
    }

    private static decimal? ParseValue(string? text)
    {
        if (string.IsNullOrEmpty(text) || text == "-") return null;
        var clean = text.Replace(",", "").Trim();
        return decimal.TryParse(clean, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    // Injected into page — finds the most recent FY column index dynamically.
    // Returns JSON.stringify so C# can deserialize with case-insensitive options.
    private const string ExtractScript = """
        () => {
            const TARGETS = {
                'Operating Cash Flow': 'operating_cash_flow',
                'Capital Expenditures': 'capital_expenditures',
                'Free Cash Flow': 'free_cash_flow',
                'Common Dividends Paid': 'total_dividends_paid'
            };

            const rows = document.querySelectorAll('tr');
            if (rows.length < 2) return '{}';

            // Find most recent annual FY column index (skip TTM)
            const headerCells = rows[0].querySelectorAll('th');
            let fyIdx = -1;
            for (let i = 0; i < headerCells.length; i++) {
                const h = headerCells[i].textContent.trim().split('\n')[0].trim();
                if (h.startsWith('FY')) { fyIdx = i; break; }
            }
            if (fyIdx < 0) return '{}';

            const result = {};
            Array.from(rows).slice(2).forEach(row => {
                const cells = row.querySelectorAll('td');
                if (cells.length <= fyIdx) return;
                const label = cells[0].textContent.trim();
                const key = TARGETS[label];
                if (!key) return;
                result[key] = {
                    current: cells[fyIdx]?.textContent.trim() ?? null,
                    prev: cells[fyIdx + 1]?.textContent.trim() ?? null
                };
            });

            return JSON.stringify(result);
        }
        """;

    public override async ValueTask DisposeAsync()
    {
        // Playwright resources are created and disposed per ExecuteScrapeAsync call
        await base.DisposeAsync();
    }
}

/// <summary>Raw cell text pair extracted from the page before numeric parsing.</summary>
internal class RawCfEntry
{
    public string? Current { get; set; }
    public string? Prev { get; set; }
}
