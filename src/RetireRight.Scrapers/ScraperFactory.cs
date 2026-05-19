using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RetireRight.Core.Interfaces;
using RetireRight.Scrapers.AntiBot;
using RetireRight.Scrapers.Sources;

namespace RetireRight.Scrapers;

public class ScraperFactory(IScraperConfigRepository configRepo, IServiceProvider sp)
{
    // Per-stock scrapers (exclude universe/macro which don't operate per-symbol)
    private static readonly HashSet<string> UniverseAndMacroTypes =
        new(StringComparer.OrdinalIgnoreCase) { "PsxUniverse", "TradingEconomics", "Macro" };

    public async Task<List<IScraper>> CreateEnabledAsync(string? sourceFilter = null, CancellationToken ct = default)
    {
        var configs = sourceFilter is null
            ? await configRepo.GetEnabledAsync(ct)
            : sourceFilter.Equals("stock", StringComparison.OrdinalIgnoreCase)
                // "stock" = all per-stock scrapers, skip universe/macro
                ? (await configRepo.GetEnabledAsync(ct))
                    .Where(c => !UniverseAndMacroTypes.Contains(c.Type))
                    .ToList()
                : (await configRepo.GetAllAsync(ct))
                    .Where(c => c.Name.Equals(sourceFilter, StringComparison.OrdinalIgnoreCase)
                             || c.Type.Equals(sourceFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();

        var scrapers = new List<IScraper>();
        foreach (var config in configs)
        {
            var scraper = Create(config.ToOptions());
            if (scraper is not null) scrapers.Add(scraper);
        }
        return scrapers;
    }

    private IScraper? Create(Core.Configuration.DataSourceOptions opts)
    {
        var lf = sp.GetRequiredService<ILoggerFactory>();
        var throttler = sp.GetRequiredService<RequestThrottler>();
        var retry = sp.GetRequiredService<RetryPolicy>();

        return opts.Type.ToLowerInvariant() switch
        {
            "sarmaaya" => new SarmaayaScraper(
                lf.CreateLogger<SarmaayaScraper>(), lf, throttler, retry, opts,
                sp.GetRequiredService<IStockRepository>(),
                sp.GetRequiredService<IPriceRepository>(),
                sp.GetRequiredService<IRawResponseRepository>(),
                sp.GetRequiredService<IFundamentalsRepository>(),
                sp.GetRequiredService<IOwnershipRepository>(),
                sp.GetRequiredService<IInsiderTransactionRepository>(),
                sp.GetRequiredService<IIndexLevelRepository>()),

            "stockanalysis" => new StockAnalysisScraper(
                lf.CreateLogger<StockAnalysisScraper>(), throttler, retry, opts,
                sp.GetRequiredService<IStockRepository>(),
                sp.GetRequiredService<IFundamentalsRepository>()),

            "psxterminal" => new PsxTerminalScraper(
                lf.CreateLogger<PsxTerminalScraper>(), throttler, retry, opts,
                sp.GetRequiredService<IStockRepository>(),
                sp.GetRequiredService<IDividendRepository>()),

            "psxuniverse" => new PsxUniverseScraper(
                lf.CreateLogger<PsxUniverseScraper>(), throttler, retry, opts,
                sp.GetRequiredService<IStockRepository>(),
                sp.GetRequiredService<IFundamentalsRepository>(),
                sp.GetRequiredService<IPriceRepository>()),

            "tradingeconomics" or "macro" => new MacroScraper(
                lf.CreateLogger<MacroScraper>(), throttler, retry, opts,
                sp.GetRequiredService<IMacroRepository>()),

            _ => null
        };
    }
}
