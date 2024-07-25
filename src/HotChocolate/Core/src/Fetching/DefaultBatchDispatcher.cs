using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using static System.Threading.Interlocked;

namespace HotChocolate.Fetching;

/// <summary>
/// The execution engine batch dispatcher.
/// </summary>
public sealed class DefaultBatchDispatcher : IBatchDispatcher
{
    private static List<Func<ValueTask>>? _localTasks;
    private static List<Task<Exception?>>? _localProcessing;

    private const int _waitTimeout = 30_000;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _sync = new();
    private readonly object _syncTaskEnqueued = new();
    private readonly List<Func<ValueTask>> _tasks = [];
    private bool _dispatchOnSchedule;
    private readonly List<EventHandler> _listeners = [];

    /// <inheritdoc />
    public event EventHandler TaskEnqueued
    {
        add
        {
            lock (_syncTaskEnqueued)
            {
                _listeners.Add(value);
            }
        }
        remove
        {
            lock (_syncTaskEnqueued)
            {
                _listeners.Remove(value);
            }
        }
    }

    public IExecutionTaskScheduler Scheduler { get; } = new BatchExecutionTaskScheduler();

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
            lock (_syncTaskEnqueued)
            {
                for (var i = 0; i < _listeners.Count; i++)
                {
                    try
                    {
                        _listeners[i].Invoke(this, EventArgs.Empty);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public void BeginDispatch(CancellationToken cancellationToken = default)
    {
        List<Func<ValueTask>>? tasks = null;
        Func<ValueTask>? task = null;

        lock (_sync)
        {
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
                    tasks = Exchange(ref _localTasks, null) ?? [];
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
    private static void BeginProcessTask(
        Func<ValueTask> task,
        CancellationToken cancellationToken = default)
        => ProcessTaskAsync(task, cancellationToken);
#pragma warning restore 4014

    private static async Task ProcessTaskAsync(
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
        var processing = Exchange(ref _localProcessing, null) ?? [];

        foreach (var task in tasks)
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
}

public sealed class DefaultExecutionTaskScheduler : IExecutionTaskScheduler
{
    public event EventHandler? AllTasksCompleted;

    public bool IsProcessing => false;

    public Task Schedule(Func<Task> work)
        => work();

    public static readonly DefaultExecutionTaskScheduler Instance = new();
}

public class BatchExecutionTaskScheduler : TaskScheduler, IExecutionTaskScheduler
{
    private readonly List<Task> _tasks = new(16);
    private int _activeTasks;

    public event EventHandler? AllTasksCompleted;

    public bool IsProcessing
    {
        get
        {
            return Volatile.Read(ref _activeTasks) == 0;
        }
    }

    public Task Schedule(Func<Task> work)
    {
        var capturedContext = ExecutionContext.Capture();

        var task = Task.Factory.StartNew<Task>(
            state =>
            {
                if (state is ExecutionContext context)
                {
                    var task = new TaskCompletionSource<object?>();

                    async void Execute(object _)
                    {
                        try
                        {
                            await work();
                        }
                        catch (Exception ex)
                        {
                            task.SetException(ex);
                        }

                        task.SetResult(null);
                    }

                    ExecutionContext.Run(context, Execute, null);

                    return task.Task;
                }

                return work();
            },
            capturedContext,
            CancellationToken.None,
            TaskCreationOptions.None,
            this);

        return task.Unwrap();
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        Task[] tasks;

        lock (_tasks)
        {
            tasks = _tasks.ToArray();
        }

        return tasks;
    }

    protected override void QueueTask(Task task)
    {
        EnqueueTask(task);

        ThreadPool.QueueUserWorkItem(
            static state =>
            {
                var scheduler = (BatchExecutionTaskScheduler)state!;
                while (scheduler.TryTakeNextTask(out var task))
                {
                    scheduler.TryExecuteTask(task);
                    scheduler.CompletedTask();
                }
            },
            this);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        if (!taskWasPreviouslyQueued)
        {
            return false;
        }

        // We try to take the task from the task backlog,
        // if not possible we cannot inline.
        if (!TryTakeTask(task))
        {
            return false;
        }

        var executed = TryExecuteTask(task);

        if (executed)
        {
            CompletedTask();
        }

        return executed;
    }

    private bool TryTakeNextTask([NotNullWhen(true)] out Task? task)
    {
        lock (_tasks)
        {
            if (_tasks.Count > 0)
            {
                var index = _tasks.Count - 1;
                task = _tasks[index];
                _tasks.RemoveAt(index);
                return true;
            }
        }

        task = null;
        return false;
    }

    private bool TryTakeTask(Task task)
    {
        lock (_tasks)
        {
            return _tasks.Remove(task);
        }
    }

    private void EnqueueTask(Task task)
    {
        Increment(ref _activeTasks);

        lock (_tasks)
        {
            _tasks.Add(task);
        }
    }

    private void CompletedTask()
    {
        var current = Decrement(ref _activeTasks);
        if (current == 0)
        {
            AllTasksCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
