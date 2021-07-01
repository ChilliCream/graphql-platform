using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        private TaskCompletionSource<bool> _completion = default!;
        private bool _completed;

        private int _processors = 1;
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
            _completion = new TaskCompletionSource<bool>();
            _requestContext = operationContext.RequestContext;
            _diagnosticEvents = operationContext.RequestContext.DiagnosticEvents;
            _stateMachine.Initialize(operationContext, queryPlan);
        }

        /// <inheritdoc />
        public int TryTake(IExecutionTask?[] buffer, bool main)
        {
            var size = 0;

            lock (_sync)
            {
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

                if (_stateMachine.IsSerial)
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
                    if (_stateMachine.CompleteNext() && !_suspended.IsEmpty)
                    {
                        TryEnqueueSuspendedUnsafe();
                    }
                }

                TryCompleteUnsafe();
            }

            if (scaled)
            {
                // we invoke the scale diagnostic event after leaving the lock to not block
                // if a an event listener is badly implemented.
                _diagnosticEvents.ScaleTaskProcessors(_requestContext, backlogSize, processors);
            }

            void TryEnqueueSuspendedUnsafe()
            {
                // note that backlog pressure is only measured on the default work queue since the
                // serial work queue is not scaled by default.
                _suspended.CopyTo(_work, _serial, _stateMachine);
                backlogSize = _work.Count;

                if (_stateMachine.IsSerial)
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
            var changedScale = false;
            var processors = _processors;
            var backlogSize = 0;

            if (_completed || _completion is null!)
            {
                return true;
            }

            try
            {
                lock (_sync)
                {
                    if (!_work.IsEmpty)
                    {
                        return false;
                    }

                    // if the backlog is empty and all tasks are running we will signal that
                    // there is no more work that we can do.
                    if (_processors == 1 && _work.IsEmpty && _work.HasRunningTasks)
                    {
                        BacklogEmpty?.Invoke(this, EventArgs.Empty);

                        // if the signal caused other components to store more work we will
                        // keep this processor alive and deny its request to scale down.
                        if (!IsEmpty)
                        {
                            return false;
                        }
                    }

                    changedScale = true;
                    processors = --_processors;
                    backlogSize = _work.Count;

                    if (processors == 0)
                    {
                        TryCompleteUnsafe();
                    }

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
            _completion.TrySetCanceled();
            _completion = default!;
            _work.Clear();
            _suspended.Clear();
            _stateMachine.Clear();
            _processors = 1;
            _completed = false;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryCompleteUnsafe()
        {
            if (HasCompleted())
            {
                _completion.TrySetResult(true);
            }

            bool HasCompleted() => IsEmpty && !HasRunningTasks && _processors == 0;
        }
    }
}
