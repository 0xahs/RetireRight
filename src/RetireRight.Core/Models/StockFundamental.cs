namespace RetireRight.Core.Models;

/// <summary>
/// Stores fundamental financial data (income statement, balance sheet, ratios)
/// as a JSONB dictionary keyed by <see cref="Constants.FundamentalKeys"/>.
/// One row per stock per scrape date per periodicity (ANN / LTM).
/// </summary>
public class StockFundamental
{
    public long Id { get; set; }
    public int StockId { get; set; }
    public DateOnly ScrapedDate { get; set; }

    /// <summary>"ANN" (annual) or "LTM" (trailing 12 months).</summary>
    public required string Periodicity { get; set; }

    public required string Source { get; set; }

    /// <summary>Standard-keyed financial data. See <see cref="Constants.FundamentalKeys"/>.</summary>
    public Dictionary<string, decimal?> Data { get; set; } = new();

    public Guid? ScrapeRunId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Stock? Stock { get; set; }
    public ScrapeRun? ScrapeRun { get; set; }
}
