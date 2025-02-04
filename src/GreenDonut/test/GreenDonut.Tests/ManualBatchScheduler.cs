using System.Collections.Concurrent;

namespace GreenDonut;

public class ManualBatchScheduler : IBatchScheduler
{
    private readonly ConcurrentQueue<Func<ValueTask>> _queue = new();

    public void Dispatch()
    {
        while (_queue.TryDequeue(out var dispatch))
        {
            Task.Run(async () => await dispatch());
        }
    }

    public void Schedule(Func<ValueTask> dispatch)
    {
        _queue.Enqueue(dispatch);
    }
}
