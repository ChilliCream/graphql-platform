using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Processing;

internal sealed class WorkQueue
{
    private readonly Stack<IExecutionTask> _immediateStack = new();
    private readonly Stack<IExecutionTask> _deferredStack = new();
    private int _running;

    public bool IsEmpty => _immediateStack.Count == 0 && _deferredStack.Count == 0;

    /// <summary>
    /// Gets a value indicating whether taken tasks have not been completed yet.
    /// The scheduler lock must be held.
    /// </summary>
    public bool HasRunningTasks => _running > 0;

    /// <summary>
    /// Marks a taken task as completed and returns <c>true</c> if no taken tasks remain.
    /// The scheduler lock must be held.
    /// </summary>
    public bool Complete()
    {
        var value = --_running;

        if (value < 0)
        {
            throw new InvalidOperationException();
        }

        return value is 0;
    }

    /// <summary>
    /// Takes the next task, preferring immediate over deferred tasks, and counts it as running.
    /// The scheduler lock must be held.
    /// </summary>
    public bool TryTake([MaybeNullWhen(false)] out IExecutionTask executionTask)
    {
        if (_immediateStack.TryPop(out executionTask)
            || _deferredStack.TryPop(out executionTask))
        {
            _running++;
            return true;
        }

        return false;
    }

    public void Push(IExecutionTask executionTask)
    {
        ArgumentNullException.ThrowIfNull(executionTask);

        if (executionTask.IsDeferred)
        {
            _deferredStack.Push(executionTask);
        }
        else
        {
            _immediateStack.Push(executionTask);
        }
    }

    /// <summary>
    /// Removes all tasks and resets the running count.
    /// Only valid on an idle or discarded scheduler, and does not take the scheduler lock.
    /// </summary>
    public void Clear()
    {
        _immediateStack.Clear();
        _deferredStack.Clear();
        _running = 0;
    }
}
