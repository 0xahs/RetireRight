using RetireRight.Core.Models;

namespace RetireRight.Core.Interfaces;

public interface IStockRepository
{
    Task<Stock?> GetBySymbolAsync(string symbol, CancellationToken ct = default);
    Task<IReadOnlyList<Stock>> GetWatchlistAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Stock>> GetAllActiveAsync(CancellationToken ct = default);
    Task<Stock> UpsertAsync(Stock stock, CancellationToken ct = default);
    Task<bool> SetWatchlistAsync(string symbol, bool isWatchlist, CancellationToken ct = default);
}

public interface IPriceRepository
{
    Task<DailyPrice?> GetLatestAsync(int stockId, CancellationToken ct = default);
    Task<IReadOnlyList<DailyPrice>> GetRangeAsync(int stockId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task UpsertAsync(DailyPrice price, CancellationToken ct = default);
    Task UpsertBatchAsync(IEnumerable<DailyPrice> prices, CancellationToken ct = default);
}

public interface IDividendRepository
{
    Task<IReadOnlyList<Dividend>> GetUpcomingAsync(int days = 30, CancellationToken ct = default);
    Task<IReadOnlyList<Dividend>> GetHistoryAsync(int stockId, CancellationToken ct = default);
    Task UpsertAsync(Dividend dividend, CancellationToken ct = default);
}

public interface IMacroRepository
{
    Task<MacroIndicator?> GetLatestAsync(string indicatorName, CancellationToken ct = default);
    Task UpsertAsync(MacroIndicator indicator, CancellationToken ct = default);
}

public interface IRawResponseRepository
{
    Task SaveAsync(RawApiResponse response, CancellationToken ct = default);
}
