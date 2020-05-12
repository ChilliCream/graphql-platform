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
        private readonly object _lock = new object();
        private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
        private bool _isDispatching;

        public bool HasTasks => !_isDispatching && _queue.Count > 0;

        public event EventHandler? TaskEnqueued;

        public Task DispatchAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested && !_isDispatching)
            {
                try
                {
                    Monitor.Enter(_lock, ref _isDispatching);

                    while(!cancellationToken.IsCancellationRequested &&
                        _queue.TryDequeue(out var dispatch))
                    {
                        dispatch();
                    }
                }
                finally
                {
                    if (_isDispatching)
                    {
                        Monitor.Exit(_lock);
                    }
                }
            }

            return Task.CompletedTask;
        }

        public void Schedule(Action dispatch)
        {
            _queue.Enqueue(dispatch);
            RaiseTaskEnqueued();
        }

        private void RaiseTaskEnqueued()
        {
            if (TaskEnqueued != null)
            {
                TaskEnqueued(this, EventArgs.Empty);
            }
        }
    }
}
