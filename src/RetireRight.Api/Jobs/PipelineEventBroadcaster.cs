using System.Collections.Concurrent;
using RetireRight.Core.DTOs;

namespace RetireRight.Api.Jobs;

/// <summary>
/// Singleton that holds the current pipeline state and broadcasts events to SSE listeners.
/// </summary>
public sealed class PipelineEventBroadcaster
{
    private readonly ConcurrentQueue<PipelineEventDto> _events = new();
    private readonly List<TaskCompletionSource<PipelineEventDto>> _waiters = [];
    private readonly Lock _lock = new();

    public PipelineJobStatusDto? ActiveJob { get; private set; }
    public int QueueDepth { get; private set; }

    public void UpdateActiveJob(PipelineJobStatusDto? job)
    {
        ActiveJob = job;
    }

    public void UpdateQueueDepth(int depth)
    {
        QueueDepth = depth;
    }

    public void Publish(PipelineEventDto evt)
    {
        _events.Enqueue(evt);
        if (_events.Count > 500)
            _events.TryDequeue(out _);

        lock (_lock)
        {
            foreach (var waiter in _waiters)
                waiter.TrySetResult(evt);
            _waiters.Clear();
        }
    }

    public IReadOnlyList<PipelineEventDto> GetRecentEvents(int count = 50)
    {
        return _events.TakeLast(count).ToList();
    }

    public Task<PipelineEventDto> WaitForNextEventAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<PipelineEventDto>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        ct.Register(() => tcs.TrySetCanceled(ct));

        lock (_lock)
            _waiters.Add(tcs);

        return tcs.Task;
    }
}
