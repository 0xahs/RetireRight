namespace RetireRight.Core.Models;

public class DailyPrice
{
    public long Id { get; set; }
    public int StockId { get; set; }
    public DateOnly Date { get; set; }
    public decimal? Open { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal Close { get; set; }
    public long? Volume { get; set; }
    public int? Trades { get; set; }
    public decimal? Value { get; set; }
    public decimal? ChangePct { get; set; }
    public required string Source { get; set; }
    public Guid? ScrapeRunId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Stock? Stock { get; set; }
    public ScrapeRun? ScrapeRun { get; set; }
}
