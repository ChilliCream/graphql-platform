using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Pipeline.Complexity;
using HotChocolate.Execution.Processing.Internal;
using HotChocolate.Execution.Processing.Plan;
using HotChocolate.Execution.Properties;

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
        private TaskCompletionSource<bool> _completion = default!;

        private bool _completed;
        private int _processors;
        private IRequestContext _requestContext = default!;
        private IDiagnosticEvents _diagnosticEvents = default!;

        /// <inheritdoc/>
        public event EventHandler<EventArgs>? BackPressureLimitExceeded;

        /// <inheritdoc/>
        public event EventHandler<EventArgs>? BacklogEmpty;

        /// <inheritdoc/>
        public Task Completion => _completion.Task;

        /// <inheritdoc/>
        public bool IsEmpty => _work.IsEmpty && _serial.IsEmpty;

        /// <inheritdoc/>
        public bool HasRunningTasks =>
            _work.HasRunningTasks ||
            _serial.HasRunningTasks ||
           !_stateMachine.IsCompleted;

        internal void Initialize(OperationContext operationContext, QueryPlan queryPlan)
        {
            Clear();

            if (BackPressureLimitExceeded is null || BacklogEmpty is null)
            {
                throw new InvalidOperationException(
                    Resources.WorkBacklog_NotFullyInitialized);
            }

            _completion = new TaskCompletionSource<bool>();
            _requestContext = operationContext.RequestContext;
            _diagnosticEvents = operationContext.RequestContext.DiagnosticEvents;
            _stateMachine.Initialize(operationContext, queryPlan);
            _requestContext.RequestAborted.Register(Cancel);
        }

        /// <inheritdoc />
        public int TryTake(IExecutionTask?[] buffer)
        {
            var size = 0;

            if (_completed)
            {
                return default;
            }

            lock (_sync)
            {
                WorkQueue work = _stateMachine.IsSerial ? _serial : _work;

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

                    if (backlogSize > CalculateScalePressure() || _processors == 0)
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
                _diagnosticEvents.ScaleTaskProcessorsUp(_requestContext, backlogSize, processors);
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
                    Debug.Assert(task != null!, "A task slot is not allowed to be empty.");

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

                // if the state machine is in serial mode and we already ave one processor running
                // we do not want to scale.
                if (_stateMachine.IsSerial && _processors > 0)
                {
                    return;
                }

                if (backlogSize > CalculateScalePressure() || _processors == 0)
                {
                    scaled = TryScaleUnsafe();
                    processors = _processors;
                }
            }

            if (scaled)
            {
                // we invoke the scale diagnostic event after leaving the lock to not block
                // if a an event listener is badly implemented.
                _diagnosticEvents.ScaleTaskProcessorsUp(_requestContext, backlogSize, processors);
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
                if (_stateMachine.Complete(task) && _suspended.HasWork)
                {
                    TryEnqueueSuspendedUnsafe();
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
                while (NeedsCompletion())
                {
                    if (_stateMachine.CompleteNext() && _suspended.HasWork)
                    {
                        TryEnqueueSuspendedUnsafe();
                    }
                }

                if (TryCompleteUnsafe())
                {
                    return;
                }
            }

            if (scaled)
            {
                // we invoke the scale diagnostic event after leaving the lock to not block
                // if a an event listener is badly implemented.
                _diagnosticEvents.ScaleTaskProcessorsUp(_requestContext, backlogSize, processors);
            }

            void TryEnqueueSuspendedUnsafe()
            {
                // note that backlog pressure is only measured on the default work queue since the
                // serial work queue is not scaled by default.
                _suspended.CopyTo(_work, _serial, _stateMachine);
                backlogSize = _work.Count;

                // if the state machine is in serial mode and we already ave one processor running
                // we do not want to scale.
                if (_stateMachine.IsSerial && _processors > 0)
                {
                    return;
                }

                if (backlogSize > CalculateScalePressure() || _processors == 0)
                {
                    scaled = TryScaleUnsafe();
                    processors = _processors;
                }
            }

            bool NeedsCompletion()
                => !_stateMachine.IsCompleted &&
                   _work.IsEmpty &&
                   _serial.IsEmpty &&
                   !_work.HasRunningTasks &&
                   !_serial.HasRunningTasks;
        }

        /// <inheritdoc/>
        public bool TryCompleteProcessor()
        {
            var firstTry = true;
            int processors;
            int backlogSize;

            RETRY:

            // if the execution is already completed or if the completion task is
            // null we scale down.
            if (_completed ||
                _completion is null! ||
                _requestContext.RequestAborted.IsCancellationRequested)
            {
                return true;
            }

            // if there is still work we keep on working. We check this here to
            // try to avoid the lock.
            if (!_work.IsEmpty)
            {
                return false;
            }

            lock (_sync)
            {
                if (!_work.IsEmpty)
                {
                    return false;
                }

                processors = _processors;

                // if the backlog is empty, this is the last processor and all tasks are
                // running we will signal that there is no more work that we can do.
                if (firstTry && processors == 1 && _work.HasRunningTasks)
                {
                    firstTry = false;
                    BacklogEmpty?.Invoke(this, EventArgs.Empty);
                    goto RETRY;
                }

                processors = --_processors;
                backlogSize = _work.Count;

                // if we are the last processor to shut down we will check
                // if the execution is completed.
                if (processors == 0)
                {
                    TryCompleteUnsafe();
                }
            }

            // we invoke the scale diagnostic event after leaving the lock to not block
            // if a an event listener is badly implemented.
            _diagnosticEvents.ScaleTaskProcessorsDown(
                _requestContext,
                backlogSize,
                processors);
            return true;
        }

        private void Cancel()
        {
            lock (_sync)
            {
                _completed = true;
                if (_completion is not null!)
                {
                    _completion.TrySetCanceled();
                }
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                if (_completion is not null!)
                {
                    _completion.TrySetCanceled();
                    _completion = default!;
                }

                _work.Clear();
                _suspended.Clear();
                _stateMachine.Clear();
                _processors = 0;
                _completed = false;
            }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CalculateScalePressure()
            => _processors switch
            {
                0 => 1,
                1 => 4,
                2 => 16,
                3 => 64,
                4 => 128,
                _ => throw new NotSupportedException("The scale size is not supported.")
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryCompleteUnsafe()
        {
            if (HasCompleted())
            {
                _completed = true;
                _completion.TrySetResult(true);
                return true;
            }

            if (IsCanceled())
            {
                _completed = true;
                _completion.TrySetCanceled();
                return true;
            }

            return false;

            bool HasCompleted()
                => !_completed &&
                _processors == 0 &&
                IsEmpty &&
                !HasRunningTasks;

            bool IsCanceled()
                => !_completed &&
                _processors == 0 &&
                _requestContext.RequestAborted.IsCancellationRequested;
        }
    }
}
