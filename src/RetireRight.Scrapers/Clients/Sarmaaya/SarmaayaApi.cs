using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RetireRight.Core.Configuration;
using RetireRight.Scrapers.AntiBot;
using RetireRight.Scrapers.Models;

namespace RetireRight.Scrapers.Clients.Sarmaaya;

/// <summary>
/// Sarmaaya REST API client — hits beta-restapi.sarmaaya.pk.
/// Per-stock: price, dividends, about, returns, fundamentals (ratios + income + balance sheet,
/// ANN + LTM), industry averages, price history, ownership, insiders, announcements.
/// Bulk: KSE100 ticker prices, market index levels.
/// Extends generic <see cref="ApiClient"/> which provides all anti-bot infrastructure.
/// </summary>
public class SarmaayaApi : ApiClient
{
    private const string SarmaayaApiBaseUrl = "https://beta-restapi.sarmaaya.pk";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public override string Name => "Sarmaaya:API";
    public override int Priority => 1; // Fastest — try first

    public SarmaayaApi(
        ILogger<SarmaayaApi> logger,
        RetryPolicy retryPolicy,
        DataSourceOptions sourceConfig,
        string? accessToken = null)
        : base(SarmaayaApiBaseUrl, logger, retryPolicy, sourceConfig)
    {
        if (accessToken is not null)
            Http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public override async Task<ScrapedStockData?> FetchAsync(string symbol, CancellationToken ct)
    {
        await WarmUpSessionAsync($"/api/stocks/{symbol}", ct);

        // ── 1. Stock price (required — if this fails, entire fetch fails) ──
        var stock = await GetJsonAsync<SarmaayaEnvelope<SarmaayaStockResponse>>(
            $"/api/stocks/{symbol}", JsonOptions, ct);

        var price = stock?.Data is { Success: true, Response: not null }
            ? stock.Data.Response
            : null;

        if (price is null)
            return null;
        var rawResponses = new List<RawResponseEntry>();

        AddRaw(rawResponses, $"/api/stocks/{symbol}", stock);

        // ── 2. Dividends (optional) ──
        var dividends = await FetchDividendsAsync(symbol, rawResponses, ct);

        // ── 3. Company info → ISIN for fundamentals (optional) ──
        var (isin, industry) = await FetchAboutAsync(symbol, rawResponses, ct);
        isin ??= price.Isin;

        // ── 4. Returns vs KSE100 (optional) ──
        var returns = await FetchReturnsAsync(symbol, rawResponses, ct);

        // ── 5. Fundamentals — needs ISIN (optional) ──
        var fundamentals = new Dictionary<string, decimal?>();
        var ownership = new List<ScrapedOwnership>();

        if (isin is not null)
        {
            // Annual statements
            await FetchFundamentalsAsync(isin, "ratios", "ratio", fundamentals, rawResponses, ct);
            await FetchFundamentalsAsync(isin, "income-statement", "income", fundamentals, rawResponses, ct);
            await FetchFundamentalsAsync(isin, "balance-sheet", "bs", fundamentals, rawResponses, ct);

            // LTM (trailing 12-month) — prefixed separately so metrics don't overwrite annual
            await FetchFundamentalsAsync(isin, "ratios", "ltm_ratio", fundamentals, rawResponses, ct, periodicity: "LTM");
            await FetchFundamentalsAsync(isin, "income-statement", "ltm_income", fundamentals, rawResponses, ct, periodicity: "LTM");

            // Industry averages — benchmarks for peer comparison
            await FetchIndustryAveragesAsync(isin, fundamentals, rawResponses, ct);

            // Ownership (requires ISIN)
            ownership = await FetchOwnershipAsync(isin, rawResponses, ct);
        }

        // ── 6. Price history — 1 year backfill (optional, uses symbol) ──
        var priceHistory = await FetchPriceHistoryAsync(symbol, rawResponses, ct);

        // ── 7. Insider transactions (optional, uses symbol) ──
        var insiders = await FetchInsiderTransactionsAsync(symbol, rawResponses, ct);

        // ── 8. Announcements — last 6 months (optional, uses symbol) ──
        var announcements = await FetchAnnouncementsAsync(symbol, rawResponses, ct);

        return new ScrapedStockData
        {
            Symbol = symbol,
            Source = Name,
            Close = price.Close,
            Open = price.Open,
            High = price.High,
            Low = price.Low,
            Volume = price.Volume,
            ChangePct = price.ChangePercentage,
            High52 = price.High52,
            Low52 = price.Low52,
            PriceDate = ParseDate(price.Date),
            Name = price.Name,
            Isin = isin,
            SectorName = price.SectorName,
            IndustryName = industry,
            Dividends = dividends,
            Fundamentals = fundamentals,
            Returns = returns,
            PriceHistory = priceHistory,
            Ownership = ownership,
            InsiderTransactions = insiders,
            Announcements = announcements,
            RawResponses = rawResponses
        };
    }

    // ─── Endpoint Fetchers ────────────────────────────────────

    private async Task<List<ScrapedDividend>> FetchDividendsAsync(
        string symbol, List<RawResponseEntry> rawResponses, CancellationToken ct)
    {
        var dividends = new List<ScrapedDividend>();

        var result = await GetJsonAsync<SarmaayaEnvelope<SarmaayaDividendResponse>>(
            $"/api/stocks/dividends/{symbol}", JsonOptions, ct);

        if (result?.Data?.Response?.PayoutHistory is not { Count: > 0 })
            return dividends;

        AddRaw(rawResponses, $"/api/stocks/dividends/{symbol}", result);

        foreach (var d in result.Data.Response.PayoutHistory)
        {
            if (d.DividendPerShare is null or <= 0) continue;
            if (!DateOnly.TryParse(d.AnnouncementDate, out var date)) continue;

            dividends.Add(new ScrapedDividend
            {
                AnnouncementDate = date,
                Dps = d.DividendPerShare.Value,
                Type = d.PayoutType ?? "dividend",
                Percentage = d.Percentage,
                Year = d.Year
            });
        }

        Logger.LogDebug("Fetched {Count} dividends for {Symbol}", dividends.Count, symbol);
        return dividends;
    }

    private async Task<(string? isin, string? industry)> FetchAboutAsync(
        string symbol, List<RawResponseEntry> rawResponses, CancellationToken ct)
    {
        var result = await GetJsonAsync<SarmaayaEnvelope<SarmaayaAboutResponse>>(
            $"/api/stocks/about?symbol={symbol}", JsonOptions, ct);

        if (result?.Data?.Response is null)
            return (null, null);

        AddRaw(rawResponses, $"/api/stocks/about?symbol={symbol}", result);

        return (result.Data.Response.Isin, result.Data.Response.Industry);
    }

    private async Task<Dictionary<string, decimal?>> FetchReturnsAsync(
        string symbol, List<RawResponseEntry> rawResponses, CancellationToken ct)
    {
        var returns = new Dictionary<string, decimal?>();

        var result = await GetJsonAsync<SarmaayaEnvelope<JsonElement>>(
            $"/api/stocks/returns/{symbol}", JsonOptions, ct);

        if (result?.Data is not { Success: true } || result.Data.Response.ValueKind != JsonValueKind.Array)
            return returns;

        AddRaw(rawResponses, $"/api/stocks/returns/{symbol}", result);

        foreach (var entry in result.Data.Response.EnumerateArray())
        {
            if (!entry.TryGetProperty("name", out var nameProp) ||
                nameProp.GetString() != symbol)
                continue;

            foreach (var period in new[] { "1w", "1m", "3m", "6m", "1y" })
            {
                if (entry.TryGetProperty(period, out var valProp))
                {
                    var str = valProp.ValueKind == JsonValueKind.String
                        ? valProp.GetString()
                        : valProp.ToString();

                    if (decimal.TryParse(str, out var val) && val != 0)
                        returns[period] = val;
                }
            }
            break;
        }

        return returns;
    }

    private async Task FetchFundamentalsAsync(
        string isin, string statementType, string keyPrefix,
        Dictionary<string, decimal?> fundamentals,
        List<RawResponseEntry> rawResponses, CancellationToken ct,
        string periodicity = "ANN")
    {
        var endpoint = $"/api/stocks/fundamentals/{statementType}?isin={isin}&periodicity={periodicity}";
        var result = await GetJsonAsync<SarmaayaEnvelope<Dictionary<string, SarmaayaFundamentalMetric>>>(
            endpoint, JsonOptions, ct);

        if (result?.Data?.Response is null)
            return;

        AddRaw(rawResponses, endpoint, result);

        foreach (var (displayName, metric) in result.Data.Response)
        {
            if (metric.Data is not { Count: > 0 }) continue;

            var normalizedName = NormalizeKey(displayName);
            var metricKey = $"{keyPrefix}_{normalizedName}";

            // Extract latest value (data[0])
            var latest = metric.Data[0];
            var latestVal = ParseFundamentalValue(latest.Value);
            if (latestVal is not null)
                fundamentals[metricKey] = latestVal;

            // Extract prior-year value (data[1]) for growth calculations.
            // Sarmaaya fills historical years with placeholder strings — data[0] is a JSON number
            // but data[1..5] are JSON strings with a repeated dummy value (e.g. "88.516").
            // Only trust data[1] if it's a JSON number (real) or if it differs from data[2+]
            // (proving it's not a repeated placeholder).
            if (periodicity == "ANN" && metric.Data.Count >= 2)
            {
                var prevEntry = metric.Data[1];
                if (prevEntry.Value.ValueKind == JsonValueKind.Number)
                {
                    // JSON number = real data from Sarmaaya
                    var prevVal = prevEntry.Value.GetDecimal();
                    if (prevVal != latestVal)
                        fundamentals[$"{metricKey}_prev_year"] = prevVal;
                }
                else if (prevEntry.Value.ValueKind == JsonValueKind.String
                         && metric.Data.Count >= 3)
                {
                    // String value — only trust if it differs from data[2] (not a repeated placeholder)
                    var prevStr = prevEntry.Value.GetString();
                    var nextStr = metric.Data[2].Value.ValueKind == JsonValueKind.String
                        ? metric.Data[2].Value.GetString()
                        : metric.Data[2].Value.ToString();
                    if (prevStr != nextStr && decimal.TryParse(prevStr, out var prevVal) && prevVal != latestVal)
                        fundamentals[$"{metricKey}_prev_year"] = prevVal;
                }
            }
        }
    }

    private async Task FetchIndustryAveragesAsync(
        string isin, Dictionary<string, decimal?> fundamentals,
        List<RawResponseEntry> rawResponses, CancellationToken ct)
    {
        var endpoint = $"/api/stocks/fundamentals/industry/{isin}";
        var result = await GetJsonAsync<SarmaayaEnvelope<SarmaayaIndustryResponse>>(
            endpoint, JsonOptions, ct);

        if (result?.Data?.Response?.IndustryAverages is not { Count: > 0 })
            return;

        AddRaw(rawResponses, endpoint, result);

        // Sarmaaya sometimes returns placeholder data (all values identical, e.g. 2.22).
        // Detect and skip if all averages share the same value.
        var distinctValues = result.Data.Response.IndustryAverages
            .Where(a => a.Average is not null)
            .Select(a => a.Average!.Value)
            .Distinct()
            .Take(2)
            .ToList();
        if (distinctValues.Count <= 1)
        {
            Logger.LogWarning("Industry averages for {Isin} appear to be placeholder data (all values = {Value}), skipping",
                isin, distinctValues.FirstOrDefault());
            return;
        }

        foreach (var avg in result.Data.Response.IndustryAverages)
        {
            if (avg.Name is null || avg.Average is null) continue;
            var metricKey = $"industry_{NormalizeKey(avg.Name)}";
            fundamentals[metricKey] = avg.Average;
        }
    }

    private async Task<List<ScrapedPriceEntry>> FetchPriceHistoryAsync(
        string symbol, List<RawResponseEntry> rawResponses, CancellationToken ct)
    {
        var history = new List<ScrapedPriceEntry>();

        var endpoint = $"/api/stocks/price-history/{symbol}?days=365";
        var result = await GetJsonAsync<SarmaayaEnvelope<List<SarmaayaPriceHistoryEntry>>>(
            endpoint, JsonOptions, ct);

        if (result?.Data?.Response is not { Count: > 0 })
            return history;

        AddRaw(rawResponses, endpoint, result);

        foreach (var entry in result.Data.Response)
        {
            // API returns ISO 8601 datetime ("2026-03-19T00:00:00.000Z") — parse as DateTime, extract date
            if (!DateTime.TryParse(entry.Date, out var dt)) continue;
            var date = DateOnly.FromDateTime(dt);

            history.Add(new ScrapedPriceEntry
            {
                Date = date,
                Price = entry.Price,
                Volume = entry.Volume,
                MarketCap = entry.MarketCap
            });
        }

        Logger.LogInformation("Fetched {Count} price history entries for {Symbol}", history.Count, symbol);
        return history;
    }

    private async Task<List<ScrapedOwnership>> FetchOwnershipAsync(
        string isin, List<RawResponseEntry> rawResponses, CancellationToken ct)
    {
        var owners = new List<ScrapedOwnership>();

        // API returns response as a direct array, not wrapped in { equityHolders: [...] }
        var endpoint = $"/api/stocks/ownership/equity-holders/{isin}";
        var result = await GetJsonAsync<SarmaayaEnvelope<List<SarmaayaOwnershipEntry>>>(
            endpoint, JsonOptions, ct);

        if (result?.Data?.Response is not { Count: > 0 })
            return owners;

        AddRaw(rawResponses, endpoint, result);

        foreach (var h in result.Data.Response)
        {
            owners.Add(new ScrapedOwnership
            {
                HolderName = h.HolderName,
                HolderType = h.HolderType,
                InvestorType = h.InvestorType,
                Shares = h.Shares,
                WeightPct = h.WeightClose
            });
        }

        Logger.LogInformation("Fetched {Count} equity holders for ISIN {Isin}", owners.Count, isin);
        return owners;
    }

    private async Task<List<ScrapedInsiderTrade>> FetchInsiderTransactionsAsync(
        string symbol, List<RawResponseEntry> rawResponses, CancellationToken ct)
    {
        var trades = new List<ScrapedInsiderTrade>();

        var endpoint = $"/api/stocks/stock-insiders/{symbol}";
        var result = await GetJsonAsync<SarmaayaEnvelope<List<SarmaayaInsiderEntry>>>(
            endpoint, JsonOptions, ct);

        if (result?.Data?.Response is not { Count: > 0 })
            return trades;

        AddRaw(rawResponses, endpoint, result);

        foreach (var t in result.Data.Response)
        {
            trades.Add(new ScrapedInsiderTrade
            {
                Name = t.Name,
                Position = t.Position,
                Action = t.Action,
                Quantity = t.Quantity,
                Rate = t.Rate,
                Date = ParseDate(t.Date)
            });
        }

        Logger.LogDebug("Fetched {Count} insider trades for {Symbol}", trades.Count, symbol);
        return trades;
    }

    private async Task<List<ScrapedAnnouncement>> FetchAnnouncementsAsync(
        string symbol, List<RawResponseEntry> rawResponses, CancellationToken ct)
    {
        var announcements = new List<ScrapedAnnouncement>();

        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)).ToString("yyyy-MM-dd");
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var endpoint = $"/api/stocks/announcements/{symbol}?startDate={startDate}&endDate={endDate}";

        var result = await GetJsonAsync<SarmaayaEnvelope<List<SarmaayaAnnouncementEntry>>>(
            endpoint, JsonOptions, ct);

        if (result?.Data?.Response is not { Count: > 0 })
            return announcements;

        AddRaw(rawResponses, endpoint, result);

        foreach (var a in result.Data.Response)
        {
            announcements.Add(new ScrapedAnnouncement
            {
                Title = a.Title,
                PostingDate = ParseDate(a.PostingDate),
                Id = a.Id
            });
        }

        Logger.LogDebug("Fetched {Count} announcements for {Symbol}", announcements.Count, symbol);
        return announcements;
    }

    // ─── Bulk / Market-Level (not per-stock) ─────────────────

    /// <summary>
    /// Fetches ALL KSE100 stock prices in a single call. Use for bulk price updates.
    /// </summary>
    public async Task<List<ScrapedBulkPrice>> FetchBulkTickerAsync(
        string index = "KSE100", CancellationToken ct = default)
    {
        var prices = new List<ScrapedBulkPrice>();

        var result = await GetJsonAsync<SarmaayaEnvelope<List<SarmaayaTickerEntry>>>(
            $"/api/stocks/ticker?index={index}", JsonOptions, ct);

        if (result?.Data?.Response is not { Count: > 0 })
            return prices;

        foreach (var t in result.Data.Response)
        {
            if (t.Symbol is null) continue;
            prices.Add(new ScrapedBulkPrice
            {
                Symbol = t.Symbol,
                Price = t.Price,
                Volume = t.Volume,
                ChangePct = t.ChangePercentage,
                Date = ParseDate(t.Date)
            });
        }

        Logger.LogInformation("Fetched {Count} bulk prices for index {Index}", prices.Count, index);
        return prices;
    }

    /// <summary>
    /// Fetches market index levels (KSE100, KSE30, KMI30).
    /// </summary>
    public async Task<List<ScrapedMarketIndex>> FetchMarketViewAsync(CancellationToken ct = default)
    {
        var indices = new List<ScrapedMarketIndex>();

        var result = await GetJsonAsync<SarmaayaEnvelope<List<SarmaayaMarketIndex>>>(
            "/api/dashboard/market-view", JsonOptions, ct);

        if (result?.Data?.Response is not { Count: > 0 })
            return indices;

        foreach (var idx in result.Data.Response)
        {
            if (idx.Symbol is null || idx.Name is null) continue;
            indices.Add(new ScrapedMarketIndex
            {
                Symbol = idx.Symbol,
                Name = idx.Name,
                Close = idx.Close,
                Change = idx.Change,
                ChangePct = idx.ChangePercentage
            });
        }

        Logger.LogInformation("Fetched {Count} market indices", indices.Count);
        return indices;
    }

    // ─── Helpers ──────────────────────────────────────────────

    private void AddRaw<T>(List<RawResponseEntry> list, string endpoint, ApiResult<T>? result)
    {
        if (result?.RawJson is null) return;
        list.Add(new RawResponseEntry
        {
            Endpoint = $"{SarmaayaApiBaseUrl}{endpoint}",
            Body = result.RawJson,
            HttpStatus = result.StatusCode
        });
    }

    private static string NormalizeKey(string displayName) =>
        displayName.ToLowerInvariant()
            .Replace(" - ", "_")
            .Replace(" & ", "_and_")
            .Replace("&", "_and_")
            .Replace(" ", "_")
            .Replace("/", "_")
            .Replace("'", "")
            .Replace("\u2019", "") // right single quotation mark (Shareholders' Equity)
            .Replace("(", "")
            .Replace(")", "")
            .Replace("%", "pct")
            .Replace("-", "_")     // "Short-Term" → "short_term"
            .Replace("--", "_")
            .TrimEnd('_');

    private static decimal? ParseFundamentalValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number)
            return value.GetDecimal();
        if (value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(value.GetString(), out var parsed))
            return parsed;
        return null;
    }

    private static DateOnly? ParseDate(string? dateStr)
    {
        if (dateStr is null) return null;
        // API returns ISO 8601: "2026-03-15T22:17:12.333Z"
        if (DateTime.TryParse(dateStr, out var dt))
            return DateOnly.FromDateTime(dt);
        return null;
    }
}
