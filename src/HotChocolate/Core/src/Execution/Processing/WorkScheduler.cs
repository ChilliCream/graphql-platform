using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// The work scheduler organizes the processing of request tasks.
/// </summary>
internal sealed partial class WorkScheduler
{
    /// <summary>
    /// Defines if the execution is completed.
    /// </summary>
    public bool IsCompleted
    {
        get
        {
            AssertNotPooled();
            return _isCompleted;
        }
    }

    /// <summary>
    /// Defines if the scheduler is initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Registers work with the task backlog.
    /// </summary>
    public void Register(IExecutionTask task)
    {
        AssertNotPooled();

        var work = task.IsSerial ? _serial : _work;
        task.IsRegistered = true;
        task.Id = Interlocked.Increment(ref _nextId);

        lock (_sync)
        {
            work.Push(task);
        }

        _signal.Set();
    }

    /// <summary>
    /// Registers work with the task backlog.
    /// </summary>
    public void Register(ReadOnlySpan<ResolverTask> tasks)
    {
        AssertNotPooled();

        lock (_sync)
        {
            foreach (var task in tasks)
            {
                task.Id = Interlocked.Increment(ref _nextId);
                task.IsRegistered = true;

                if (task.IsSerial)
                {
                    _serial.Push(task);
                }
                else
                {
                    _work.Push(task);
                }
            }
        }

        _signal.Set();
    }

    /// <summary>
    /// Complete a task
    /// </summary>
    public void Complete(IExecutionTask task)
    {
        AssertNotPooled();

        if (task.IsRegistered)
        {
            // complete is thread-safe
            var work = task.IsSerial ? _serial : _work;

            if (work.Complete())
            {
                _completed.TryAdd(task.Id, true);

                lock (_sync)
                {
                    _signal.Set();
                }
            }
        }
    }
}
