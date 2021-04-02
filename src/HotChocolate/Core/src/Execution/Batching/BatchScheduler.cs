using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;

namespace HotChocolate.Execution.Batching
{
    internal class BatchScheduler
        : IBatchScheduler
        , IBatchDispatcher
    {
        private readonly ConcurrentQueue<Func<ValueTask>> _scheduled = new();
        private readonly ConcurrentDictionary<IExecutionContext, bool> _contexts = new();
        private int _suspended = 0;

        public void Schedule(Func<ValueTask> dispatch)
        {
            _scheduled.Enqueue(dispatch);
            TryDispatch();
        }

        public void Suspend()
        {
            Interlocked.Increment(ref _suspended);
        }

        public void Resume()
        {
            var suspended = Interlocked.Decrement(ref _suspended);
            if (suspended == 0)
            {
                TryDispatch();
            }
        }

        public void Register(IExecutionContext context)
        {
            if (!_contexts.TryAdd(context, true))
            {
                throw new ArgumentException("context is already registered", nameof(context));
            }
            context.TaskStats.StateChanged += TaskStatisticsEventHandler;
        }

        public void Unregister(IExecutionContext context)
        {
            if (!_contexts.TryRemove(context, out bool dummy))
            {
                throw new ArgumentException("context is not registered", nameof(context));
            }
            context.TaskStats.StateChanged -= TaskStatisticsEventHandler;
        }

        private void TaskStatisticsEventHandler(
            object? source, EventArgs args) =>
            TryDispatch();

        private void TryDispatch()
        {
            if (_suspended == 0 && _contexts.All(x => x.Key.TaskBacklog.IsEmpty) && _scheduled.Count > 0)
            {
                Dispatch();
            }
        }

        private void Dispatch()
        {
            lock (_scheduled)
            {
                if (_scheduled.Count > 0)
                {
                    var context = _contexts.Select(x => x.Key).Where(x => !x.IsCompleted).FirstOrDefault();
                    if (context == null)
                    {
                        throw new Exception("Batch is scheduled but there are no remaining pending contexts");
                    }

                    var tasks = new List<Func<ValueTask>>();

                    while (_scheduled.TryDequeue(out Func<ValueTask>? dispatch))
                    {
                        tasks.Add(dispatch);
                    }

                    var taskDefinition = new BatchExecutionTaskDefinition(tasks);
                    context?.TaskBacklog.Register(taskDefinition.Create(context.TaskContext));
                }
            }
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

        private class BatchExecutionTask
            : IExecutionTask
        {
            private readonly IExecutionTaskContext _context;
            private readonly IReadOnlyList<Func<ValueTask>> _tasks;
            private ValueTask _task;

            public BatchExecutionTask(
                IExecutionTaskContext context,
                IReadOnlyList<Func<ValueTask>> tasks)
            {
                _context = context;
                _tasks = tasks;
            }

            public bool IsCompleted => _task.IsCompleted;

            public bool IsCanceled { get; private set; }

            public void BeginExecute(CancellationToken cancellationToken)
            {
                _context.Started();
                _task = ExecuteAsync(cancellationToken);
            }

            private async ValueTask ExecuteAsync(CancellationToken cancellationToken)
            {
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
                    _context.Completed();
                }
            }
        }
    }
}
