namespace RetireRight.Core.DTOs;

public record ApiResponse<T>(T Data, ApiMeta Meta);

public record ApiMeta(
    DateTime? ScrapedAt,
    DateTime? ComputedAt,
    string SourceConfidence,
    string[] MissingMetrics);

public record PagedResponse<T>(T[] Items, int Total, int Page, int PageSize);
