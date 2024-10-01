using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Processing;

internal sealed class WorkQueue
{
    private readonly Stack<IExecutionTask> _stack = new();
    private int _running;

    public bool IsEmpty => _stack.Count == 0;

    public bool HasRunningTasks => _running > 0;

    public bool Complete()
    {
        var value = Interlocked.Decrement(ref _running);

        if (value < 0)
        {
            throw new InvalidOperationException();
        }

        return value is 0;
    }

    public bool TryTake([MaybeNullWhen(false)] out IExecutionTask executionTask)
    {
        if (_stack.TryPop(out executionTask))
        {
            Interlocked.Increment(ref _running);
            return true;
        }

        return false;
    }

    public void Push(IExecutionTask executionTask)
    {
        if (executionTask is null)
        {
            throw new ArgumentNullException(nameof(executionTask));
        }

        _stack.Push(executionTask);
    }

    public void Clear()
    {
        _stack.Clear();
        _running = 0;
    }
}
