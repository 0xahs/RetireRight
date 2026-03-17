using RetireRight.Core.Enums;

namespace RetireRight.Core.Models;

public class ComputedMetrics
{
    public long Id { get; set; }
    public int StockId { get; set; }
    public DateOnly ComputedDate { get; set; }
    public required Dictionary<string, decimal?> Metrics { get; set; }
    public decimal? CompositeScore { get; set; }
    public decimal? DividendSafetyScore { get; set; }
    public decimal? DividendGrowthScore { get; set; }
    public decimal? ValuationScore { get; set; }
    public SignalType? Signal { get; set; }
    public Guid? ScrapeRunId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Stock? Stock { get; set; }
    public ScrapeRun? ScrapeRun { get; set; }
}
