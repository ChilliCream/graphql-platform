using System;
using System.Threading;

namespace HotChocolate.Execution
{
    internal class TaskStatistics : ITaskStatistics
    {
        private int _running;
        private int _enqueued;

        public int Enqueued => _enqueued;

        public int Running => _running;

        public event EventHandler<EventArgs>? StateChanged;

        public void Clear()
        {
            _running = 0;
            _enqueued = 0;
        }

        public void TaskEnqueued()
        {
            Interlocked.Increment(ref _enqueued);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void TaskDequeued()
        {
            Interlocked.Decrement(ref _enqueued);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void TaskStarted()
        {
            Interlocked.Increment(ref _running);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void TaskCompleted()
        {
            Interlocked.Decrement(ref _running);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
