using RetireRight.Scrapers.Models;

namespace RetireRight.Scrapers.Clients;

/// <summary>
/// A single data-fetching strategy for a stock (e.g., REST API, headless browser, HTML parser).
/// Implementations are pure data fetchers — they return data and know nothing about persistence.
/// </summary>
public interface IStockDataClient : IAsyncDisposable
{
    /// <summary>Display name for logging, e.g. "Sarmaaya:API", "PsxOfficial:Browser".</summary>
    string Name { get; }

    /// <summary>Lower = tried first in a fallback chain.</summary>
    int Priority { get; }

    /// <summary>
    /// Fetch all available data for a single stock symbol.
    /// Returns null if this client cannot provide data (source down, blocked, etc.).
    /// </summary>
    Task<ScrapedStockData?> FetchAsync(string symbol, CancellationToken ct);
}
