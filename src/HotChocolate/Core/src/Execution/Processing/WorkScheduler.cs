using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing.Internal;
using HotChocolate.Execution.Processing.Plan;

namespace HotChocolate.Execution.Processing
{
    internal partial class WorkScheduler : IWorkScheduler, IQueryPlanState
    {
        /// <inheritdoc/>
        public bool IsCompleted => _completed;

        /// <inheritdoc />
        public IDeferredWorkBacklog DeferredWork
        {
            get
            {
                AssertNotPooled();
                return _deferredWorkBacklog;
            }
        }

        /// <inheritdoc />
        public bool IsEmpty => _work.IsEmpty && _serial.IsEmpty;

        private bool HasRunningTasks
            => _work.HasRunningTasks ||
               _serial.HasRunningTasks ||
               !_stateMachine.IsCompleted;

        private bool CanDispatch
            => _batchDispatcher.HasTasks &&
               _work.IsEmpty &&
               _work.HasRunningTasks &&
               !_stateMachine.IsSerial;

        IOperationContext IQueryPlanState.Context => _operationContext;

        ISet<int> IQueryPlanState.Selections => _selections;

        /// <inheritdoc/>
        public void Register(IExecutionTask task)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var started = false;

            // first we initialize the task execution state.
            // This can be done without acquiring a lock since we only
            // interact with the task object itself.
            _stateMachine.TryInitializeTask(task);
            task.IsRegistered = true;

            lock (_sync)
            {
                if (_stateMachine.RegisterTask(task))
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
        public void Register(IReadOnlyList<IExecutionTask> tasks)
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            if (tasks.Count == 1)
            {
                Register(tasks[0]);
                return;
            }

            var started = false;

            // first we initialize the task execution state.
            // This can be done without acquiring a lock since we only
            // interact with the task object itself.
            for (var i = 0; i < tasks.Count; i++)
            {
                IExecutionTask task = tasks[i];
                _stateMachine.TryInitializeTask(task);
                task.IsRegistered = true;
            }

            lock (_sync)
            {
                var start = false;

                for (var i = 0; i < tasks.Count; i++)
                {
                    IExecutionTask task = tasks[i];

                    if (_stateMachine.RegisterTask(task))
                    {
                        WorkQueue work = task.IsSerial ? _serial : _work;
                        work.Push(task);
                        start = true;
                    }
                    else
                    {
                        _suspended.Enqueue(task);
                    }
                }

                if (start)
                {
                    started = TryStartProcessingUnsafe();
                }
            }

            if (started)
            {
                // we invoke the scale diagnostic event after leaving the lock to not block
                // if a an event listener is badly implemented.
                _diagnosticEvents.StartProcessing(_requestContext);
            }
        }

        void IQueryPlanState.RegisterUnsafe(IReadOnlyList<IExecutionTask> tasks)
        {
            for (var i = 0; i < tasks.Count; i++)
            {
                IExecutionTask task = tasks[i];
                _stateMachine.TryInitializeTask(task);
                task.IsRegistered = true;

                if (_stateMachine.RegisterTask(task))
                {
                    WorkQueue work = task.IsSerial ? _serial : _work;
                    work.Push(task);
                }
                else
                {
                    _suspended.Enqueue(task);
                }
            }
        }

        /// <inheritdoc/>
        public void Complete(IExecutionTask task)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var started = false;

            lock (_sync)
            {
                if (IsCompleted)
                {
                    return;
                }

                var registered = _serial.Count + _work.Count;

                // we first complete the task on the state machine so that if we are completing
                // the last task the state machine is marked as complete before the work queue
                // signals that it is complete.
                if (_stateMachine.Complete(task) && _suspended.HasWork)
                {
                    _suspended.CopyTo(_work, _serial, _stateMachine);
                }

                // if was registered than we will mark it complete on the queue.
                if (task.IsRegistered)
                {
                    // determine the work queue.
                    WorkQueue work = task.IsSerial ? _serial : _work;

                    // now we complete the work queue which will signal to the execution context
                    // that work has been completed if it has no more tasks enqueued or marked
                    // running.
                    work.Complete();
                }

                // if there is now more work and the state machine is not completed yet we will
                // close open steps and reevaluate. This can happen if optional resolver tasks
                // are not enqueued.
                while (NeedsStateMachineCompletion() && !_requestAborted.IsCancellationRequested)
                {
                    if (_stateMachine.CompleteNext() && _suspended.HasWork)
                    {
                        _suspended.CopyTo(_work, _serial, _stateMachine);
                    }
                }

                // if the workload changed through completion we will ensure
                // that the task processing is running.
                if (registered != _serial.Count + _work.Count)
                {
                    started = TryStartProcessingUnsafe();
                }

                TryCompleteProcessingUnsafe();
            }

            if (started)
            {
                // we invoke the scale diagnostic event after leaving the lock to not block
                // if a an event listener is badly implemented.
                _diagnosticEvents.StartProcessing(_requestContext);
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
        private bool TryStartProcessingUnsafe(bool force = false)
        {
            if (!_processing && (force || !_work.IsEmpty || !_serial.IsEmpty))
            {
                _processing = true;
                _pause.TryContinueUnsafe();
                return true;
            }

            return false;
        }

        private ValueTask<bool> TryStopProcessing()
        {
            // if the execution is already completed or if the completion task is
            // null we stop processing
            if (_completed || _requestAborted.IsCancellationRequested)
            {
                return new(true);
            }

            // if there is still work we keep on processing. We check this here to
            // try to avoid the lock.
            if (!_work.IsEmpty)
            {
                return new(false);
            }

            lock (_sync)
            {
                if (!_work.IsEmpty)
                {
                    return new(false);
                }

                if (CanDispatch)
                {
                    return InvokeDispatch();
                }

                _processing = false;
                _diagnosticEvents.StopProcessing(_requestContext);

                return TryCompleteProcessingUnsafe()
                    ? new(true)
                    : InvokePause();
            }

            async ValueTask<bool> InvokeDispatch()
            {
                // we yield here to give back control so that the lock can be released.
                await Task.Yield();

                do
                {
                    await _batchDispatcher.DispatchAsync(_requestAborted).ConfigureAwait(false);
                } while (!_requestAborted.IsCancellationRequested && CanDispatch);

                return _requestAborted.IsCancellationRequested;
            }

            async ValueTask<bool> InvokePause()
            {
                await _pause;
                return false;
            }
        }

        private void BatchDispatcherEventHandler(object? source, EventArgs args)
        {
            lock (_sync)
            {
                if (!_processing && CanDispatch)
                {
                    TryStartProcessingUnsafe(force: true);
                }
            }
        }

        private void Cancel()
        {
            lock (_sync)
            {
                _completed = true;
                _pause.TryContinueUnsafe();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryCompleteProcessingUnsafe()
        {
            if (HasCompleted())
            {
                _completed = true;
                _pause.TryContinueUnsafe();
                return true;
            }

            if (IsCanceled())
            {
                _completed = true;
                _pause.TryContinueUnsafe();
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
