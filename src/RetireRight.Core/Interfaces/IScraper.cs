using RetireRight.Core.Enums;

namespace RetireRight.Core.Interfaces;

public interface IScraper : IAsyncDisposable
{
    DataSourceType Source { get; }
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
    public DataSourceType Source { get; init; }
    public bool Success { get; init; }
    public int RecordsScraped { get; init; }
    public int RecordsFailed { get; init; }
    public TimeSpan Duration { get; init; }
    public List<string> Errors { get; init; } = [];
}
