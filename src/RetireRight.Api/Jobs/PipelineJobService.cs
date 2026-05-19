using System.Threading.Channels;
using RetireRight.Api.Services;
using RetireRight.Core.DTOs;

namespace RetireRight.Api.Jobs;

public sealed record PipelineJob(string JobType, string[]? Symbols = null, string Mode = "live", string? Source = null);

/// <summary>
/// Background service that processes pipeline jobs (scrape, compute, export) sequentially.
/// Jobs are enqueued via TriggerAsync and executed one at a time.
/// </summary>
public sealed class PipelineJobService : BackgroundService
{
    private readonly Channel<PipelineJob> _queue =
        Channel.CreateBounded<PipelineJob>(new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PipelineEventBroadcaster _broadcaster;
    private readonly ILogger<PipelineJobService> _logger;

    public PipelineJobService(
        IServiceScopeFactory scopeFactory,
        PipelineEventBroadcaster broadcaster,
        ILogger<PipelineJobService> logger)
    {
        _scopeFactory = scopeFactory;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    public int QueueDepth => _queue.Reader.Count;

    public async Task<bool> TriggerAsync(PipelineJob job, CancellationToken ct = default)
    {
        if (!_queue.Writer.TryWrite(job))
        {
            _broadcaster.Publish(new PipelineEventDto("QUEUE_FULL", job.JobType,
                null, "Job queue is full. Try again later.", null, null, DateTime.UtcNow));
            return false;
        }

        _broadcaster.UpdateQueueDepth(_queue.Reader.Count);
        _broadcaster.Publish(new PipelineEventDto("JOB_QUEUED", job.JobType,
            null, $"{job.JobType} job queued.", null, null, DateTime.UtcNow));
        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            _broadcaster.UpdateQueueDepth(_queue.Reader.Count);
            try
            {
                await ExecuteJobAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "Pipeline job {JobType} failed: {Inner}", job.JobType, msg);
                _broadcaster.Publish(new PipelineEventDto("JOB_FAILED", job.JobType,
                    null, msg, null, null, DateTime.UtcNow));
                _broadcaster.UpdateActiveJob(null);
            }
        }
    }

    private async Task ExecuteJobAsync(PipelineJob job, CancellationToken ct)
    {
        _logger.LogInformation("Starting pipeline job: {JobType}", job.JobType);

        _broadcaster.UpdateActiveJob(new PipelineJobStatusDto(
            job.JobType, "running", null, 0, 0, DateTime.UtcNow));
        _broadcaster.Publish(new PipelineEventDto($"{job.JobType.ToUpper()}_STARTED",
            job.JobType, null, $"{job.JobType} started.", 0, 0, DateTime.UtcNow));

        // AsyncServiceScope ensures scrapers implementing only IAsyncDisposable
        // (e.g. MacroScraper backed by Playwright) are disposed via DisposeAsync.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<PipelineOrchestrator>();

        switch (job.JobType.ToLowerInvariant())
        {
            case "scrape":
                await orchestrator.ScrapeAsync(job.Symbols, job.Mode, job.Source,
                    (source, scraped, total) =>
                    {
                        _broadcaster.UpdateActiveJob(new PipelineJobStatusDto(
                            "scrape", "running", source, scraped, total, DateTime.UtcNow));
                        _broadcaster.Publish(new PipelineEventDto("SCRAPE_PROGRESS", "scrape",
                            source, $"{source}: {scraped}/{total} records.", scraped, total, DateTime.UtcNow));
                    }, ct);
                _broadcaster.Publish(new PipelineEventDto("SCRAPE_COMPLETE", "scrape",
                    null, "Scrape complete.", null, null, DateTime.UtcNow));
                break;

            case "compute":
                var (computed, failed) = await orchestrator.ComputeAsync(job.Symbols,
                    (symbol, done, total) =>
                    {
                        _broadcaster.UpdateActiveJob(new PipelineJobStatusDto(
                            "compute", "running", symbol, done, total, DateTime.UtcNow));
                        _broadcaster.Publish(new PipelineEventDto("COMPUTE_PROGRESS", "compute",
                            symbol, $"Computed {symbol} ({done}/{total}).", done, total, DateTime.UtcNow));
                    }, ct);
                _broadcaster.Publish(new PipelineEventDto("COMPUTE_COMPLETE", "compute",
                    null, $"Compute complete: {computed} succeeded, {failed} failed.", computed, computed + failed, DateTime.UtcNow));
                break;

            case "export":
                await orchestrator.ExportAsync(
                    (done, total) =>
                    {
                        _broadcaster.Publish(new PipelineEventDto("EXPORT_PROGRESS", "export",
                            null, $"Export {done}/{total}.", done, total, DateTime.UtcNow));
                    }, ct);
                _broadcaster.Publish(new PipelineEventDto("EXPORT_COMPLETE", "export",
                    null, "Export complete.", null, null, DateTime.UtcNow));
                break;

            default:
                throw new InvalidOperationException($"Unknown job type: {job.JobType}");
        }

        _broadcaster.UpdateActiveJob(null);
    }
}
