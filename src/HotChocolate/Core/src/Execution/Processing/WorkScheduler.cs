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
    /// Registers work with the task backlog.
    /// </summary>
    public void Register(IExecutionTask task)
    {
        AssertNotPooled();

        var work = task.IsSerial ? _serial : _work;
        task.IsRegistered = true;

        lock (_sync)
        {
            work.Push(task);
        }

        _pause.TryContinue();
    }

    /// <summary>
    /// Registers work with the task backlog.
    /// </summary>
    public void Register(ReadOnlySpan<ResolverTask> tasks)
    {
        AssertNotPooled();

        lock (_sync)
        {
            for (var i = 0; i < tasks.Length; i++)
            {
                var task = tasks[i];
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

        _pause.TryContinue();
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
                lock (_sync)
                {
                    _pause.TryContinue();
                }
            }
        }
    }
}
