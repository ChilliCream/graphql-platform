using System.Collections.Concurrent;

namespace GreenDonut;

public class ManualBatchScheduler : IBatchScheduler
{
    private readonly ConcurrentQueue<Batch> _queue = new();

    public void Schedule(Batch batch)
    {
        _queue.Enqueue(batch);
    }

    public void Dispatch()
    {
        while (_queue.TryDequeue(out var batch))
        {
            Task.Run(batch.DispatchAsync);
        }
    }
}
