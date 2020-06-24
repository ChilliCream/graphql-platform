using System;
using System.Collections.Concurrent;
using GreenDonut;

namespace HotChocolate.Fetching
{
    /// <summary>
    /// Represents batch dispatcher that needs to be triggered in order to dispatch all batches
    /// enqueued so far.
    /// </summary>
    public class BatchScheduler
        : IBatchScheduler
        , IBatchDispatcher
    {
        private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        /// <inheritdoc/>
        public bool HasTasks => _queue.Count > 0;

        /// <inheritdoc/>
        public event EventHandler? TaskEnqueued;

        /// <inheritdoc/>
        public void Dispatch()
        {
            while (_queue.TryDequeue(out Action? dispatch))
            {
                dispatch();
            }
        }

        /// <inheritdoc/>
        public void Schedule(Action dispatch)
        {
            _queue.Enqueue(dispatch);
            TaskEnqueued?.Invoke(this, EventArgs.Empty);
        }
    }
}
