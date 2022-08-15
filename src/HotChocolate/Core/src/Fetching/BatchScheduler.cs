using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using static System.Threading.Interlocked;

namespace HotChocolate.Fetching;

/// <summary>
/// The execution engine batch dispatcher.
/// </summary>
public class BatchScheduler
    : IBatchScheduler
    , IBatchDispatcher
{
    private static List<Func<ValueTask>>? _localTasks;
    private static List<Task<Exception?>>? _localProcessing;

    private const int _waitTimeout = 30_000;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _sync = new();
    private readonly List<Func<ValueTask>> _tasks = new();
    private bool _dispatchOnSchedule;
    private bool _suspended;
    private bool _dispatchOnResume;

    /// <inheritdoc />
    public event EventHandler? TaskEnqueued;

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

    /// <inheritdoc />
    public void BeginDispatch(CancellationToken cancellationToken = default)
    {
        List<Func<ValueTask>>? tasks = null;
        Func<ValueTask>? task = null;

        lock (_sync)
        {
            if (_suspended)
            {
                _dispatchOnResume = true;
                return;
            }
            switch (_tasks.Count)
            {
                case 0:
                    return;

                case 1:
                    task = _tasks[0];
                    _tasks.Clear();
                    break;

                default:
                    // we will try to reuse the pooled list.
                    tasks = Exchange(ref _localTasks, null) ?? new();
                    tasks.AddRange(_tasks);
                    _tasks.Clear();
                    break;
            }
        }

        if (task is not null)
        {
            BeginProcessTask(task, cancellationToken);
        }
        else if (tasks is not null)
        {
            BeginProcessTasks(tasks, cancellationToken);
        }
    }

#pragma warning disable 4014
    private void BeginProcessTask(
        Func<ValueTask> task,
        CancellationToken cancellationToken = default)
        => ProcessTaskAsync(task, cancellationToken);
#pragma warning restore 4014

    private async Task ProcessTaskAsync(
        Func<ValueTask> task,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        await ExecuteBatchAsync(task, cancellationToken).ConfigureAwait(false);
    }

#pragma warning disable 4014
    private void BeginProcessTasks(
        List<Func<ValueTask>> tasks,
        CancellationToken cancellationToken = default)
        => ProcessTasksAsync(tasks, cancellationToken);
#pragma warning restore 4014

    private async Task ProcessTasksAsync(
        List<Func<ValueTask>> tasks,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        // First we will get a list to hold on to the tasks.
        List<Task<Exception?>> processing = Exchange(ref _localProcessing, null) ?? new();

        foreach (Func<ValueTask> task in tasks)
        {
            processing.Add(ExecuteBatchAsync(task, cancellationToken));
        }

        await Task.WhenAll(processing).ConfigureAwait(false);

        processing.Clear();
        tasks.Clear();

        // if there is no new instance for processing we will return our processing instances.
        CompareExchange(ref _localProcessing, processing, null);
        CompareExchange(ref _localTasks, tasks, null);
    }

    private static async Task<Exception?> ExecuteBatchAsync(
        Func<ValueTask> executeBatchAsync,
        CancellationToken cancellationToken)
    {
        try
        {
            await executeBatchAsync().ConfigureAwait(false);
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

#pragma warning disable 4014
    private void BeginDispatchOnEnqueue(Func<ValueTask> dispatch)
        => DispatchOnEnqueue(dispatch);
#pragma warning restore 4014

    private async Task DispatchOnEnqueue(Func<ValueTask> dispatch)
    {
        await _semaphore.WaitAsync(_waitTimeout).ConfigureAwait(false);

        try
        {
            await dispatch().ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Suspend()
    {
        lock(_sync) {
            _suspended = true;
        }
    }

    public void Resume()
    {
        lock (_sync)
        {
            _suspended = false;
            if (_dispatchOnResume)
            {
                BeginDispatch();
            }
        }
    }
}
