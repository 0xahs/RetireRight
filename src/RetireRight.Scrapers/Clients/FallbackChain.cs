using Microsoft.Extensions.Logging;
using RetireRight.Scrapers.Models;

namespace RetireRight.Scrapers.Clients;

/// <summary>
/// Tries an ordered list of <see cref="IStockDataClient"/> for each stock.
/// Returns the first successful result; logs and skips failures.
/// Generic — works for any source (Sarmaaya, PsxOfficial, KseStocks, etc.).
/// </summary>
public class FallbackChain
{
    private readonly IReadOnlyList<IStockDataClient> _clients;
    private readonly ILogger _logger;

    public FallbackChain(IEnumerable<IStockDataClient> clients, ILogger logger)
    {
        _clients = clients.OrderBy(c => c.Priority).ToList();
        _logger = logger;
    }

    /// <summary>
    /// Tries each client in priority order until one succeeds.
    /// Returns null only if ALL clients fail.
    /// </summary>
    public async Task<ScrapedStockData?> FetchAsync(string symbol, CancellationToken ct)
    {
        foreach (var client in _clients)
        {
            try
            {
                var result = await client.FetchAsync(symbol, ct);
                if (result is not null)
                {
                    _logger.LogDebug("Fetched {Symbol} via {Client}", symbol, client.Name);
                    return result;
                }

                _logger.LogDebug("{Client} returned no data for {Symbol}", client.Name, symbol);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Client} failed for {Symbol}, trying next fallback",
                    client.Name, symbol);
            }
        }

        _logger.LogWarning("All {Count} clients failed for {Symbol}",
            _clients.Count, symbol);
        return null;
    }

    /// <summary>Dispose all clients in the chain.</summary>
    public async ValueTask DisposeAllAsync()
    {
        foreach (var client in _clients)
            await client.DisposeAsync();
    }
}
