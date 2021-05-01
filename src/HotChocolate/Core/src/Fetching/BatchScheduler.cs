using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution;

namespace HotChocolate.Fetching
{
    public class BatchScheduler
        : IBatchScheduler
        , IBatchDispatcher
    {
        private readonly ConcurrentQueue<Func<ValueTask>> _queue = new();

        public bool HasTasks => _queue.Count > 0;

        public event EventHandler? TaskEnqueued;

        public void Dispatch(Action<IExecutionTaskDefinition> enqueue)
        {
            lock (_queue)
            {
                if (_queue.Count > 0)
                {
                    var tasks = new List<Func<ValueTask>>();

                    while (_queue.TryDequeue(out Func<ValueTask>? dispatch))
                    {
                        tasks.Add(dispatch);
                    }

                    enqueue(new BatchExecutionTaskDefinition(tasks));
                }
            }
        }

        public void Schedule(Func<ValueTask> dispatch)
        {
            _queue.Enqueue(dispatch);
            TaskEnqueued?.Invoke(this, EventArgs.Empty);
        }

        private class BatchExecutionTaskDefinition
            : IExecutionTaskDefinition
        {
            private readonly IReadOnlyList<Func<ValueTask>> _tasks;

            public BatchExecutionTaskDefinition(IReadOnlyList<Func<ValueTask>> tasks)
            {
                _tasks = tasks;
            }

            public IExecutionTask Create(IExecutionTaskContext context)
            {
                return new BatchExecutionTask(context, _tasks);
            }
        }

        private class BatchExecutionTask : IExecutionTask
        {
            private readonly IExecutionTaskContext _context;
            private readonly IReadOnlyList<Func<ValueTask>> _tasks;

            public BatchExecutionTask(
                IExecutionTaskContext context,
                IReadOnlyList<Func<ValueTask>> tasks)
            {
                _context = context;
                _tasks = tasks;
            }

            public bool IsCompleted { get; private set; }

            public bool IsCanceled { get; private set; }

            public void BeginExecute(CancellationToken cancellationToken)
            {
                _context.Started();
                ExecuteAsync(cancellationToken);
            }

            public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
            {
                // _context.Started();

                try
                {
                    using (_context.Track(this))
                    {
                        foreach (Func<ValueTask> task in _tasks)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            try
                            {
                                await task.Invoke().ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _context.ReportError(this, ex);
                            }
                        }
                    }
                }
                finally
                {
                    IsCanceled = cancellationToken.IsCancellationRequested;
                    IsCompleted = true;
                    _context.Completed();
                }
            }
        }
    }
}
