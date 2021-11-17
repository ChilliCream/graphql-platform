using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Processing.Internal;

internal sealed class WorkQueue
{
    private readonly Stack<IExecutionTask> _stack = new();
    private int _running;

    public bool IsEmpty => _stack.Count == 0;

    public bool HasRunningTasks => _running > 0;

    public int Count => _stack.Count;

    public void Complete()
    {
        Debug.Assert(_running > 0, "There are no running tasks.");
        _running--;
    }

    public bool TryTake([MaybeNullWhen(false)] out IExecutionTask executionTask)
    {
#if NETSTANDARD2_0
        if (_stack.Count > 0)
        {
            executionTask = _stack.Pop();
            _running++;
            return true;
        }

        executionTask = default;
#else
        if (_stack.TryPop(out executionTask))
        {
            _running++;
            return true;
        }
#endif
        return false;
    }

    public int Push(IExecutionTask executionTask)
    {
        if (executionTask is null)
        {
            throw new ArgumentNullException(nameof(executionTask));
        }

        _stack.Push(executionTask);
        return _stack.Count;
    }

    public void Clear()
    {
        _stack.Clear();
        _running = 0;
    }
}
