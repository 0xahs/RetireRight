namespace RetireRight.Core.DTOs;

public record PipelineStatusDto(
    SourceStatusDto[] Sources,
    PipelineJobStatusDto? ActiveJob,
    DateTime? LastComputedAt,
    DateTime? LastExportedAt,
    MissingMetricDto[] TopMissingMetrics,
    int QueueDepth);

public record SourceStatusDto(
    string Name,
    string Status,
    DateTime? LastRunAt,
    int? StocksScraped,
    int? StocksFailed,
    TimeSpan? Duration);

public record ScrapeRunDto(
    Guid Id,
    string Source,
    string Mode,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int SymbolCount,
    int RecordsScraped,
    int RecordsFailed);

public record MissingMetricDto(string MetricKey, int AffectedStockCount, string Reason);

public record PipelineJobStatusDto(
    string JobType,
    string Status,
    string? CurrentSymbol,
    int Progress,
    int Total,
    DateTime StartedAt);

public record PipelineEventDto(
    string EventType,
    string? JobType,
    string? Symbol,
    string? Message,
    int? Progress,
    int? Total,
    DateTime Timestamp);

public record ScheduleConfigDto(
    string DailyRunTime,
    bool Enabled,
    string[] Jobs);
