using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;

namespace HotChocolate.Execution.Batching
{
    internal sealed class ContextBatchDispatcher
    : IContextBatchDispatcher, IDisposable
    {
        private readonly object _dispatchLock = new();
        private readonly TrackableTaskScheduler _taskScheduler;
        private readonly TaskScheduler _batchScheduler;
        private readonly IBatchDispatcher _dispatcher;
        private readonly ConcurrentDictionary<IExecutionContext, bool> _contexts = new();
        private int _suspended = 0;

        public ContextBatchDispatcher(IBatchDispatcher dispatcher)
        {
            _batchScheduler = TaskScheduler.Current;
            Contract.Assert(!(_batchScheduler is TrackableTaskScheduler));
            _taskScheduler = new TrackableTaskScheduler(_batchScheduler);
            _dispatcher = dispatcher;
            _dispatcher.TaskEnqueued += BatchDispatcherEventHandler;
        }

        public void Dispose()
        {
            _taskScheduler.Complete();
        }

        public IBatchDispatcher BatchDispatcher => _dispatcher;

        public TaskScheduler TaskScheduler => _taskScheduler;

        public void Suspend()
        {
            lock(_dispatchLock)
            {
                ++_suspended;
            }
        }

        public void Resume()
        {
            lock (_dispatchLock)
            {
                --_suspended;
                StartDispatch();
            }
        }

        public void Register(IExecutionContext context)
        {
            if (!_contexts.TryAdd(context, true))
            {
                throw new ArgumentException("context is already registered", nameof(context));
            }
        }

        public void Unregister(IExecutionContext context)
        {
            if (!_contexts.TryRemove(context, out bool dummy))
            {
                throw new ArgumentException("context is not registered", nameof(context));
            }
        }

        private void BatchDispatcherEventHandler(object? source, EventArgs args)
        {
            lock (_dispatchLock)
            {
                StartDispatch();
            }
        }

        private void StartDispatch()
        {
            if (_suspended == 0 && _dispatcher.HasTasks)
            {
                Suspend();
                try
                {
                    Task.Factory.StartNew(Dispatch, default, TaskCreationOptions.None, _batchScheduler);
                }
                catch
                {
                    Resume();
                    throw;
                }
            }
        }

        private async Task Dispatch()
        {
            try
            {
                while (!_taskScheduler.IsEmpty || _contexts.Any(x => !(x.Key.TaskBacklog.IsEmpty)))
                {
                    await _taskScheduler.WaitTillEmpty().ConfigureAwait(false);
                    foreach (var taskBacklog in _contexts.Select(x => x.Key.TaskBacklog))
                    {
                        await taskBacklog.WaitTillEmpty().ConfigureAwait(false);
                    }
                }

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
                        safeContext.TaskBacklog.Register(taskDefinition.Create(safeContext.TaskContext));
                    });
                }
                finally
                {
                    safeContext.TaskStats.ResumeCompletionEvent();
                }
            }
            finally
            {
                Resume();
            }

            lock (_dispatchLock)
            {
                StartDispatch();
            }
        }
    }
}

