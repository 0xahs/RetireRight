using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using RetireRight.Core.Attributes;
using RetireRight.Core.Configuration;
using RetireRight.Core.Enums;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Scrapers.AntiBot;
using RetireRight.Scrapers.Base;

namespace RetireRight.Scrapers.Sources;

/// <summary>
/// Scrapes historical dividend data from psxterminal.com via the SvelteKit __data.json endpoint.
/// Returns 6+ years of per-dividend records (ex-date, payment-date, DPS) — far more history
/// than Sarmaaya's 2-year window, enabling a12_consecutive_div_years and b3_dps_cagr_5y.
///
/// Endpoint: GET https://psxterminal.com/symbol/{TICKER}/__data.json
/// The response uses SvelteKit's flat deduplication format: a data[] array where
/// integer values inside objects/arrays are indices into that array.
/// </summary>
// Re-enabled as a cross-validation dividend source alongside StockAnalysisDividends.
// PsxTerminal carries forward-announced ex-dates (sometimes the only source — e.g. ATLH
// where stockanalysis.com's history table lags) plus 6+ years of per-stock history.
// Order=80 → runs before StockAnalysisDividends (Order=90); the smart merge in
// DividendRepository keeps confirmed values from either source over estimates.
// Also scrapes psxterminal.com/__data.json futureDividends as a discovery layer for
// stocks not in watchlist; values there are net-of-WHT so we gross them up to match
// the rest of the system's gross-DPS convention.
[DataSource("PsxTerminal", PerSymbol = true, Order = 80)]
public class PsxTerminalScraper : BaseScraper
{
    private const string BaseUrl = "https://psxterminal.com";

    private readonly IStockRepository _stockRepo;
    private readonly IDividendRepository _dividendRepo;
    private readonly HttpClient _http;
    private Guid? _currentScrapeRunId;

    public override ScrapingMethod Method => ScrapingMethod.Api;

    public PsxTerminalScraper(
        ILogger<PsxTerminalScraper> logger,
        RequestThrottler throttler,
        RetryPolicy retryPolicy,
        DataSourceOptions sourceConfig,
        IStockRepository stockRepo,
        IDividendRepository dividendRepo)
        : base(logger, throttler, retryPolicy, sourceConfig)
    {
        _stockRepo = stockRepo;
        _dividendRepo = dividendRepo;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
        _http.DefaultRequestHeaders.Add("Referer", BaseUrl);
    }

    protected override async Task<(int scraped, int failed, List<string> errors)> ExecuteScrapeAsync(
        ScrapeRequest request, CancellationToken ct)
    {
        var scraped = 0;
        var failed = 0;
        var errors = new List<string>();
        _currentScrapeRunId = request.ScrapeRunId;

        // Step 1: scrape the homepage's "Upcoming Dividends" section first. Two purposes:
        //   (a) capture symbols with announced ex-dates that aren't in the watchlist (e.g.
        //       AABS / ATLH / HCAR) so the per-stock loop below also covers them, and
        //   (b) seed the dividends table with the announcement values directly. Values on
        //       this endpoint are net-of-WHT — grossed up before storage.
        var homepageSymbols = await FetchAndUpsertHomepageUpcomingAsync(ct);

        var baseSymbols = request.Symbols.Length > 0
            ? request.Symbols
            : (await _stockRepo.GetWatchlistAsync(ct)).Select(s => s.Symbol).ToArray();

        var symbols = baseSymbols
            .Union(homepageSymbols, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var symbol in symbols)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await ThrottleAsync(ct);
                var dividends = await FetchDividendsAsync(symbol, ct);
                if (dividends is null || dividends.Count == 0)
                {
                    errors.Add($"{symbol}: no dividends found");
                    failed++;
                    continue;
                }

                var stock = await _stockRepo.GetBySymbolAsync(symbol, ct);
                if (stock is null)
                {
                    errors.Add($"{symbol}: not in DB — run Sarmaaya scrape first");
                    failed++;
                    continue;
                }

                foreach (var div in dividends)
                {
                    div.StockId = stock.Id;
                    div.ScrapeRunId = _currentScrapeRunId;
                    await _dividendRepo.UpsertAsync(div, ct);
                }

                Logger.LogInformation("[PsxTerminal] {Symbol}: {Count} dividends upserted", symbol, dividends.Count);
                scraped++;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to scrape {Symbol} from PsxTerminal", symbol);
                errors.Add($"{symbol}: {ex.Message}");
                failed++;
            }
        }

        return (scraped, failed, errors);
    }

    private async Task<List<Dividend>?> FetchDividendsAsync(string symbol, CancellationToken ct)
    {
        var url = $"{BaseUrl}/symbol/{symbol}/__data.json";
        var json = await WithRetryAsync(
            () => _http.GetStringAsync(url, ct),
            $"GET {url}", ct);

        var doc = JsonNode.Parse(json);
        if (doc is null) return null;

        // SvelteKit format: nodes[1] contains symbol-page data with dividendsData key
        var nodes = doc["nodes"]?.AsArray();
        if (nodes is null || nodes.Count < 2) return null;

        var node1 = nodes[1];
        var rawData = node1?["data"]?.AsArray();
        if (rawData is null || rawData.Count == 0) return null;

        var root = rawData[0]?.AsObject();
        if (root is null) return null;

        var divIdx = root["dividendsData"]?.GetValue<int>();
        if (divIdx is null) return null;

        var divList = Deref(rawData, divIdx.Value)?.AsArray();
        if (divList is null) return null;

        var results = new List<Dividend>();
        foreach (var item in divList)
        {
            // divList contains integer indices into rawData — dereference each one
            var entryNode = DerefOnce(rawData, item);
            var entry = entryNode?.AsObject();
            if (entry is null) continue;

            var exDateStr = DerefString(rawData, entry["ex_date"]);
            var payDateStr = DerefString(rawData, entry["payment_date"]);
            var recDateStr = DerefString(rawData, entry["record_date"]);
            var amountNode = DerefOnce(rawData, entry["amount"]);
            var yearNode = DerefOnce(rawData, entry["year"]);

            if (exDateStr is null) continue;
            if (!DateOnly.TryParse(exDateStr, out var exDate)) continue;

            // amount is stored as an integer or decimal — NOT as a further index
            decimal dps;
            if (amountNode is JsonValue amtVal)
            {
                try { dps = amtVal.GetValue<decimal>(); }
                catch { continue; }
            }
            else continue; // object (corrupt entry) — skip

            DateOnly? payDate = payDateStr is not null && DateOnly.TryParse(payDateStr, out var pd) ? pd : null;
            DateOnly? recDate = recDateStr is not null && DateOnly.TryParse(recDateStr, out var rd) ? rd : null;
            string? year = yearNode is JsonValue yv ? yv.GetValue<int>().ToString() : null;

            results.Add(new Dividend
            {
                ExDate = exDate,
                PaymentDate = payDate,
                RecordDate = recDate,
                Dps = dps,
                Year = year,
                DividendType = "regular",
                Source = Source,
            });
        }

        return results.Count > 0 ? results : null;
    }

    /// <summary>
    /// Hits psxterminal.com/__data.json and parses the homepage's "Upcoming Dividends"
    /// section (root.futureDividends). Upserts each row into the dividends table and
    /// returns the set of symbols seen so the per-stock loop can also process them.
    ///
    /// Source values are NET-of-WHT (verified: AABS 6.38 = 7.5 × 0.85; HCAR 7.65 = 9.0 × 0.85).
    /// We gross them up via the configured filer WHT rate (0.15) so they're stored in the
    /// same gross-DPS convention as every other source.
    /// </summary>
    private async Task<List<string>> FetchAndUpsertHomepageUpcomingAsync(CancellationToken ct)
    {
        const decimal whtRate = 0.15m;
        var seenSymbols = new List<string>();

        try
        {
            var json = await WithRetryAsync(
                () => _http.GetStringAsync($"{BaseUrl}/__data.json", ct),
                $"GET {BaseUrl}/__data.json", ct);

            var doc = JsonNode.Parse(json);
            var rawData = doc?["nodes"]?[0]?["data"]?.AsArray();
            if (rawData is null || rawData.Count == 0) return seenSymbols;

            var root = rawData[0]?.AsObject();
            var fdIdx = root?["futureDividends"]?.GetValue<int>();
            if (fdIdx is null) return seenSymbols;

            var list = Deref(rawData, fdIdx.Value)?.AsArray();
            if (list is null) return seenSymbols;

            foreach (var item in list)
            {
                var entry = DerefOnce(rawData, item)?.AsObject();
                if (entry is null) continue;

                var symbol  = DerefString(rawData, entry["symbol"]);
                var exStr   = DerefString(rawData, entry["ex_date"]);
                var amtNode = DerefOnce  (rawData, entry["amount"]);

                if (symbol is null || exStr is null) continue;
                if (!DateOnly.TryParse(exStr, out var exDate)) continue;
                if (amtNode is not JsonValue amtVal) continue;
                decimal netAmount;
                try { netAmount = amtVal.GetValue<decimal>(); }
                catch { continue; }
                if (netAmount <= 0) continue;

                var grossDps = Math.Round(netAmount / (1m - whtRate), 4);

                // payment_date / record_date on this endpoint are placeholders (always ==
                // ex_date for these entries). Leave them null — per-stock scrape or the
                // StockAnalysisDividends scrape will fill them in if available.
                var stock = await _stockRepo.GetBySymbolAsync(symbol, ct);
                if (stock is null)
                {
                    Logger.LogDebug("[PsxTerminal] upcoming {Symbol}: not in DB — skipping seed", symbol);
                    seenSymbols.Add(symbol);
                    continue;
                }

                await _dividendRepo.UpsertAsync(new Dividend
                {
                    StockId      = stock.Id,
                    ExDate       = exDate,
                    Dps          = grossDps,
                    DividendType = "regular",
                    Source       = Source,
                    ScrapeRunId  = _currentScrapeRunId,
                }, ct);
                seenSymbols.Add(symbol);
            }

            Logger.LogInformation("[PsxTerminal] upcoming homepage: {Count} announced dividends upserted",
                seenSymbols.Count);
        }
        catch (Exception ex)
        {
            // Non-fatal: per-stock loop continues even if homepage scrape fails.
            Logger.LogWarning(ex, "[PsxTerminal] homepage upcoming-dividends scrape failed — continuing per-stock");
        }

        return seenSymbols;
    }

    // Dereference one level: if val is an int index, return rawData[val]; else return val.
    private static JsonNode? DerefOnce(JsonArray rawData, JsonNode? val)
    {
        if (val is JsonValue jv && jv.TryGetValue<int>(out var idx) && idx >= 0 && idx < rawData.Count)
            return rawData[idx];
        return val;
    }

    // Dereference up to two levels to get the container (dict/array) at an index.
    private static JsonNode? Deref(JsonArray rawData, int idx)
    {
        if (idx < 0 || idx >= rawData.Count) return null;
        return rawData[idx];
    }

    private static string? DerefString(JsonArray rawData, JsonNode? val)
    {
        var node = DerefOnce(rawData, val);
        return node is JsonValue sv ? sv.GetValue<string>() : null;
    }

    public override ValueTask DisposeAsync()
    {
        _http.Dispose();
        return ValueTask.CompletedTask;
    }
}
