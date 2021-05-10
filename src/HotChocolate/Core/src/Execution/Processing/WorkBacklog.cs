using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing.Internal;
using HotChocolate.Execution.Processing.Plan;

namespace HotChocolate.Execution.Processing
{
    /// <inheritdoc/>
    internal class WorkBacklog : IWorkBacklog
    {
        private SpinLock _lock = new(Debugger.IsAttached);
        private readonly UnsafeWorkQueue _work = new();
        private readonly UnsafeSuspendedWorkQueue _suspended = new();
        private readonly QueryPlanStateMachine _stateMachine = new();

        private int _processors = 1;
        private bool _mainIsPaused;
        private IRequestContext _requestContext = default!;

        public WorkBacklog()
        {
            _work.BacklogEmpty += (sender, args) => BacklogEmpty?.Invoke(sender, args);
        }

        /// <summary>
        /// The diagnostic events.
        /// </summary>
        private IDiagnosticEvents DiagnosticEvents => _requestContext.DiagnosticEvents;

        /// <inheritdoc/>
        public event EventHandler<EventArgs>? BackPressureLimitExceeded;

        /// <inheritdoc/>
        public event EventHandler<EventArgs>? BacklogEmpty;

        /// <inheritdoc/>
        public bool IsEmpty => _work.IsEmpty;

        /// <inheritdoc/>
        public bool IsRunning
        {
            get
            {
                return _work.IsRunning || !_stateMachine.IsCompleted || _processors > 1;
            }
        }

        internal void Initialize(IRequestContext requestContext, QueryPlan queryPlan)
        {
            _requestContext = requestContext;
            _stateMachine.Initialize(queryPlan);
        }

        /// <inheritdoc/>
        public bool TryTake([NotNullWhen(true)] out IExecutionTask? task)
        {
            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);
                return _work.TryTake(out task);
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }

        /// <inheritdoc/>
        public void Register(IExecutionTask task)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var lockTaken = false;
            var scaled = false;
            var backlogSize = 0;
            var processors = _processors;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_stateMachine.Register(task))
                {
                    backlogSize = _work.Push(task);

                    if (backlogSize > CalculateScalePressure() || _mainIsPaused && _processors == 1)
                    {
                        scaled = TryScaleUnsafe();
                        processors = _processors;
                    }
                }
                else
                {
                    _suspended.Enqueue(task);
                }
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }

            if (scaled)
            {
                // we invoke the scale diagnostic event after leaving the lock to not block
                // if a an event listener is badly implemented.
                DiagnosticEvents.ScaleTaskProcessors(_requestContext, backlogSize, processors);
            }
        }

        /// <inheritdoc/>
        public void Complete(IExecutionTask task)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (task.Parent is not null)
            {
                return;
            }

            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                // we first complete the task on the state machine so that if we are completing
                // the last task the state machine is marked as complete before the work queue
                // signals that it is complete.
                if (_stateMachine.Complete(task) && !_suspended.IsEmpty)
                {
                    _suspended.CopyTo(_work, _stateMachine);
                }

                // last we complete the work queue which will signal to the execution context
                // that work has been completed if it has no more tasks enqueued or marked
                // running.
                _work.Complete(task);
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }

        /// <inheritdoc/>
        public async Task WaitForWorkAsync(CancellationToken cancellationToken)
        {
            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                // if we have work we will not wait and end waiting for new work.
                if (!_work.IsEmpty || _work.IsEmpty && !IsRunning)
                {
                    return;
                }

                // mark the main processor as being paused.
                _mainIsPaused = true;
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }

            try
            {
                // lets take the first thing in the in progress list an wait for it.
                if (_work.TryPeekInProgress(out IExecutionTask? executionTask))
                {
                    await executionTask
                        .WaitForCompletionAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    // if we have no work in progress we will just wait for 2 spins for a second.
                    Thread.SpinWait(2);

                    // now lets see if there is new work or if we have completed.
                    if (!_work.IsEmpty || _work.IsEmpty && !IsRunning)
                    {
                        return;
                    }

                    // if we are still running and there is no work in progress that we can await
                    // we will yield the task back to the task scheduler for a second and then
                    // complete waiting.
                    await Task.Yield();
                }
            }
            finally
            {
                // mark the main task as in progress again.
                _mainIsPaused = false;
            }
        }

        /// <inheritdoc/>
        public bool TryCompleteProcessor()
        {
            var changedScale = false;
            var processors = _processors;
            var backlogSize = 0;

            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_mainIsPaused && _processors == 2 && !_work.IsEmpty)
                {
                    return false;
                }

                changedScale = true;
                processors = --_processors;
                backlogSize = _work.Count;
                return true;
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);

                if (changedScale)
                {
                    // we invoke the scale diagnostic event after leaving the lock to not block
                    // if a an event listener is badly implemented.
                    DiagnosticEvents.ScaleTaskProcessors(_requestContext, backlogSize, processors);
                }
            }
        }

        public void Clear()
        {
            _work.Clear();
            _suspended.Clear();
            _stateMachine.Clear();
            _processors = 1;
            BackPressureLimitExceeded = null;
        }

        private bool TryScaleUnsafe()
        {
            if (_processors < 4)
            {
                _processors++;
                BackPressureLimitExceeded?.Invoke(null, EventArgs.Empty);
                return true;
            }

            return false;
        }

        private int CalculateScalePressure() =>
            _processors switch
            {
                1 => 4,
                2 => 8,
                3 => 16,
                4 => 32,
                5 => 64,
                6 => 128,
                7 => 256,
                _ => 512
            };
    }
}
