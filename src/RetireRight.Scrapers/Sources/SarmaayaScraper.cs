using Microsoft.Extensions.Logging;
using RetireRight.Core.Attributes;
using RetireRight.Core.Configuration;
using RetireRight.Core.Enums;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Scrapers.AntiBot;
using RetireRight.Scrapers.Base;
using RetireRight.Scrapers.Clients;
using RetireRight.Scrapers.Clients.Sarmaaya;
using RetireRight.Scrapers.Models;

namespace RetireRight.Scrapers.Sources;

/// <summary>
/// Sarmaaya scraper — thin orchestrator that:
///   1. Builds a <see cref="FallbackChain"/> from independent data clients
///   2. Fetches data per stock (API first → browser fallback)
///   3. Persists results to repositories
///
/// The actual data fetching is done by <see cref="SarmaayaApi"/> and
/// <see cref="SarmaayaBrowser"/> — standalone, reusable clients.
/// </summary>
[DataSource("Sarmaaya", PerSymbol = true)]
public class SarmaayaScraper : BaseScraper
{
    private readonly IStockRepository _stockRepo;
    private readonly IPriceRepository _priceRepo;
    private readonly IRawResponseRepository _rawRepo;
    private readonly IFundamentalsRepository _fundamentalsRepo;
    private readonly IOwnershipRepository _ownershipRepo;
    private readonly IInsiderTransactionRepository _insiderRepo;
    private readonly IIndexLevelRepository _indexLevelRepo;
    private readonly ILoggerFactory _loggerFactory;
    private FallbackChain? _chain;
    private Guid? _currentScrapeRunId;

    public override ScrapingMethod Method => ScrapingMethod.Api;

    public SarmaayaScraper(
        ILogger<SarmaayaScraper> logger,
        ILoggerFactory loggerFactory,
        RequestThrottler throttler,
        RetryPolicy retryPolicy,
        DataSourceOptions sourceConfig,
        IStockRepository stockRepo,
        IPriceRepository priceRepo,
        IRawResponseRepository rawRepo,
        IFundamentalsRepository fundamentalsRepo,
        IOwnershipRepository ownershipRepo,
        IInsiderTransactionRepository insiderRepo,
        IIndexLevelRepository indexLevelRepo)
        : base(logger, throttler, retryPolicy, sourceConfig)
    {
        _loggerFactory = loggerFactory;
        _stockRepo = stockRepo;
        _priceRepo = priceRepo;
        _rawRepo = rawRepo;
        _fundamentalsRepo = fundamentalsRepo;
        _ownershipRepo = ownershipRepo;
        _insiderRepo = insiderRepo;
        _indexLevelRepo = indexLevelRepo;
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

        // Authenticate — get JWT for locked financial endpoints
        var token = await SarmaayaAuth.GetTokenAsync(SourceConfig.Credentials, Logger, ct);

        // Build fallback chain: API (fast, priority 1) → Browser (slow, priority 10)
        var apiClient = new SarmaayaApi(
            _loggerFactory.CreateLogger<SarmaayaApi>(),
            RetryPolicy, SourceConfig, token?.AccessToken);

        _chain = new FallbackChain(
        [
            apiClient,
            new SarmaayaBrowser(
                _loggerFactory.CreateLogger<SarmaayaBrowser>(),
                SourceConfig)
        ], Logger);

        // Fetch market indices before per-stock loop
        try
        {
            var indices = await apiClient.FetchMarketViewAsync(ct);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            foreach (var idx in indices)
            {
                await _indexLevelRepo.UpsertAsync(new IndexLevel
                {
                    IndexName = idx.Symbol ?? idx.Name ?? "UNKNOWN",
                    Date = today,
                    Close = idx.Close,
                    ChangePct = idx.ChangePct,
                    Source = "Sarmaaya:API",
                    ScrapeRunId = _currentScrapeRunId
                }, ct);
            }
            if (indices.Count > 0)
                Logger.LogInformation("Persisted {Count} market indices", indices.Count);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to fetch market indices — continuing with per-stock scrape");
        }

        try
        {
            foreach (var symbol in symbols)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await ThrottleAsync(ct);

                    var data = await _chain.FetchAsync(symbol, ct);

                    if (data is not null)
                    {
                        await PersistAsync(data, ct);
                        scraped++;

                        Logger.LogInformation(
                            "[{Source}] {Symbol}: Rs. {Price} ({Change:+0.00;-0.00}%) | " +
                            "{FundCount} fundamentals | {HistCount} history | " +
                            "{OwnCount} owners | {InsCount} insiders | {AnnCount} announcements",
                            data.Source, symbol, data.Close, data.ChangePct ?? 0,
                            data.Fundamentals.Count, data.PriceHistory.Count,
                            data.Ownership.Count, data.InsiderTransactions.Count, data.Announcements.Count);
                    }
                    else
                    {
                        errors.Add($"{symbol}: all fetch strategies failed");
                        failed++;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to scrape {Symbol} from Sarmaaya", symbol);
                    errors.Add($"{symbol}: {ex.Message}");
                    failed++;
                }
            }
        }
        finally
        {
            await _chain.DisposeAllAsync();
        }

        return (scraped, failed, errors);
    }

    // ─── Persistence ──────────────────────────────────────────

    private async Task PersistAsync(ScrapedStockData data, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Ensure stock exists — fix sector and ISIN on every scrape
        var stock = await _stockRepo.GetBySymbolAsync(data.Symbol, ct);
        if (stock is null)
        {
            stock = await _stockRepo.UpsertAsync(new Stock
            {
                Symbol = data.Symbol,
                Name = data.Name ?? data.Symbol,
                Industry = data.IndustryName,
                Isin = data.Isin,
                IsWatchlist = true,
                ScrapeRunId = _currentScrapeRunId
            }, ct);
        }
        else
        {
            // Update mutable fields — sector is owned by PsxUniverseScraper, don't overwrite
            stock.Industry = data.IndustryName ?? stock.Industry;
            stock.Isin = data.Isin ?? stock.Isin;
            stock.Name = data.Name ?? stock.Name;
            stock.ScrapeRunId = _currentScrapeRunId;
            stock = await _stockRepo.UpsertAsync(stock, ct);
        }

        // Save price
        if (data.Close is not null)
        {
            await _priceRepo.UpsertAsync(new DailyPrice
            {
                StockId = stock.Id,
                Date = data.PriceDate ?? today,
                Close = data.Close.Value,
                Open = data.Open,
                High = data.High,
                Low = data.Low,
                Volume = data.Volume,
                ChangePct = data.ChangePct,
                Source = data.Source,
                ScrapeRunId = _currentScrapeRunId
            }, ct);
        }


        // Save price history (backfill)
        foreach (var entry in data.PriceHistory)
        {
            await _priceRepo.UpsertAsync(new DailyPrice
            {
                StockId = stock.Id,
                Date = entry.Date,
                Close = entry.Price,
                Volume = entry.Volume,
                Source = $"{data.Source}:History",
                ScrapeRunId = _currentScrapeRunId
            }, ct);
        }

        // Save fundamentals — split ANN vs LTM by key prefix
        if (data.Fundamentals.Count > 0)
        {
            var annKeys = data.Fundamentals
                .Where(kv => !kv.Key.StartsWith("ltm_", StringComparison.OrdinalIgnoreCase)
                          && !kv.Key.StartsWith("industry_", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            var ltmKeys = data.Fundamentals
                .Where(kv => kv.Key.StartsWith("ltm_", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            if (annKeys.Count > 0)
            {
                var annStandard = SarmaayaKeyMapper.MapToStandard(annKeys);
                // Also extract industry averages into the ANN record
                var allFundamentals = data.Fundamentals.ToDictionary(kv => kv.Key, kv => kv.Value);
                var industryAvgs = SarmaayaKeyMapper.ExtractIndustryAverages(allFundamentals);
                foreach (var (key, value) in industryAvgs)
                    annStandard[key] = value;

                // Fold returns into ANN fundamentals (e.g. return_1w, return_1m, etc.)
                foreach (var (period, val) in data.Returns)
                    annStandard[$"return_{period}"] = val;

                // Store announcement count as metadata
                if (data.Announcements.Count > 0)
                    annStandard["announcement_count_6m"] = data.Announcements.Count;

                await _fundamentalsRepo.UpsertAsync(new StockFundamental
                {
                    StockId = stock.Id,
                    ScrapedDate = today,
                    Periodicity = "ANN",
                    Source = data.Source,
                    Data = annStandard,
                    ScrapeRunId = _currentScrapeRunId
                }, ct);
            }

            if (ltmKeys.Count > 0)
            {
                var ltmStandard = SarmaayaKeyMapper.MapToStandard(ltmKeys);
                await _fundamentalsRepo.UpsertAsync(new StockFundamental
                {
                    StockId = stock.Id,
                    ScrapedDate = today,
                    Periodicity = "LTM",
                    Source = data.Source,
                    Data = ltmStandard,
                    ScrapeRunId = _currentScrapeRunId
                }, ct);
            }
        }

        // Save ownership
        if (data.Ownership.Count > 0)
        {
            await _ownershipRepo.UpsertAsync(new StockOwnership
            {
                StockId = stock.Id,
                AsOfDate = today,
                Source = data.Source,
                Holders = data.Ownership.Select(o => new OwnershipEntry
                {
                    HolderName = o.HolderName,
                    HolderType = o.HolderType,
                    InvestorType = o.InvestorType,
                    Shares = o.Shares,
                    WeightPct = o.WeightPct
                }).ToList(),
                ScrapeRunId = _currentScrapeRunId
            }, ct);
        }

        // Save insider transactions
        foreach (var trade in data.InsiderTransactions)
        {
            if (trade.Name is null || trade.Action is null || trade.Date is null) continue;
            await _insiderRepo.UpsertAsync(new InsiderTransaction
            {
                StockId = stock.Id,
                PersonName = trade.Name,
                Position = trade.Position,
                Action = trade.Action,
                Quantity = trade.Quantity,
                Rate = trade.Rate,
                TransactionDate = trade.Date.Value,
                Source = data.Source,
                ScrapeRunId = _currentScrapeRunId
            }, ct);
        }

        // Save raw responses
        foreach (var raw in data.RawResponses)
        {
            await _rawRepo.SaveAsync(new RawApiResponse
            {
                Source = Source,
                Endpoint = raw.Endpoint,
                Symbol = data.Symbol,
                ResponseBody = raw.Body,
                ContentType = raw.ContentType,
                HttpStatus = raw.HttpStatus
            }, ct);
        }
    }

    public override async Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        await using var apiClient = new SarmaayaApi(
            _loggerFactory.CreateLogger<SarmaayaApi>(),
            RetryPolicy, SourceConfig, accessToken: null);

        var result = await apiClient.FetchAsync("MCB", ct);
        return result is not null;
    }

    public override async ValueTask DisposeAsync()
    {
        if (_chain is not null)
            await _chain.DisposeAllAsync();
    }
}
