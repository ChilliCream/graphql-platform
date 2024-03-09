using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.Fetching;

public sealed class DefaultBatchScheduler : IExecutorBatchScheduler
{
    private readonly object _sync = new();
    private readonly List<BatchJob> _scheduledJobs = [];
    private List<Task>? _processing;
    private List<BatchJob>? _enqueuedJobs;
    private Action? _callback;

    public void RegisterTaskEnqueuedCallback(Action callback)
    {
        if (_callback is not null)
        {
            throw new InvalidOperationException("Callback already registered.");
        }

        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    public void Schedule(BatchJob job)
    {
        lock (_sync)
        {
            _scheduledJobs.Add(job);
        }
        
        _callback?.Invoke();
    }

    public void BeginDispatch(CancellationToken cancellationToken = default)
    {
        List<BatchJob>? tasks = null;
        BatchJob? task = null;

        lock (_sync)
        {
            switch (_scheduledJobs.Count)
            {
                case 0:
                    return;

                case 1:
                    task = _scheduledJobs[0];
                    _scheduledJobs.Clear();
                    break;

                default:
                    tasks = Interlocked.Exchange(ref _enqueuedJobs, null) ?? [];
                    tasks.AddRange(_scheduledJobs);
                    _scheduledJobs.Clear();
                    break;
            }
        }

        if (task is not null)
        {
            BeginProcessTask(task.Value);
        }
        else if (tasks is not null)
        {
            BeginProcessTasks(tasks);
        }
    }

#pragma warning disable 4014
    private static void BeginProcessTask(BatchJob task)
        => ProcessTaskAsync(task);
#pragma warning restore 4014

    private static async Task ProcessTaskAsync(BatchJob task)
    {
        await Task.Yield();
        await ExecuteBatchAsync(task).ConfigureAwait(false);
    }

#pragma warning disable 4014
    private void BeginProcessTasks(List<BatchJob> tasks)
        => ProcessTasksAsync(tasks);
#pragma warning restore 4014

    private async Task ProcessTasksAsync(List<BatchJob> enqueuedJobs)
    {
        await Task.Yield();

        // First we will get a list to hold on to the tasks.
        var processing = Interlocked.Exchange(ref _processing, null) ?? [];

        foreach (var job in enqueuedJobs)
        {
            processing.Add(ExecuteBatchAsync(job));
        }

        await Task.WhenAll(processing).ConfigureAwait(false);

        processing.Clear();
        enqueuedJobs.Clear();

        // if there is no new instance for processing we will return our processing instances.
        Interlocked.CompareExchange(ref _processing, processing, null);
        Interlocked.CompareExchange(ref _enqueuedJobs, enqueuedJobs, null);
    }

    private static async Task ExecuteBatchAsync(BatchJob job)
    {
        try
        {
            await job.DispatchAsync().ConfigureAwait(false);
        }
        catch
        {
            // we catch any batching exception here.
            // standard exceptions are handled in the DataLoader itself,
            // exceptions here have to do with cancellations.
        }
    }

    public void Dispose()
    {
        _scheduledJobs.Clear();
        _processing?.Clear();
        _enqueuedJobs?.Clear();
        _callback = null;
    }
}