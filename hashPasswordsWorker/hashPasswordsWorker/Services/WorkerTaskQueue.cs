using System.Collections.Concurrent;
using Core.Models;

namespace Worker.Services;

public class WorkerTaskQueue
{
    private readonly ConcurrentQueue<CrackHashManagerRequest> _tasks = new();
    private readonly SemaphoreSlim _signal = new(0);

    public void Enqueue(CrackHashManagerRequest task)
    {
        _tasks.Enqueue(task);
        _signal.Release();
    }

    public async Task<CrackHashManagerRequest?> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        _tasks.TryDequeue(out var task);
        return task;
    }
}