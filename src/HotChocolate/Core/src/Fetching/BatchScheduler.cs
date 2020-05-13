using System.Security.Cryptography;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.Fetching
{
    public class BatchScheduler
        : IBatchScheduler
        , IBatchDispatcher
    {
        private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        public bool HasTasks => _queue.Count > 0;

        public event EventHandler? TaskEnqueued;

        public void Dispatch()
        {
            while(_queue.TryDequeue(out Action? dispatch))
            {
                dispatch();
            }
        }

        public void Schedule(Action dispatch)
        {
            _queue.Enqueue(dispatch);
            TaskEnqueued?.Invoke(this, EventArgs.Empty);
        }
    }
}
