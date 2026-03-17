namespace RetireRight.Core.Models;

public class InsiderTransaction
{
    public long Id { get; set; }
    public int StockId { get; set; }
    public required string PersonName { get; set; }
    public string? Position { get; set; }
    public required string Action { get; set; }
    public long? Quantity { get; set; }
    public decimal? Rate { get; set; }
    public DateOnly TransactionDate { get; set; }
    public required string Source { get; set; }

    public Guid? ScrapeRunId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Stock? Stock { get; set; }
    public ScrapeRun? ScrapeRun { get; set; }
}
