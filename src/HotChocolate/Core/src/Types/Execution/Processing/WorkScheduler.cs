namespace HotChocolate.Execution.Processing;

/// <summary>
/// The work scheduler organizes the processing of request tasks.
/// </summary>
internal sealed partial class WorkScheduler
{
    private readonly Dictionary<int, Branch> _activeBranches = [];

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

            if (!_activeBranches.TryGetValue(task.BranchId, out var branch))
            {
                branch = new Branch(task.BranchId);
                _activeBranches.Add(task.BranchId, branch);
            }

            branch.RegisterTask();
        }

        _signal.Set();
    }

    /// <summary>
    /// Registers work with the task backlog.
    /// </summary>
    public void Register(ReadOnlySpan<IExecutionTask> tasks)
    {
        AssertNotPooled();

        lock (_sync)
        {
            for (var i = tasks.Length - 1; i >= 0; i--)
            {
                var task = tasks[i];
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

                if (!_activeBranches.TryGetValue(task.BranchId, out var branch))
                {
                    branch = new Branch(task.BranchId);
                    _activeBranches.Add(task.BranchId, branch);
                }

                branch.RegisterTask();
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
                    if (_activeBranches.TryGetValue(task.BranchId, out var branch)
                        && branch.CompleteTask())
                    {
                        _activeBranches.Remove(task.BranchId);
                        branch.Complete();
                    }

                    _signal.Set();
                }
            }
        }
    }

    private sealed class Branch(int id)
    {
        private readonly AsyncManualResetEvent _signal = new();
        private int _runningTasks;

        public int Id { get; } = id;

        public int RunningTasks => _runningTasks;

        public void RegisterTask() => _runningTasks++;

        public bool CompleteTask() => --_runningTasks == 0;

        public void Complete() => _signal.Set();

        public async ValueTask WaitForCompletionAsync(CancellationToken cancellationToken)
        {
            await using var registration = cancellationToken.Register(_signal.Set);
            await _signal;
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
