using System.Text.Json;
using Microsoft.Extensions.Options;
using RetireRight.Api.Jobs;
using RetireRight.Api.Services;
using RetireRight.Core.Configuration;

namespace RetireRight.Api.Endpoints;

public static class ExportEndpoints
{
    public static void MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/export").WithTags("Export");

        group.MapGet("/latest", GetLatestExportAsync)
            .WithName("GetLatestExport");

        group.MapPost("/run", TriggerExportRunAsync)
            .WithName("TriggerExportRun");
    }

    private static async Task<IResult> GetLatestExportAsync(
        IOptions<RetireRightOptions> options,
        PipelineOrchestrator orchestrator,
        CancellationToken ct)
    {
        var latestJson = Path.Combine(options.Value.Export.OutputPath, "latest.json");

        if (File.Exists(latestJson))
        {
            var content = await File.ReadAllTextAsync(latestJson, ct);
            return Results.Content(content, "application/json");
        }

        // No export file yet — build from DB on the fly
        var data = await orchestrator.BuildExportDataAsync(ct);
        return Results.Ok(data);
    }

    private static async Task<IResult> TriggerExportRunAsync(
        PipelineJobService jobService,
        CancellationToken ct)
    {
        var queued = await jobService.TriggerAsync(new PipelineJob("export"), ct);
        return queued ? Results.Accepted() : Results.StatusCode(429);
    }
}
