namespace RetireRight.Core.Models;

public class Dividend
{
    public long Id { get; set; }
    public int StockId { get; set; }
    public DateOnly ExDate { get; set; }
    public DateOnly? RecordDate { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public decimal Dps { get; set; }
    public decimal? Percentage { get; set; }
    public string? Year { get; set; }
    public string DividendType { get; set; } = "regular";
    public string? Frequency { get; set; }
    public required string Source { get; set; }
    public Guid? ScrapeRunId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Stock? Stock { get; set; }
    public ScrapeRun? ScrapeRun { get; set; }
}
