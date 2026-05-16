using RetireRight.Core.Models;

namespace RetireRight.Core.Interfaces;

public interface IPortfolioRepository
{
    Task<IReadOnlyList<PortfolioHolding>> GetHoldingsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PortfolioTransaction>> GetTransactionsAsync(int? stockId = null, CancellationToken ct = default);
    Task<PortfolioHolding> UpsertHoldingAsync(PortfolioHolding holding, CancellationToken ct = default);
    Task<PortfolioTransaction> AddTransactionAsync(PortfolioTransaction tx, CancellationToken ct = default);
    Task<bool> DeactivateHoldingAsync(int holdingId, CancellationToken ct = default);
}
