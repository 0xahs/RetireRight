namespace RetireRight.Core.Models;

public class MacroIndicator
{
    public long Id { get; set; }
    public required string IndicatorName { get; set; }
    public DateOnly Date { get; set; }
    public decimal Value { get; set; }
    public string? Unit { get; set; }
    public required string Source { get; set; }
    public Guid? ScrapeRunId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ScrapeRun? ScrapeRun { get; set; }
}
