namespace RetireRight.Core.Models;

public class IndexLevel
{
    public long Id { get; set; }
    public string IndexName { get; set; } = "KSE100";
    public DateOnly Date { get; set; }
    public decimal? Open { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal Close { get; set; }
    public long? Volume { get; set; }
    public decimal? ChangePct { get; set; }
    public required string Source { get; set; }
    public Guid? ScrapeRunId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ScrapeRun? ScrapeRun { get; set; }
}
