namespace RetireRight.Core.Models;

/// <summary>
/// Stores equity holder breakdown for a stock as JSONB.
/// One row per stock per date snapshot.
/// </summary>
public class StockOwnership
{
    public long Id { get; set; }
    public int StockId { get; set; }
    public DateOnly AsOfDate { get; set; }
    public required string Source { get; set; }

    /// <summary>JSONB array of holders with name, type, shares, weight.</summary>
    public List<OwnershipEntry> Holders { get; set; } = [];

    public Guid? ScrapeRunId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Stock? Stock { get; set; }
    public ScrapeRun? ScrapeRun { get; set; }
}

public class OwnershipEntry
{
    public string? HolderName { get; set; }
    public string? HolderType { get; set; }
    public string? InvestorType { get; set; }
    public long? Shares { get; set; }
    public decimal? WeightPct { get; set; }
}
