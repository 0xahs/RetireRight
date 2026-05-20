using Microsoft.Extensions.DependencyInjection;
using RetireRight.Core.Attributes;
using RetireRight.Core.Configuration;
using RetireRight.Core.Interfaces;

namespace RetireRight.Scrapers;

/// <summary>
/// Builds scrapers from <see cref="DataSourceConfig"/> rows.
/// Scraper classes are discovered by scanning this assembly for <see cref="DataSourceAttribute"/>,
/// so adding a new scraper requires only the attribute — no edits to the factory.
/// </summary>
public class ScraperFactory(IScraperConfigRepository configRepo, IServiceProvider sp)
{
    private sealed record Registration(Type ClrType, bool PerSymbol);

    // Reflection-built map: config.type (case-insensitive) -> scraper class + per-symbol flag.
    private static readonly IReadOnlyDictionary<string, Registration> ByIdentifier = BuildRegistry();

    private static Dictionary<string, Registration> BuildRegistry()
    {
        var entries = typeof(ScraperFactory).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(IScraper).IsAssignableFrom(t))
            .Select(t => (Type: t, Attr: t.GetCustomAttributes(typeof(DataSourceAttribute), false)
                                          .Cast<DataSourceAttribute>()
                                          .FirstOrDefault()))
            .Where(x => x.Attr is not null);

        var map = new Dictionary<string, Registration>(StringComparer.OrdinalIgnoreCase);
        foreach (var (type, attr) in entries)
        {
            if (map.ContainsKey(attr!.Identifier))
                throw new InvalidOperationException(
                    $"Duplicate [DataSource(\"{attr.Identifier}\")] — {type.Name} collides with {map[attr.Identifier].ClrType.Name}.");
            map[attr.Identifier] = new Registration(type, attr.PerSymbol);
        }
        return map;
    }

    public async Task<List<IScraper>> CreateEnabledAsync(string? sourceFilter = null, CancellationToken ct = default)
    {
        var configs = sourceFilter is null
            ? await configRepo.GetEnabledAsync(ct)
            : sourceFilter.Equals("stock", StringComparison.OrdinalIgnoreCase)
                // "stock" = all per-stock scrapers, skip whole-universe / macro scrapers.
                ? (await configRepo.GetEnabledAsync(ct))
                    .Where(c => ByIdentifier.TryGetValue(c.Type, out var r) && r.PerSymbol)
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

    private IScraper? Create(DataSourceOptions opts)
    {
        if (!ByIdentifier.TryGetValue(opts.Type, out var reg)) return null;
        // ActivatorUtilities resolves all other constructor args from DI; opts is passed as extra.
        return (IScraper)ActivatorUtilities.CreateInstance(sp, reg.ClrType, opts);
    }
}
