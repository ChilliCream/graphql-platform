using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing.Internal;
using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing;

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

    /// <inheritdoc />
    public bool IsEmpty => _work.IsEmpty && _serial.IsEmpty;

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

    private bool TryStartProcessingUnsafe(bool force = false)
    {
        if (!_processing && (force || !_work.IsEmpty || !_serial.IsEmpty))
        {
            _processing = true;
            TryContinueUnsafe();
            return true;
        }

        return false;
    }

    private void TryContinueUnsafe()
    {
        var pause = _pause;
        _pause = null;

        if (pause is not null)
        {
            pause.TryContinue();
            _pausePool.Enqueue(pause);
        }
    }

    private async ValueTask<bool> TryStopProcessingAsync()
    {
        var stataus = TryStopProcessing();
        var pause = _pause;

        if (pause is not null)
        {
            await pause;
        }

        return stataus;
    }

    private bool TryStopProcessing()
    {
        // if the execution is already completed or if the completion task is
        // null we stop processing
        if (_completed)
        {
            return true;
        }

        // if there is still work we keep on processing. We check this here to
        // try to avoid the lock.
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

            if (CanDispatch())
            {
                _batchDispatcher.BeginDispatch(_requestAborted);
                _diagnosticEvents.DispatchBatch(_requestContext);
                return false;
            }

            _processing = false;
            _diagnosticEvents.StopProcessing(_requestContext);

            if (TryCompleteProcessingUnsafe())
            {
                return true;
            }

            var pause = _pause;

            Debug.Assert(
                pause is null,
                "Since we have only one main worker there should only be one pause obj.");

            // lets dequeue a fresh pause object and ensure its reset.
            pause = _pausePool.Dequeue();
            pause.Reset();

            // set the pause while we are holding the lock.
            _pause = pause;

            return false;
        }

        bool CanDispatch()
            => _batchDispatcher.HasTasks &&
            _work.IsEmpty &&
            _work.HasRunningTasks &&
            !_stateMachine.IsSerial;
    }

    private void BatchDispatcherEventHandler(object? source, EventArgs args)
    {
        lock (_sync)
        {
            if (!_processing)
            {
                TryStartProcessingUnsafe(force: true);
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
            EnsureContextIsClean();
            TryContinueUnsafe();
            return true;
        }

        return false;

        bool HasRunningTasks()
            => _processing ||
                _work.HasRunningTasks ||
                _serial.HasRunningTasks;

        bool HasCompleted() => IsEmpty && _stateMachine.IsCompleted;

        void EnsureContextIsClean()
        {
            // if there are still tasks enqueued when we cancel the execution
            // we will try to reclaim the resolver tasks by properly cancelling
            // them.
            CancelTasks(_work);
            CancelTasks(_serial);
            CancelSuspendedTasks(_suspended);
        }

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
