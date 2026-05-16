namespace RetireRight.Core.Interfaces;

public interface IExporter
{
    string Format { get; }
    Task ExportAsync(ExportData data, string outputPath, CancellationToken ct = default);
}

public record ExportData
{
    public required MarketSnapshot Market { get; init; }
    public required IReadOnlyList<StockSnapshot> Stocks { get; init; }
    public required IReadOnlyList<DividendSnapshot> Dividends { get; init; }
    public required IReadOnlyList<AlertSnapshot> Alerts { get; init; }
    public DataFreshness Freshness { get; init; } = new();
}

public record MarketSnapshot
{
    public decimal? Kse100Level { get; init; }
    public decimal? Kse100ChangePct { get; init; }
    public decimal? PkrUsd { get; init; }
    public decimal? BrentCrude { get; init; }
    public decimal? SbpRate { get; init; }
    public decimal? Cpi { get; init; }
    public DateTime AsOf { get; init; }
}

public record StockSnapshot
{
    public required string Symbol { get; init; }
    public required string Name { get; init; }
    public string? Sector { get; init; }
    public decimal? Price { get; init; }
    public decimal? ChangePct { get; init; }
    public decimal? Volume { get; init; }
    public Dictionary<string, decimal?> Metrics { get; init; } = new();
    public decimal? CompositeScore { get; init; }
    public string? Signal { get; init; }

    // Opportunity detection
    public decimal? OpportunityScore { get; init; }
    public List<string> OpportunityFlags { get; init; } = [];
}

public record DividendSnapshot
{
    public required string Symbol { get; init; }
    public DateOnly ExDate { get; init; }
    public decimal Dps { get; init; }
    public DateOnly? PaymentDate { get; init; }
    public DateOnly LastBuyDate { get; init; }
    public int DaysUntilEx { get; init; }
}

public record AlertSnapshot
{
    public required string Type { get; init; }
    public required string Message { get; init; }
    public required string Severity { get; init; }
    public string? Symbol { get; init; }
}

public record DataFreshness
{
    public DateTime? Prices { get; init; }
    public DateTime? Fundamentals { get; init; }
    public DateTime? Dividends { get; init; }
    public DateTime? Macro { get; init; }
    public List<string> StaleWarnings { get; init; } = [];
}
