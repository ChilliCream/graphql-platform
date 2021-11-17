using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing.Internal;
using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing;

internal partial class WorkScheduler : IWorkScheduler
{
    private bool _dispatch;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsEmpty() => _work.IsEmpty && _serial.IsEmpty;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ShouldStartProcessing() => !_processing && !IsEmpty();

    /// <inheritdoc/>
    public void Register(IExecutionTask task)
    {
        if (task is null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        var start = false;

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
                start = ShouldStartProcessing();
            }
            else
            {
                _suspended.Enqueue(task);
            }
        }

        if (start)
        {
            TryContinue();
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

        var start = false;

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
                start = ShouldStartProcessing();
            }
        }

        if (start)
        {
            TryContinue();
        }
    }

    /// <inheritdoc/>
    public void Complete(IExecutionTask task)
    {
        if (task is null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        var start = false;

        lock (_sync)
        {
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

            // if there is no more work and the state machine is not completed we will
            // close open steps and reevaluate. This can happen if optional resolver tasks
            // are not enqueued.
            while (NeedsStateMachineCompletion())
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
                start = ShouldStartProcessing();
            }

            if (TryCompleteProcessingUnsafe())
            {
                EnsureContextIsClean();
                start = true;
            }
        }

        if (start)
        {
            TryContinue();
        }

        bool NeedsStateMachineCompletion()
            => !_stateMachine.IsCompleted &&
               IsEmpty() &&
               !_work.HasRunningTasks &&
               !_serial.HasRunningTasks;
    }

    private int TryTake(IExecutionTask?[] buffer)
    {
        var size = 0;

        if (_completed || _requestAborted.IsCancellationRequested)
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
    private void TryContinue() => _pause.TryContinue();

    private async ValueTask<bool> TryStopProcessingAsync()
    {
        var processingFinished = TryStopProcessing();

        if (!_processing)
        {
            _diagnosticEvents.StopProcessing(_requestContext);

            if (_pause.IsPaused)
            {
                await _pause;
            }
        }

        return processingFinished;
    }

    private bool TryStopProcessing()
    {
        // if there is still work we keep on processing.
        // We check this here to try to avoid the lock.
        if (!_work.IsEmpty && !_requestAborted.IsCancellationRequested)
        {
            return false;
        }

        lock (_sync)
        {
            if (!_work.IsEmpty && !_requestAborted.IsCancellationRequested)
            {
                return false;
            }

            if (_dispatch && _work.IsEmpty)
            {
                using (_diagnosticEvents.DispatchBatch(_requestContext))
                {
                    _batchDispatcher.BeginDispatch(_requestAborted);
                    _dispatch = false;
                    return false;
                }
            }

            _processing = false;

            if (TryCompleteProcessingUnsafe())
            {
                EnsureContextIsClean();
                return true;
            }

            _pause.Reset();
            return false;
        }
    }

    private void BatchDispatcherEventHandler(object? source, EventArgs args)
    {
        lock (_sync)
        {
            _dispatch = true;
            if (!_processing)
            {
                TryContinue();
            }
        }
    }

    private bool TryCompleteProcessingUnsafe()
    {
        // if there are still running tasks we cannot complete the execution.
        if (HasRunningTasks())
        {
            return false;
        }

        if (HasCompleted() || _requestAborted.IsCancellationRequested)
        {
            _completed = true;
            return true;
        }

        return false;

        bool HasRunningTasks()
            => _processing ||
                _work.HasRunningTasks ||
                _serial.HasRunningTasks;

        bool HasCompleted() => IsEmpty() && _stateMachine.IsCompleted;
    }

    private void EnsureContextIsClean()
    {
        // if there are still tasks enqueued when we cancel the execution
        // we will try to reclaim the resolver tasks by properly cancelling
        // them.
        CancelTasks(_work);
        CancelTasks(_serial);
        CancelSuspendedTasks(_suspended);

        void CancelTasks(WorkQueue queue)
        {
            while (queue.TryTake(out IExecutionTask? task))
            {
                if (task is ResolverTask resolverTask)
                {
                    resolverTask.Return();
                }
            }
        }

        void CancelSuspendedTasks(SuspendedWorkQueue queue)
        {
            while (queue.TryDequeue(out IExecutionTask? task))
            {
                if (task is ResolverTask resolverTask)
                {
                    resolverTask.Return();
                }
            }
        }
    }
}
