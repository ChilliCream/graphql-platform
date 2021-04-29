using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;

namespace HotChocolate.Execution.Batching
{
    internal sealed class ContextBatchDispatcher
    : IContextBatchDispatcher, IDisposable
    {
        private readonly object _dispatchLock = new();
        private readonly TrackableTaskScheduler? _trackableScheduler;
        private readonly TaskScheduler _underlyingScheduler;
        private readonly IBatchDispatcher _dispatcher;
        private readonly ConcurrentDictionary<IExecutionContext, CancellationToken> _contexts = new();
        private int _suspended = 0;
        private Task _dispatchTask = default!;
        /// <summary>The amount of time in milliseconds that we wait before starting a batch</summary>
        /// <remarks>If nothing is in progress, the wait time will be less, if tasks are still being created it can be more</remarks>
        private readonly int _dispatchTimeout;

        public ContextBatchDispatcher(IBatchDispatcher dispatcher, IBatchingOptionsAccessor options)
        {
            _underlyingScheduler = TaskScheduler.Current;
            Contract.Assert(!(_underlyingScheduler is TrackableTaskScheduler));
            _dispatcher = dispatcher;
            _dispatcher.TaskEnqueued += BatchDispatcherEventHandler;
            _dispatchTimeout = (int) options.BatchTimeout.TotalMilliseconds;
            if (_dispatchTimeout > 0)
            {
                _trackableScheduler = new TrackableTaskScheduler(_underlyingScheduler);
            }
        }

        public void Dispose()
        {
            _trackableScheduler?.Complete();
        }

        public TaskScheduler TaskScheduler => _trackableScheduler ?? _underlyingScheduler;

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
            if (!_contexts.TryRemove(context, out _))
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
                _dispatchTask = Task.Factory.StartNew(Dispatch, default, TaskCreationOptions.None, _underlyingScheduler);
            }
        }

        private async Task Dispatch()
        {
            try
            {
                await DispatchCanStart();

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
            }
            finally
            {
                lock (_dispatchLock)
                {
                    _dispatchTask = default!;
                    TryStartDispatch();
                }
            }
        }

        /// <summary>Wait till the batch should actually be started</summary>
        private async ValueTask DispatchCanStart()
        {
            bool keepWaiting = true;
            while (keepWaiting)
            {
                var contexts = RunningContexts().ToList();
                if (contexts.Any())
                {
                    var contextCtx = CancellationTokenSource.CreateLinkedTokenSource(contexts.Select(x => x.Value).ToArray());
                    try
                    {
                        var checks = contexts.Select(x => x.Key.TaskBacklog.WaitTillIdle(contextCtx.Token)).ToList();
                        bool willYield = checks.Any(x => !x.IsCompleted);
                        await Task.WhenAll(checks).ConfigureAwait(false);

                        // if we triggered an actual context switch, restart the loop because new items might have been added since then
                        if (willYield) continue;

                        // all internal tasks are scheduled, await running task completion/idle time if necessary
                        keepWaiting = await TasksBlockDispatch(contextCtx.Token);
                    }
                    catch (Exception)
                    {
                        // keep trying if anything goes wrong till all contexts are aborted
                        // (example exceptions: TaskCanceledException if task aborted but not yet returned to pool,
                        //                      ObjectDisposedException if context has been returned to pool)
                        keepWaiting = true;
                    }
                }
                else
                {
                    keepWaiting = false;
                }
            }
        }

        /// <summary>Waits for the completion or idle time of all tasks in progress (if necessary)</summary>
        /// <param name="contextCtx">A cancellation token this is triggered when any of the current context's are aborted</param>
        /// <returns>true if DispatchCanStart cannot yet return and should restart the wait loop, false if it can return</returns>
        private async ValueTask<bool> TasksBlockDispatch(CancellationToken contextCtx)
        {
            if (!contextCtx.IsCancellationRequested && _trackableScheduler != null)
            {
                // there are no pending tasks to be scheduled,
                // wait till there are no more actually running tasks
                var timeoutSource = new CancellationTokenSource();
                try
                {
                    timeoutSource.CancelAfter(_dispatchTimeout);
                    var taskCtx = CancellationTokenSource.CreateLinkedTokenSource(contextCtx, timeoutSource.Token).Token;
                    var check = _trackableScheduler.WaitTillIdle(taskCtx);
                    bool willYield = !check.IsCompleted;
                    await check.ConfigureAwait(false);

                    // if we triggered an actual context switch, restart the loop because new items might have been added since then
                    if (willYield) return true;
                }
                catch (Exception)
                {
                    // in case of failure, keep trying till timeout is reached
                    return !timeoutSource.IsCancellationRequested;
                }
            }
            return false;
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
                catch (Exception)
                {
                    // could trigger for example when context was returned to pool with unfortunate timing
                }
            }
            return acquiredContext;
        }

        private IEnumerable<KeyValuePair<IExecutionContext, CancellationToken>> RunningContexts()
        {
            return _contexts.Where(x => {
                try
                {
                    return !(x.Value.IsCancellationRequested || x.Key.TaskStats.IsCompleted);
                }
                catch (Exception)
                {
                    // could trigger for example when context was returned to pool with unfortunate timing
                    return false;
                } 
            });
        }
    }
}

