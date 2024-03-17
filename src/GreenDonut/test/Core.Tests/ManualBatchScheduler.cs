using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GreenDonut;

public class ManualBatchScheduler : IBatchScheduler
{
    private readonly ConcurrentQueue<BatchJob> _queue = new();

    public void Dispatch()
    {
        while (_queue.TryDequeue(out var job))
        {
            Task.Run(async () => await job.DispatchAsync());
        }
    }

    public void Schedule(BatchJob job)
        => _queue.Enqueue(job);
}