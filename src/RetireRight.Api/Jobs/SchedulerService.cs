using Microsoft.Extensions.Options;
using RetireRight.Core.Configuration;

namespace RetireRight.Api.Jobs;

/// <summary>
/// Checks the daily schedule and enqueues jobs automatically.
/// The schedule is configurable from appsettings (and later via Settings UI).
/// </summary>
public sealed class SchedulerService : BackgroundService
{
    private readonly PipelineJobService _jobService;
    private readonly IOptionsMonitor<RetireRightOptions> _options;
    private readonly ILogger<SchedulerService> _logger;

    private DateOnly _lastRunDate = DateOnly.MinValue;

    public SchedulerService(
        PipelineJobService jobService,
        IOptionsMonitor<RetireRightOptions> options,
        ILogger<SchedulerService> logger)
    {
        _jobService = jobService;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SchedulerService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndTriggerAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "SchedulerService error");
            }

            // Check every minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task CheckAndTriggerAsync(CancellationToken ct)
    {
        var opts = _options.CurrentValue.Scheduler;
        if (!opts.Enabled) return;

        if (!TimeOnly.TryParse(opts.DailyRunTime, out var scheduledTime))
        {
            _logger.LogWarning("Invalid DailyRunTime: {Time}", opts.DailyRunTime);
            return;
        }

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);

        if (currentTime < scheduledTime) return;
        if (_lastRunDate >= today) return;

        _logger.LogInformation("Scheduler triggering daily run at {Time}", now);
        _lastRunDate = today;

        foreach (var job in opts.Jobs)
        {
            await _jobService.TriggerAsync(new PipelineJob(job), ct);
        }
    }
}
