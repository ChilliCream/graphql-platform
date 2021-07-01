using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HotChocolate.Execution.Processing.Internal
{
    internal sealed class WorkQueue
    {
        private readonly Stack<IExecutionTask> _stack = new();
        private IExecutionTask? _runningHead;

        public bool IsEmpty => _stack.Count == 0;

        public bool HasRunningTasks => _runningHead is not null;


        public int Count => _stack.Count;

        public void Complete(IExecutionTask executionTask)
        {
            if (executionTask is null)
            {
                throw new ArgumentNullException(nameof(executionTask));
            }

            Remove(ref _runningHead, executionTask);
        }

        public bool TryPeekInProgress([MaybeNullWhen(false)] out IExecutionTask executionTask)
        {
            executionTask = _runningHead;
            return executionTask is not null;
        }

        public bool TryTake([MaybeNullWhen(false)] out IExecutionTask executionTask)
        {
#if NETSTANDARD2_0
            if (_stack.Count > 0)
            {
                executionTask = _stack.Pop();
                Add(ref _runningHead, executionTask);
                return true;
            }

            executionTask = default;
#else
            if (_stack.TryPop(out executionTask))
            {
                Add(ref _runningHead, executionTask);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Add(ref IExecutionTask? head, IExecutionTask executionTask)
        {
            executionTask.Next = head;

            if (head is not null)
            {
                head.Previous = executionTask;
            }

            head = executionTask;
        }

        private static void Remove(ref IExecutionTask? head, IExecutionTask executionTask)
        {
            IExecutionTask? previous = executionTask.Previous;
            IExecutionTask? next = executionTask.Next;

            if (previous is null)
            {
                if (ReferenceEquals(head, executionTask))
                {
                    head = next;

                    if (next is not null)
                    {
                        next.Previous = null;
                    }
                }
            }
            else
            {
                previous.Next = next;

                if (next is not null)
                {
                    next.Previous = previous;
                }
            }
        }

        public void Clear()
        {
            _stack.Clear();
            _runningHead = null;
        }
    }
}
