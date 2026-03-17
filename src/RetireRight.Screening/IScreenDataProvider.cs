namespace RetireRight.Screening;

/// <summary>
/// Provides stock data for screening. Implemented by the Infrastructure layer
/// to decouple screening logic from database access.
/// </summary>
public interface IScreenDataProvider
{
    Task<IReadOnlyList<ScreenableStock>> GetScreenableStocksAsync(
        bool watchlistOnly = false,
        string[]? sectors = null,
        CancellationToken ct = default);
}

public class ScreenableStock
{
    public required string Symbol { get; init; }
    public required string Name { get; init; }
    public string? Sector { get; init; }
    public decimal? Price { get; init; }
    public decimal? CompositeScore { get; init; }
    public string? Signal { get; init; }
    public Dictionary<string, decimal?> Metrics { get; init; } = new();
}
