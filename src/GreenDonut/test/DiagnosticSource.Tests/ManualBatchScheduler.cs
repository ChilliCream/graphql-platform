using System;
using System.Collections.Concurrent;

namespace GreenDonut
{
    public class ManualBatchScheduler
        : IBatchScheduler
    {
        private readonly object _sync = new object();
        private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        public void Dispatch()
        {
            lock(_sync)
            {
                while (_queue.TryDequeue(out Action dispatch))
                {
                    dispatch();
                }
            }
        }

        public void Schedule(Action dispatch)
        {
            _queue.Enqueue(dispatch);
        }
    }
}
