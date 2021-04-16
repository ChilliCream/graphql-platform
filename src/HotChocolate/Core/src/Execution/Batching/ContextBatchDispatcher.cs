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
    internal class ContextBatchDispatcher
    : IContextBatchDispatcher
    {
        private readonly IBatchDispatcher _dispatcher;
        private readonly ConcurrentDictionary<IExecutionContext, bool> _contexts = new();
        private int _suspended = 0;

        public ContextBatchDispatcher(IBatchDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _dispatcher.TaskEnqueued += BatchDispatcherEventHandler;
        }

        public IBatchDispatcher BatchDispatcher => _dispatcher;

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

        private void BatchDispatcherEventHandler(
            object? source, EventArgs args) =>
            TryDispatch();

        private void TaskStatisticsEventHandler(
            object? source, EventArgs args) =>
            TryDispatch();

        private void TryDispatch()
        {
            if (_suspended == 0 && _contexts.All(x => x.Key.TaskBacklog.IsEmpty ) && _dispatcher.HasTasks)
            {
                Dispatch();
            }
        }

        private void Dispatch()
        {
            IExecutionContext safeContext = default!;
            Exception safeContextException = default!;
            foreach (var context in _contexts.Keys)
            {
                try
                {
                    var taskStats = context.TaskStats;
                    taskStats.SuspendCompletionEvent();
                    if (!taskStats.IsCompleted)
                    {
                        safeContext = context;
                        break;
                    }
                    else
                    {
                        taskStats.ResumeCompletionEvent();
                    }
                }
                catch (Exception e)
                {
                    safeContextException = e;
                }
            }
            if (safeContext is null)
            {
                throw new InvalidOperationException("Batch is scheduled but there are no remaining pending contexts", safeContextException);
            }
            try
            {
                _dispatcher.Dispatch(taskDefinition =>
                {
                    safeContext.TaskBacklog.Register(taskDefinition.Create(new ContextDispatchBlocker(safeContext.TaskContext, this)));
                });
            }
            finally
            {
                safeContext.TaskStats.ResumeCompletionEvent();
            }
        }
    }
}
