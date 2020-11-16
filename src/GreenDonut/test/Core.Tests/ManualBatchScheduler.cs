using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GreenDonut
{
    public class ManualBatchScheduler
        : IBatchScheduler
    {
        private readonly object _sync = new object();
        private readonly ConcurrentQueue<Func<ValueTask>> _queue =
            new ConcurrentQueue<Func<ValueTask>>();

        public void Dispatch()
        {
            lock(_sync)
            {
                while (_queue.TryDequeue(out Func<ValueTask> dispatch))
                {
                    dispatch();
                }
            }
        }

        public void Schedule(Func<ValueTask> dispatch)
        {
            _queue.Enqueue(dispatch);
        }
    }
}
