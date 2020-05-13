using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution
{
    internal sealed class CompletionQueue : ICompletionQueue
    {
        private readonly ConcurrentQueue<ResolverTask> _queue =
            new ConcurrentQueue<ResolverTask>();

        public event EventHandler? TaskEnqueued;

        public void Enqueue(ResolverTask task)
        {
            _queue.Enqueue(task);
            TaskEnqueued?.Invoke(this, EventArgs.Empty);
        }

        public bool TryDequeue([NotNullWhen(true)]out ResolverTask? task)
        {
            return _queue.TryDequeue(out task);
        }
    }
}