namespace RetireRight.Core.Models;

public class Stock
{
    public int Id { get; set; }
    public required string Symbol { get; set; }
    public required string Name { get; set; }
    public string Sector { get; set; } = "Other";
    public string? Industry { get; set; }
    public DateOnly? ListedDate { get; set; }
    public string? Isin { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsWatchlist { get; set; }
    public decimal? CircuitLimitPct { get; set; }
    public int LotSize { get; set; } = 1;
    public string? TradingBoard { get; set; }
    public Guid? ScrapeRunId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ScrapeRun? ScrapeRun { get; set; }
}
