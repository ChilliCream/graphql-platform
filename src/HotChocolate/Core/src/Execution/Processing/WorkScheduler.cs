using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing.Internal;

namespace HotChocolate.Execution.Processing
{
    internal partial class WorkScheduler : IWorkScheduler
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

        private bool IsEmpty => _work.IsEmpty && _serial.IsEmpty;

        private bool HasRunningTasks
            => _work.HasRunningTasks ||
               _serial.HasRunningTasks ||
               !_stateMachine.IsCompleted;

        /// <inheritdoc/>
        public void Register(IExecutionTask task)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var started = false;
            var state = _stateMachine.TryGetStep(task);

            lock (_sync)
            {
                if (_stateMachine.RegisterTask(task, state))
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
        public void Register(List<IExecutionTask> tasks)
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            if (tasks.Count == 1)
            {
                Register(tasks[0]!);
                return;
            }

            var started = false;

            for (var i = 0; i < tasks.Count; i++)
            {
                IExecutionTask task = tasks[i]!;
                task.State ??= _stateMachine.TryGetStep(task);
            }

            lock (_sync)
            {
                var start = false;

                for (var i = 0; i < tasks.Count; i++)
                {
                    IExecutionTask task = tasks[i]!;

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
                work.Complete();

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
                _processing = true;
                _pause.TryContinueUnsafe();
                return true;
            }

            return false;
        }

        private async ValueTask<bool> TryStopProcessing()
        {
            bool completed;

            // if the execution is already completed or if the completion task is
            // null we stop processing
            if (_completed || _requestAborted.IsCancellationRequested)
            {
                return true;
            }

            // if there is still work we keep on processing. We check this here to
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

                // if the backlog is empty and we have running tasks we will try to dispatch
                // any batch tasks.
                if (_work.HasRunningTasks && _batchDispatcher.HasTasks)
                {
                    TryDispatchBatchesUnsafe();

                    if (!_work.IsEmpty)
                    {
                        return false;
                    }
                }

                _processing = false;
                completed = TryCompleteProcessingUnsafe();
            }

            _diagnosticEvents.StopProcessing(_requestContext);

            if (completed)
            {
                return true;
            }

            await _pause;
            return completed;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
