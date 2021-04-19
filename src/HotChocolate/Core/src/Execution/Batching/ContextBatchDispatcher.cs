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
        private readonly ConcurrentDictionary<IExecutionContext, CancellationToken> _contexts = new();
        private int _suspended = 0;
        private Task _dispatchTask = default!;

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
                TryStartDispatch();
            }
        }

        public void Register(IExecutionContext context, CancellationToken ctx)
        {
            if (!_contexts.TryAdd(context, ctx))
            {
                throw new ArgumentException("context is already registered", nameof(context));
            }
        }

        public void Unregister(IExecutionContext context)
        {
            if (!_contexts.TryRemove(context, out CancellationToken dummy))
            {
                throw new ArgumentException("context is not registered", nameof(context));
            }
        }

        private void BatchDispatcherEventHandler(object? source, EventArgs args)
        {
            lock (_dispatchLock)
            {
                TryStartDispatch();
            }
        }

        /// <remarks>Assumes _dispatchLock has been acquired</remarks>
        private void TryStartDispatch()
        {
            if (_suspended == 0 &&
                _dispatcher.HasTasks &&
                _dispatchTask is null &&
                RunningContexts().Any())
            {
                _dispatchTask = Task.Factory.StartNew(Dispatch, default, TaskCreationOptions.None, _batchScheduler);
            }
        }

        private async Task Dispatch()
        {
            await BatchTimeout();

            var context = AcquireContext();
            if (context != null)
            {
                try
                {
                    _dispatcher.Dispatch(taskDefinition =>
                    {
                        context.TaskBacklog.Register(taskDefinition.Create(context.TaskContext));
                    });
                }
                finally
                {
                    context.TaskStats.ResumeCompletionEvent();
                }
            }
            // if general logging is ever implement,
            // log a warning here is RunningContexts().Any() is true

            lock (_dispatchLock)
            {
                _dispatchTask = default!;
                TryStartDispatch();
            }
        }

        /// <summary>Wait till the batch should actually be started</summary>
        private async ValueTask BatchTimeout()
        {
            // TODO: add configurable timeout as well to account for sync implementations
            //       (postponed till after all tests pass because that could hide problems with the tests)
            var runningContexts = RunningContexts().ToList();
            while (runningContexts.Any() && (!_taskScheduler.IsIdle || runningContexts.Any(x => !x.Key.TaskBacklog.IsIdle)))
            {
                using (var ctxSource = CancellationTokenSource.CreateLinkedTokenSource(runningContexts.Select(x => x.Value).ToArray()))
                {
                    try
                    {
                        await _taskScheduler.WaitTillIdle(ctxSource.Token).ConfigureAwait(false);
                        foreach (var taskBacklog in runningContexts.Select(x => x.Key.TaskBacklog))
                        {
                            await taskBacklog.WaitTillIdle(ctxSource.Token).ConfigureAwait(false);
                        }
                    }
                    catch(TaskCanceledException)
                    {
                        // keep running as long as there are running contexts
                    }
                    catch(Exception)
                    {
                        // if an unexpected exception happened while waiting,
                        // just start the batch and see what happens there
                        // (if general logging is ever implemented log a warning here).
                        return;
                    }
                }
                runningContexts = RunningContexts().ToList();
            }
        }

        /// <summary>
        /// Locks one the the running execution contexts for completion and returns it.
        /// Returns null if there are no remaining uncompleted execution contexts.
        /// </summary>
        private IExecutionContext AcquireContext()
        {
            IExecutionContext acquiredContext = default!;
            foreach (var context in RunningContexts().Select(x => x.Key))
            {
                try
                {
                    var taskStats = context.TaskStats;
                    taskStats.SuspendCompletionEvent();
                    if (!taskStats.IsCompleted)
                    {
                        acquiredContext = context;
                        break;
                    }
                    else
                    {
                        taskStats.ResumeCompletionEvent();
                    }
                }
                catch
                {
                    // consider the context as unavailable if anything went
                    // wrong while acquiring it
                }
            }
            return acquiredContext;
        }

        private IEnumerable<KeyValuePair<IExecutionContext, CancellationToken>> RunningContexts()
        {
            return _contexts.Where(x => !(x.Value.IsCancellationRequested || x.Key.TaskStats.IsCompleted));
        }
    }
}

