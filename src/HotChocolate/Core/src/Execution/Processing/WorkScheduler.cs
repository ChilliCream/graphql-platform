using System.Collections.Generic;

namespace HotChocolate.Execution.Processing;

internal sealed partial class WorkScheduler : IWorkScheduler
{
    public bool IsCompleted
    {
        get
        {
            AssertNotPooled();
            return _isCompleted;
        }
    }

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

    public void Register(IReadOnlyList<IExecutionTask> tasks)
    {
        AssertNotPooled();

        lock (_sync)
        {
            for (var i = 0; i < tasks.Count; i++)
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
