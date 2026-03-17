namespace RetireRight.Core.Models;

public class ScrapeRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public required string Source { get; set; }

    /// <summary>"test" or "live" — test data can be bulk-deleted.</summary>
    public required string Mode { get; set; }

    public int SymbolCount { get; set; }
    public int RecordsScraped { get; set; }
    public int RecordsFailed { get; set; }

    /// <summary>"running", "completed", or "failed".</summary>
    public string Status { get; set; } = "running";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
