using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HotChocolate.Execution.Processing.Internal
{
    internal sealed class WorkQueue
    {
        private readonly Stack<IExecutionTask> _stack = new();
        private IExecutionTask? _head;
        private int _count;

        public event EventHandler<EventArgs>? BacklogEmpty;

        public bool IsEmpty => _stack.Count == 0;

        public bool IsRunning => _head is not null;

        public int Count => _count;

        public void Complete(IExecutionTask executionTask)
        {
            if (executionTask is null)
            {
                throw new ArgumentNullException(nameof(executionTask));
            }

            IExecutionTask? previous = executionTask.Previous;
            IExecutionTask? next = executionTask.Next;

            if (previous is null)
            {
                if (ReferenceEquals(_head, executionTask))
                {
                    _head = next;

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

        public bool TryPeekInProgress([MaybeNullWhen(false)] out IExecutionTask executionTask)
        {
            executionTask = _head;
            return executionTask is not null;
        }

        public bool TryTake([MaybeNullWhen(false)] out IExecutionTask executionTask)
        {
#if NETSTANDARD2_0
            if (_stack.Count > 0)
            {
                executionTask = _stack.Pop();
                MarkInProgress(executionTask);
                _count = _stack.Count;

                if (IsEmpty)
                {
                    BacklogEmpty?.Invoke(this, EventArgs.Empty);
                }

                return true;
            }

            executionTask = default;
#else
            if (_stack.TryPop(out executionTask))
            {
                MarkInProgress(executionTask);
                _count = _stack.Count;

                if (IsEmpty)
                {
                    BacklogEmpty?.Invoke(this, EventArgs.Empty);
                }

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
            return _count = _stack.Count;
        }

        public int PushMany(IReadOnlyList<IExecutionTask> executionTasks)
        {
            if (executionTasks is null)
            {
                throw new ArgumentNullException(nameof(executionTasks));
            }

            for (var i = 0; i < executionTasks.Count; i++)
            {
                _stack.Push(executionTasks[i]);
            }

            return _count = _stack.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MarkInProgress(IExecutionTask executionTask)
        {
            executionTask.Next = _head;

            if (_head is not null)
            {
                _head.Previous = executionTask;
            }

            _head = executionTask;
        }

        public void Clear()
        {
            _stack.Clear();
            _count = 0;
            _head = null;
        }
    }
}
