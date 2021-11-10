using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution;

namespace HotChocolate.Fetching;

/// <summary>
/// The execution engine batch dispatcher.
/// </summary>
public class BatchScheduler
    : IBatchScheduler
    , IBatchDispatcher
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _sync = new();
    private readonly List<Func<ValueTask>> _tasks = new();
    private readonly List<Func<ValueTask>> _taskQueue = new();
    private bool _dispatchOnSchedule;

    /// <inheritdoc />
    public event EventHandler? TaskEnqueued;

    /// <inheritdoc />
    public bool HasTasks => _tasks.Count > 0;

    /// <inheritdoc />
    public bool DispatchOnSchedule
    {
        get => _dispatchOnSchedule;
        set
        {
            lock (_sync)
            {
                _dispatchOnSchedule = value;
            }
        }
    }

    /// <inheritdoc />
    public Task<BatchDispatcherResult> DispatchAsync(
        CancellationToken cancellationToken = default)
        => DispatchAsync(_tasks, _taskQueue, cancellationToken);

    private async Task<BatchDispatcherResult> DispatchAsync(
        List<Func<ValueTask>> tasks,
        List<Func<ValueTask>> taskQueue,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            EnqueueTasksForProcessing();

            if (taskQueue.Count == 0)
            {
                return BatchDispatcherResult.Success;
            }

            var processing = new Task<Exception?>[taskQueue.Count];

            for (var i = 0; i < taskQueue.Count; i++)
            {
                processing[i] = ExecuteBatchAsync(taskQueue[i]);
            }

            await Task.WhenAll(processing).ConfigureAwait(false);

            List<Exception>? errors = null;

            foreach (Task<Exception?>? task in processing)
            {
                if (task.Exception is not null)
                {
                    (errors ??= new()).Add(task.Exception);
                }
            }

            return errors is null
                ? BatchDispatcherResult.Success
                : new BatchDispatcherResult(errors);
        }
        finally
        {
            _taskQueue.Clear();
            _semaphore.Release();
        }

        async Task<Exception?> ExecuteBatchAsync(Func<ValueTask> executeBatchAsync)
        {
            try
            {
                await executeBatchAsync.Invoke().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // If we run into this exception the request was aborted.
                // In this case we do nothing and just return.
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    // if cancellation is request we do no longer report errors to the
                    // operation context.
                    return null;
                }

                return ex;
            }

            return null;
        }

        void EnqueueTasksForProcessing()
        {
            lock (_sync)
            {
                if (tasks.Count > 0)
                {
                    taskQueue.AddRange(tasks);
                    tasks.Clear();
                }
            }
        }
    }

    public void Schedule(Func<ValueTask> dispatch)
    {
        bool dispatchOnSchedule;

        lock (_sync)
        {
            if (_dispatchOnSchedule)
            {
                dispatchOnSchedule = true;
            }
            else
            {
                dispatchOnSchedule = false;
                _tasks.Add(dispatch);
            }
        }

        if (dispatchOnSchedule)
        {
            BeginDispatchOnEnqueue(dispatch);
        }
        else
        {
            TaskEnqueued?.Invoke(this, EventArgs.Empty);
        }
    }

#pragma warning disable 4014
    private void BeginDispatchOnEnqueue(Func<ValueTask> dispatch) =>
        DispatchOnEnqueue(dispatch);
#pragma warning restore 4014

    private async Task DispatchOnEnqueue(Func<ValueTask> dispatch)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            await dispatch().ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
