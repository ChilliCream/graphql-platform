using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing.Plan;

namespace HotChocolate.Execution.Processing.Internal;

internal sealed class SuspendedWorkQueue
{
    private IExecutionTask? _head;

    public bool IsEmpty { get; private set; } = true;

    public bool HasWork => !IsEmpty;

    public void CopyTo(WorkQueue work, WorkQueue serial, QueryPlanStateMachine stateMachine)
    {
        IExecutionTask? head = _head;
        _head = null;

        while (head is not null)
        {
            IExecutionTask current = head;
            head = head.Next;
            current.Next = null;

            if (stateMachine.IsSuspended(current))
            {
                AppendTask(ref _head, current);
            }
            else
            {
                (current.IsSerial ? serial : work).Push(current);
            }
        }

        IsEmpty = _head is null;
    }

    public void Enqueue(IExecutionTask executionTask)
    {
        if (executionTask is null)
        {
            throw new ArgumentNullException(nameof(executionTask));
        }

        AppendTask(ref _head, executionTask);
        IsEmpty = false;
    }

    public bool TryDequeue([NotNullWhen(true)] out IExecutionTask? executionTask)
    {
        executionTask = _head;

        if (executionTask is null)
        {
            return false;
        }

        _head = _head?.Next;
        executionTask.Next = null;
        IsEmpty = _head is null;
        return true;
    }

    private static void AppendTask(ref IExecutionTask? head, IExecutionTask executionTask)
    {
        executionTask.Previous = null;
        executionTask.Next = head;
        head = executionTask;
    }

    public void Clear()
    {
        _head = null;
        IsEmpty = true;
    }
}
