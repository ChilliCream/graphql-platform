using System.Collections.Generic;

namespace HotChocolate.Execution.Processing;

internal sealed partial  class WorkScheduler : IWorkScheduler
{
    public bool IsCompleted
    {
        get
        {
            AssertNotPooled();
            return _isCompleted;
        }
    }

    public IDeferredWorkBacklog DeferredWork
    {
        get
        {
            AssertNotPooled();
            return _deferred;
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
        
        var hasWork = true;

        if (task.IsRegistered)
        {
            var work = task.IsSerial ? _serial : _work;

            lock (task)
            {
                work.Complete();
                hasWork = (!_work.IsEmpty && !_serial.IsEmpty) || _work.HasRunningTasks;
            }
        }

        if (!hasWork)
        {
            _pause.TryContinue();
        }
    }
}
