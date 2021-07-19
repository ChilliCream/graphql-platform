using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing.Internal;
using HotChocolate.Execution.Processing.Plan;
using HotChocolate.Fetching;

namespace HotChocolate.Execution.Processing
{
    /// <inheritdoc/>
    internal partial class WorkBacklog : IWorkBacklog
    {
        private static readonly Task<bool> _falseResult = Task.FromResult(false);
        private static readonly Task<bool> _trueResult = Task.FromResult(true);

        private readonly object _sync = new();
        private readonly IExecutionTask?[] _buffer = new IExecutionTask[4];
        private readonly WorkQueue _work = new();
        private readonly WorkQueue _serial = new();
        private readonly SuspendedWorkQueue _suspended = new();
        private readonly QueryPlanStateMachine _stateMachine = new();

        private TaskCompletionSource<bool> _completion = default!;
        private TaskCompletionSource<bool>? _pause;

        private bool _processing;
        private bool _completed;

        private IRequestContext _requestContext = default!;
        private IBatchDispatcher _batchDispatcher = default!;
        private IErrorHandler _errorHandler = default!;
        private IResultHelper _result = default!;
        private IDiagnosticEvents _diagnosticEvents = default!;
        private CancellationToken _requestAborted;

        private bool IsEmpty => _work.IsEmpty && _serial.IsEmpty;

        private bool HasRunningTasks
            => _work.HasRunningTasks ||
               _serial.HasRunningTasks ||
               !_stateMachine.IsCompleted;

        /// <inheritdoc/>
        public Task Completion => _completion.Task;

        internal void Initialize(OperationContext operationContext)
        {
            Clear();

            _completion = new TaskCompletionSource<bool>();

            _batchDispatcher = operationContext.Execution.BatchDispatcher;
            _requestContext = operationContext.RequestContext;
            _diagnosticEvents = operationContext.RequestContext.DiagnosticEvents;
            _requestAborted = operationContext.RequestAborted;
            _errorHandler = operationContext.ErrorHandler;
            _result = operationContext.Result;

            _stateMachine.Initialize(operationContext, operationContext.QueryPlan);
            _requestContext.RequestAborted.Register(Cancel);

            _batchDispatcher.TaskEnqueued += BatchDispatcherEventHandler;
        }

        /// <inheritdoc/>
        public void Register(IExecutionTask task)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var started = false;

            lock (_sync)
            {
                if (_stateMachine.Register(task))
                {
                    WorkQueue work = task.IsSerial ? _serial : _work;
                    work.Push(task);

                    started = TryStartProcessingUnsafe();
                }
                else
                {
                    _suspended.Enqueue(task);
                }
            }

            if (started)
            {
                // we invoke the scale diagnostic event after leaving the lock to not block
                // if a an event listener is badly implemented.
                _diagnosticEvents.StartProcessing(_requestContext);
            }
        }

        /// <inheritdoc/>
        public void Register(IExecutionTask?[] tasks, int length)
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            bool started;

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
                        work.Push(task);
                    }
                    else
                    {
                        _suspended.Enqueue(task);
                    }
                }

                started = TryStartProcessingUnsafe();
            }

            if (started)
            {
                // we invoke the scale diagnostic event after leaving the lock to not block
                // if a an event listener is badly implemented.
                _diagnosticEvents.StartProcessing(_requestContext);
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

            lock (_sync)
            {
                // we first complete the task on the state machine so that if we are completing
                // the last task the state machine is marked as complete before the work queue
                // signals that it is complete.
                if (_stateMachine.Complete(task) && _suspended.HasWork)
                {
                    _suspended.CopyTo(_work, _serial, _stateMachine);
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
                while (NeedsStateMachineCompletion())
                {
                    if (_stateMachine.CompleteNext() && _suspended.HasWork)
                    {
                        _suspended.CopyTo(_work, _serial, _stateMachine);
                    }
                }

                TryCompleteProcessingUnsafe();
            }

            bool NeedsStateMachineCompletion()
                => !_stateMachine.IsCompleted &&
                   _work.IsEmpty &&
                   _serial.IsEmpty &&
                   !_work.HasRunningTasks &&
                   !_serial.HasRunningTasks;
        }

        private int TryTake(IExecutionTask?[] buffer)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryStartProcessingUnsafe()
        {
            if (!_processing && (!_work.IsEmpty || !_serial.IsEmpty))
            {
                if (_pause is null)
                {
                    StartProcessing();
                }
                else
                {
                    TaskCompletionSource<bool> pause = _pause;
                    _pause = null;
                    pause.TrySetResult(false);
                }

                return true;
            }

            return false;
        }

        private Task<bool> TryStopProcessing()
        {
            bool completed;

            // if the execution is already completed or if the completion task is
            // null we stop processing
            if (_completed || _completion is null! || _requestAborted.IsCancellationRequested)
            {
                return _trueResult;
            }

            // if there is still work we keep on processing. We check this here to
            // try to avoid the lock.
            if (!_work.IsEmpty)
            {
                return _falseResult;
            }

            lock (_sync)
            {
                if (!_work.IsEmpty)
                {
                    return _falseResult;
                }

                // if the backlog is empty and we have running tasks we will try to dispatch
                // any batch tasks.
                if (_work.HasRunningTasks)
                {
                    TryDispatchBatchesUnsafe();

                    if (!_work.IsEmpty)
                    {
                        return _falseResult;
                    }
                }

                completed = TryCompleteProcessingUnsafe();
            }

            _diagnosticEvents.StopProcessing(_requestContext);

            if (completed)
            {
                return _trueResult;
            }

            _pause = new TaskCompletionSource<bool>();
            return _pause.Task;
        }

        private void TryDispatchBatches()
        {
            if (!_processing && IsEmpty && _batchDispatcher.HasTasks)
            {
                lock (_sync)
                {
                    if (!_processing)
                    {
                        TryDispatchBatchesUnsafe();
                    }
                }
            }
        }

        private void TryDispatchBatchesUnsafe()
        {
            if (IsEmpty && _batchDispatcher.HasTasks)
            {
                using (_diagnosticEvents.DispatchBatch(_requestContext))
                {
                    _batchDispatcher.Dispatch();
                }
            }
        }

        private void BatchDispatcherEventHandler(object? source, EventArgs args)
            => TryDispatchBatches();

        private void Cancel()
        {
            lock (_sync)
            {
                _completed = true;

                if (_completion is not null!)
                {
                    _completion.TrySetCanceled();
                }

                _pause?.TrySetResult(true);
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

                _pause?.TrySetResult(true);
                _pause = null;

                if (_batchDispatcher is not null!)
                {
                    _batchDispatcher.TaskEnqueued -= BatchDispatcherEventHandler;
                    _batchDispatcher = default!;
                }

                _work.Clear();
                _suspended.Clear();
                _stateMachine.Clear();
                _processing = false;
                _completed = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryCompleteProcessingUnsafe()
        {
            if (HasCompleted())
            {
                _completed = true;
                _completion.TrySetResult(true);
                _pause?.TrySetResult(true);
                return true;
            }

            if (IsCanceled())
            {
                _completed = true;
                _completion.TrySetCanceled();
                _pause?.TrySetResult(true);
                return true;
            }

            return false;

            bool HasCompleted()
                => !_completed &&
                   !_processing &&
                   IsEmpty &&
                   !HasRunningTasks;

            bool IsCanceled()
                => !_completed &&
                   !_processing &&
                   _requestAborted.IsCancellationRequested;
        }
    }
}
