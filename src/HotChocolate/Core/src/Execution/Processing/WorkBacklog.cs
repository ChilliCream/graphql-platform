using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        private readonly object _sync = new();
        private readonly WorkQueue _work = new();
        private readonly WorkQueue _serial = new();
        private readonly SuspendedWorkQueue _suspended = new();
        private readonly QueryPlanStateMachine _stateMachine = new();

        private int _processors = 1;
        private bool _mainIsPaused;
        private IRequestContext _requestContext = default!;
        private IDiagnosticEvents _diagnosticEvents = default!;

        public WorkBacklog()
        {
            _work.BacklogEmpty += (sender, args) => BacklogEmpty?.Invoke(sender, args);
        }

        /// <inheritdoc/>
        public event EventHandler<EventArgs>? BackPressureLimitExceeded;

        /// <inheritdoc/>
        public event EventHandler<EventArgs>? BacklogEmpty;

        /// <inheritdoc/>
        public bool IsEmpty => _work.IsEmpty && _serial.IsEmpty;

        /// <inheritdoc/>
        public bool IsRunning =>
            _work.IsRunning ||
            _serial.IsRunning ||
           !_stateMachine.IsCompleted ||
           _processors > 1;

        internal void Initialize(OperationContext operationContext, QueryPlan queryPlan)
        {
            _requestContext = operationContext.RequestContext;
            _diagnosticEvents = operationContext.RequestContext.DiagnosticEvents;
            _stateMachine.Initialize(operationContext, queryPlan);
        }

        /// <inheritdoc />
        public int TryTake(IExecutionTask?[] buffer, bool main)
        {
            lock (_sync)
            {
                var size = 0;
                WorkQueue work = main && _stateMachine.IsSerial ? _serial : _work;

                for (var i = 0; i < buffer.Length; i++)
                {
                    if (!work.TryTake(out IExecutionTask? task))
                    {
                        break;
                    }

                    size++;
                    buffer[i] = task;
                }

                return size;
            }
        }

        /// <inheritdoc/>
        public void Register(IExecutionTask task)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var scaled = false;
            var backlogSize = 0;
            var processors = _processors;

            lock (_sync)
            {
                if (_stateMachine.Register(task))
                {
                    WorkQueue work = task.IsSerial ? _serial : _work;
                    backlogSize = work.Push(task);

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

            if (scaled)
            {
                // we invoke the scale diagnostic event after leaving the lock to not block
                // if a an event listener is badly implemented.
                _diagnosticEvents.ScaleTaskProcessors(_requestContext, backlogSize, processors);
            }
        }

        /// <inheritdoc/>
        public void Register(IExecutionTask?[] tasks, int length)
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            var scaled = false;
            var backlogSize = 0;
            var processors = _processors;

            lock (_sync)
            {
                for (var i = 0; i < length; i++)
                {
                    IExecutionTask task = tasks[i]!;
                    tasks[i] = null;
                    Debug.Assert(task != null, "A task slot is not allowed to be empty.");

                    if (_stateMachine.Register(task))
                    {
                        WorkQueue work = task.IsSerial ? _serial : _work;
                        backlogSize = work.Push(task);
                    }
                    else
                    {
                        _suspended.Enqueue(task);
                    }
                }

                if (backlogSize > CalculateScalePressure() || _mainIsPaused && _processors == 1)
                {
                    scaled = TryScaleUnsafe();
                    processors = _processors;
                }
            }

            if (scaled)
            {
                // we invoke the scale diagnostic event after leaving the lock to not block
                // if a an event listener is badly implemented.
                _diagnosticEvents.ScaleTaskProcessors(_requestContext, backlogSize, processors);
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

            var scaled = false;
            var backlogSize = 0;
            var processors = _processors;

            lock (_sync)
            {
                // we first complete the task on the state machine so that if we are completing
                // the last task the state machine is marked as complete before the work queue
                // signals that it is complete.
                if (_stateMachine.Complete(task) && !_suspended.IsEmpty)
                {
                    TryEnqueueSuspended();
                }

                // determine the work queue.
                WorkQueue work = task.IsSerial ? _serial : _work;

                // now we complete the work queue which will signal to the execution context
                // that work has been completed if it has no more tasks enqueued or marked
                // running.
                work.Complete(task);

                // if there is now more work and the state machine is not completed yet we will
                // close open steps and reevaluate. This can happen if optional resolver tasks
                // are not enqueued.
                while (!_stateMachine.IsCompleted &&
                    _work.IsEmpty &&
                    _serial.IsEmpty &&
                    !_work.IsRunning &&
                    !_serial.IsRunning)
                {
                    if (_stateMachine.CompleteNext() && !_suspended.IsEmpty)
                    {
                        TryEnqueueSuspended();
                    }
                }
            }

            if (scaled)
            {
                // we invoke the scale diagnostic event after leaving the lock to not block
                // if a an event listener is badly implemented.
                _diagnosticEvents.ScaleTaskProcessors(_requestContext, backlogSize, processors);
            }

            void TryEnqueueSuspended()
            {
                // note that backlog pressure is only measured on the default work queue since the
                // serial work queue is not scaled by default.
                _suspended.CopyTo(_work, _serial, _stateMachine);
                backlogSize = _work.Count;

                if (backlogSize > CalculateScalePressure() || _mainIsPaused && _processors == 1)
                {
                    scaled = TryScaleUnsafe();
                    processors = _processors;
                }
            }
        }

        /// <inheritdoc/>
        public async Task WaitForWorkAsync(CancellationToken cancellationToken)
        {
            // we do not have any code to switch between serial and parallel work since the wait
            // for work is only meant for parallel work since the serial work is executed in
            // one stream.

            lock (_sync)
            {
                // if we have work we will not wait and end waiting for new work.
                if (!_work.IsEmpty || _work.IsEmpty && !IsRunning)
                {
                    return;
                }

                // mark the main processor as being paused.
                _mainIsPaused = true;
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

            try
            {
                lock (_sync)
                {
                    if (_mainIsPaused && _processors == 2 && !_work.IsEmpty)
                    {
                        return false;
                    }

                    changedScale = true;
                    processors = --_processors;
                    backlogSize = _work.Count;
                    return true;
                }
            }
            finally
            {
                if (changedScale)
                {
                    // we invoke the scale diagnostic event after leaving the lock to not block
                    // if a an event listener is badly implemented.
                    _diagnosticEvents.ScaleTaskProcessors(_requestContext, backlogSize, processors);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryScaleUnsafe()
        {
            if (_processors < 4 && BackPressureLimitExceeded is not null)
            {
                _processors++;
                BackPressureLimitExceeded(null, EventArgs.Empty);
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
