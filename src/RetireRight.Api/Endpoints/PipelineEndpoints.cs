using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RetireRight.Api.Jobs;
using RetireRight.Core.Configuration;
using RetireRight.Core.DTOs;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Api.Endpoints;

public static class PipelineEndpoints
{
    public static void MapPipelineEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pipeline").WithTags("Pipeline");

        group.MapGet("/status", GetStatusAsync).WithName("GetPipelineStatus");
        group.MapGet("/runs", GetRunsAsync).WithName("GetScrapeRuns");
        group.MapPost("/scrape", TriggerScrapeAsync).WithName("TriggerScrape");
        group.MapPost("/compute", TriggerComputeAsync).WithName("TriggerCompute");
        group.MapPost("/export", TriggerExportAsync).WithName("TriggerExport");
        group.MapGet("/events", GetEventsAsync).WithName("PipelineEvents").ExcludeFromDescription();

        var settingsGroup = app.MapGroup("/api/settings").WithTags("Settings");
        settingsGroup.MapGet("/schedule", GetScheduleAsync).WithName("GetSchedule");
        settingsGroup.MapPut("/schedule", UpdateScheduleAsync).WithName("UpdateSchedule");
    }

    private static readonly string[] AllSources = ["Sarmaaya", "StockAnalysis", "TradingEconomics"];
    private static string FriendlySourceName(string source) =>
        source == "TradingEconomics" ? "Macro" : source;

    private static async Task<IResult> GetStatusAsync(
        PipelineJobService jobService,
        PipelineEventBroadcaster broadcaster,
        RetireRightDbContext db,
        CancellationToken ct)
    {
        var recentRuns = await db.ScrapeRuns
            .OrderByDescending(r => r.StartedAt)
            .Take(30)
            .ToListAsync(ct);

        var runsBySource = recentRuns.GroupBy(r => r.Source).ToDictionary(g => g.Key, g => g.First());

        var sourceGroups = AllSources.Select(source =>
        {
            if (runsBySource.TryGetValue(source, out var latest))
            {
                var duration = latest.CompletedAt.HasValue
                    ? latest.CompletedAt.Value - latest.StartedAt
                    : (TimeSpan?)null;
                return new SourceStatusDto(
                    Name: FriendlySourceName(source),
                    Status: latest.Status,
                    LastRunAt: latest.StartedAt,
                    StocksScraped: latest.RecordsScraped,
                    StocksFailed: latest.RecordsFailed,
                    Duration: duration);
            }
            return new SourceStatusDto(
                Name: FriendlySourceName(source),
                Status: "never",
                LastRunAt: null,
                StocksScraped: null,
                StocksFailed: null,
                Duration: null);
        }).ToArray();

        var lastComputed = await db.ComputedMetrics
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => (DateTime?)c.UpdatedAt)
            .FirstOrDefaultAsync(ct);

        return Results.Ok(new PipelineStatusDto(
            Sources: sourceGroups,
            ActiveJob: broadcaster.ActiveJob,
            LastComputedAt: lastComputed,
            LastExportedAt: null,
            TopMissingMetrics: [],
            QueueDepth: jobService.QueueDepth));
    }

    private static async Task<IResult> GetRunsAsync(
        RetireRightDbContext db,
        CancellationToken ct,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await db.ScrapeRuns.CountAsync(ct);
        var runs = await db.ScrapeRuns
            .OrderByDescending(r => r.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ScrapeRunDto(
                r.Id, r.Source, r.Mode, r.Status,
                r.StartedAt, r.CompletedAt, r.SymbolCount,
                r.RecordsScraped, r.RecordsFailed))
            .ToListAsync(ct);

        return Results.Ok(new PagedResponse<ScrapeRunDto>(
            runs.ToArray(), total, page, pageSize));
    }

    private static async Task<IResult> TriggerScrapeAsync(
        TriggerJobRequest? request,
        PipelineJobService jobService,
        CancellationToken ct)
    {
        var job = new PipelineJob("scrape", request?.Symbols, request?.Mode ?? "live", request?.Source);
        var queued = await jobService.TriggerAsync(job, ct);
        return queued ? Results.Accepted() : Results.StatusCode(429);
    }

    private static async Task<IResult> TriggerComputeAsync(
        TriggerJobRequest? request,
        PipelineJobService jobService,
        CancellationToken ct)
    {
        var job = new PipelineJob("compute", request?.Symbols);
        var queued = await jobService.TriggerAsync(job, ct);
        return queued ? Results.Accepted() : Results.StatusCode(429);
    }

    private static async Task<IResult> TriggerExportAsync(
        PipelineJobService jobService,
        CancellationToken ct)
    {
        var job = new PipelineJob("export");
        var queued = await jobService.TriggerAsync(job, ct);
        return queued ? Results.Accepted() : Results.StatusCode(429);
    }

    private static async Task GetEventsAsync(
        HttpContext context,
        PipelineEventBroadcaster broadcaster,
        CancellationToken ct)
    {
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("X-Accel-Buffering", "no");

        // Send recent events as catch-up
        foreach (var evt in broadcaster.GetRecentEvents(20))
        {
            await WriteEventAsync(context.Response, evt, ct);
        }

        // Stream live events
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var evt = await broadcaster.WaitForNextEventAsync(ct);
                await WriteEventAsync(context.Response, evt, ct);
                await context.Response.Body.FlushAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static readonly System.Text.Json.JsonSerializerOptions _camelCase = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    };

    private static Task WriteEventAsync(HttpResponse response, PipelineEventDto evt, CancellationToken ct)
    {
        var data = System.Text.Json.JsonSerializer.Serialize(evt, _camelCase);
        var text = $"event: {evt.EventType}\ndata: {data}\n\n";
        return response.WriteAsync(text, ct);
    }

    private static IResult GetScheduleAsync(IOptions<RetireRightOptions> options)
    {
        var s = options.Value.Scheduler;
        return Results.Ok(new ScheduleConfigDto(s.DailyRunTime, s.Enabled, s.Jobs.ToArray()));
    }

    private static IResult UpdateScheduleAsync(
        ScheduleConfigDto config,
        IOptions<RetireRightOptions> options)
    {
        // In-memory update (persists until restart; Phase 3 will write to DB)
        var s = options.Value.Scheduler;
        s.DailyRunTime = config.DailyRunTime;
        s.Enabled = config.Enabled;
        s.Jobs = [.. config.Jobs];
        return Results.Ok(config);
    }
}

public record TriggerJobRequest(string[]? Symbols = null, string Mode = "live", string? Source = null);
