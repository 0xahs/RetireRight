using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RetireRight.Core.Configuration;
using RetireRight.Scrapers.Models;

namespace RetireRight.Scrapers.Clients.Sarmaaya;

/// <summary>
/// Sarmaaya Playwright browser client — navigates sarmaaya.pk, intercepts SPA's internal
/// API calls, clicks tabs to trigger lazy-loaded data, falls back to page text parsing.
/// Extends generic <see cref="BrowserDataClient"/> which provides all stealth infrastructure.
///
/// Data extraction tiers (per stock):
///   Tier 1: Intercept the XHR calls the SPA makes to beta-restapi (structured JSON)
///   Tier 2: Parse price from rendered page text (last resort)
/// </summary>
public class SarmaayaBrowser : BrowserDataClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private IBrowserContext? _context;

    public override string Name => "Sarmaaya:Browser";
    public override int Priority => 10; // Slow — used as fallback

    public SarmaayaBrowser(
        ILogger<SarmaayaBrowser> logger,
        DataSourceOptions sourceConfig)
        : base(logger, sourceConfig)
    {
    }

    public override async Task<ScrapedStockData?> FetchAsync(string symbol, CancellationToken ct)
    {
        _context ??= await CreateContextAsync();

        var url = $"{SourceConfig.BaseUrl}/stocks/{symbol}";
        var page = await _context.NewPageAsync();

        try
        {
            // Register interceptors BEFORE navigation — captures all API calls the SPA makes
            var intercepted = InterceptApiCalls(page, "beta-restapi.sarmaaya.pk/api");

            var (_, response) = await NavigateWithPage(page, url);

            if (response is null || !response.Ok)
            {
                Logger.LogWarning("Failed to load {Url}: {Status}", url, response?.Status);
                return null;
            }

            // Click tabs to trigger lazy-loaded API calls
            await TryClickTabAsync(page, "Dividends");
            await TryClickTabAsync(page, "Ownership");
            await TryClickTabAsync(page, "Insider");
            await TryClickTabAsync(page, "Announcements");

            // Give SPA a moment to fire lazy API calls
            await Task.Delay(2000, ct);

            // Save raw HTML
            var rawResponses = new List<RawResponseEntry>();
            var html = await GetPageHtmlAsync(page);
            rawResponses.Add(new RawResponseEntry
            {
                Endpoint = url,
                Body = html,
                ContentType = "text/html",
                HttpStatus = response.Status
            });

            // Archive all intercepted API responses
            foreach (var (apiUrl, body) in intercepted)
            {
                rawResponses.Add(new RawResponseEntry
                {
                    Endpoint = apiUrl,
                    Body = body,
                    HttpStatus = 200
                });
            }

            // ── Tier 1: Parse intercepted API responses ──

            var stockData = FindIntercepted<SarmaayaEnvelope<SarmaayaStockResponse>>(
                intercepted, "/api/stocks/", excludePatterns: ["/dividends", "/price-history", "/returns", "/about", "/peers", "/fundamentals", "/insiders", "/announcements", "/reports", "/details", "/ticker", "/metrics", "/ownership"]);

            var aboutData = FindIntercepted<SarmaayaEnvelope<SarmaayaAboutResponse>>(
                intercepted, "/api/stocks/about");

            var ownershipData = FindIntercepted<SarmaayaEnvelope<List<SarmaayaOwnershipEntry>>>(
                intercepted, "/api/stocks/ownership/");

            var insiderData = FindIntercepted<SarmaayaEnvelope<List<SarmaayaInsiderEntry>>>(
                intercepted, "/api/stocks/stock-insiders/");

            var announcementData = FindIntercepted<SarmaayaEnvelope<List<SarmaayaAnnouncementEntry>>>(
                intercepted, "/api/stocks/announcements/");

            var returnsData = FindIntercepted<SarmaayaEnvelope<JsonElement>>(
                intercepted, "/api/stocks/returns/");

            if (stockData is { Success: true, Response: not null })
            {
                var price = stockData.Response;

                var ownership = new List<ScrapedOwnership>();
                if (ownershipData is { Success: true, Response.Count: > 0 })
                {
                    foreach (var h in ownershipData.Response)
                    {
                        ownership.Add(new ScrapedOwnership
                        {
                            HolderName = h.HolderName,
                            HolderType = h.HolderType,
                            InvestorType = h.InvestorType,
                            Shares = h.Shares,
                            WeightPct = h.WeightClose
                        });
                    }
                }

                var insiders = new List<ScrapedInsiderTrade>();
                if (insiderData is { Success: true, Response.Count: > 0 })
                {
                    foreach (var t in insiderData.Response)
                    {
                        insiders.Add(new ScrapedInsiderTrade
                        {
                            Name = t.Name,
                            Position = t.Position,
                            Action = t.Action,
                            Quantity = t.Quantity,
                            Rate = t.Rate,
                            Date = DateOnly.TryParse(t.Date, out var d) ? d : null
                        });
                    }
                }

                var announcements = new List<ScrapedAnnouncement>();
                if (announcementData is { Success: true, Response.Count: > 0 })
                {
                    foreach (var a in announcementData.Response)
                    {
                        announcements.Add(new ScrapedAnnouncement
                        {
                            Title = a.Title,
                            PostingDate = DateOnly.TryParse(a.PostingDate, out var d) ? d : null,
                            Id = a.Id
                        });
                    }
                }

                var returns = ExtractReturns(returnsData, symbol);

                Logger.LogInformation("Scraped {Symbol} via browser (API intercept): Rs. {Price}",
                    symbol, price.Close);

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
                    Name = price.Name,
                    Isin = aboutData?.Response?.Isin ?? price.Isin,
                    SectorName = price.SectorName,
                    IndustryName = aboutData?.Response?.Industry,
                    Ownership = ownership,
                    InsiderTransactions = insiders,
                    Announcements = announcements,
                    Returns = returns,
                    RawResponses = rawResponses
                };
            }

            // ── Tier 2: Parse page text (last resort) ──

            var parsedPrice = await ExtractPriceFromPageAsync(page, symbol);
            if (parsedPrice is null) return null;

            Logger.LogInformation("Scraped {Symbol} via browser (page parse): Rs. {Price}",
                symbol, parsedPrice.Value.price);

            return new ScrapedStockData
            {
                Symbol = symbol,
                Source = $"{Name}:Parse",
                Close = parsedPrice.Value.price,
                Name = parsedPrice.Value.name,
                RawResponses = rawResponses
            };
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ─── Helpers ──────────────────────────────────────────────

    private async Task<(IPage page, IResponse? response)> NavigateWithPage(IPage page, string url)
    {
        Logger.LogDebug("Browser: navigating to {Url}", url);

        var response = await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });

        await AntiBot.StealthConfigurator.SimulateHumanDelayAsync(1000, 3000);
        return (page, response);
    }

    private async Task TryClickTabAsync(IPage page, string tabText)
    {
        try
        {
            var tab = page.GetByRole(AriaRole.Link, new() { Name = tabText });
            if (await tab.IsVisibleAsync())
            {
                await tab.ClickAsync();
                await Task.Delay(1500);
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Could not click {Tab} tab", tabText);
        }
    }

    private static T? FindIntercepted<T>(
        List<(string url, string body)> intercepted,
        string urlContains,
        string[]? excludePatterns = null)
    {
        foreach (var (url, body) in intercepted)
        {
            if (!url.Contains(urlContains)) continue;
            if (excludePatterns?.Any(p => url.Contains(p)) == true) continue;

            try
            {
                return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch { /* wrong shape — keep searching */ }
        }

        return default;
    }

    private async Task<(decimal price, string? name)?> ExtractPriceFromPageAsync(IPage page, string symbol)
    {
        try
        {
            var data = await page.EvaluateAsync<JsonElement>("""
                () => {
                    const candidates = [...document.querySelectorAll('h1, h2, h3, .price, [class*="price"], [class*="Price"]')];
                    let priceText = '';
                    for (const el of candidates) {
                        const n = parseFloat(el.textContent.replace(/[,]/g, ''));
                        if (!isNaN(n) && n > 1 && n < 100000) {
                            priceText = el.textContent.trim();
                            break;
                        }
                    }
                    return {
                        title: document.title || '',
                        priceText: priceText,
                        bodySnippet: document.body ? document.body.innerText.substring(0, 3000) : ''
                    };
                }
                """);

            var title = data.GetProperty("title").GetString() ?? "";
            var priceText = data.GetProperty("priceText").GetString() ?? "";
            var bodySnippet = data.GetProperty("bodySnippet").GetString() ?? "";

            decimal? price = null;

            if (!string.IsNullOrEmpty(priceText))
            {
                var cleaned = priceText.Replace(",", "").Replace("PKR", "").Replace("Rs.", "").Trim();
                if (decimal.TryParse(cleaned, out var p) && p > 1)
                    price = p;
            }

            price ??= ExtractCurrencyTaggedPrice(bodySnippet);

            if (price is null)
            {
                Logger.LogWarning("Could not extract price for {Symbol} from browser page", symbol);
                return null;
            }

            string? name = null;
            if (title.Contains('-'))
                name = title.Split('-')[0].Trim();

            return (price.Value, name);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Browser price extraction failed for {Symbol}", symbol);
            return null;
        }
    }

    private static Dictionary<string, decimal?> ExtractReturns(
        SarmaayaEnvelope<JsonElement>? returnsData, string symbol)
    {
        var returns = new Dictionary<string, decimal?>();
        if (returnsData is not { Success: true } ||
            returnsData.Response.ValueKind != JsonValueKind.Array)
            return returns;

        foreach (var entry in returnsData.Response.EnumerateArray())
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

    private static decimal? ExtractCurrencyTaggedPrice(string text)
    {
        var match = Regex.Match(text, @"(?:Rs\.?|PKR)\s*([\d,]+\.?\d*)");
        if (match.Success)
        {
            var cleaned = match.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(cleaned, out var p) && p > 1 && p < 100000)
                return p;
        }
        return null;
    }

    public override async ValueTask DisposeAsync()
    {
        if (_context is not null)
            await _context.DisposeAsync();
        await base.DisposeAsync();
    }
}
