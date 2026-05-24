using RetireRight.Core.Enums;

namespace RetireRight.Core.Interfaces;

public interface IScraper : IAsyncDisposable
{
    /// <summary>Type identifier — matches <c>data_source_configs.type</c> (e.g. "Sarmaaya").</summary>
    string Source { get; }

    /// <summary>True for per-stock scrapers, false for universe/macro.</summary>
    bool IsPerSymbol { get; }

    ScrapingMethod Method { get; }
    Task<ScrapeResult> ScrapeAsync(ScrapeRequest request, CancellationToken ct = default);
    Task<bool> HealthCheckAsync(CancellationToken ct = default);
}

public record ScrapeRequest
{
    public string[] Symbols { get; init; } = [];
    public DataType[] DataTypes { get; init; } = [];
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public Guid? ScrapeRunId { get; init; }
}

public record ScrapeResult
{
    public string Source { get; init; } = "";
    public bool Success { get; init; }
    public int RecordsScraped { get; init; }
    public int RecordsFailed { get; init; }
    public TimeSpan Duration { get; init; }
    public List<string> Errors { get; init; } = [];

    /// <summary>
    /// Optional follow-up scrape request the orchestrator should enqueue after this one.
    /// Example: PsxUniverseScraper discovers stocks with future announced ex-dates that
    /// aren't in the watchlist and asks for a per-stock StockAnalysisDividends pass on
    /// just those symbols. Null source = no follow-up.
    /// </summary>
    public string? FollowUpSource { get; init; }
    public string[] FollowUpSymbols { get; init; } = [];
}
