using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace HotChocolate.Execution.Processing.Internal
{
    internal sealed class MonitorWorkQueue
    {
        private readonly object _sync = new();
        private readonly Stack<IExecutionTask> _stack = new();
        private IExecutionTask? _head;

        public event EventHandler<EventArgs>? BacklogEmpty;

        public bool IsEmpty { get; private set; } = true;

        public bool IsRunning => _head is not null;

        public void Complete(IExecutionTask executionTask)
        {
            if (executionTask is null)
            {
                throw new ArgumentNullException(nameof(executionTask));
            }

            var lockTaken = false;

            try
            {
                Monitor.Enter(_sync, ref lockTaken);

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
            finally
            {
                if (lockTaken) Monitor.Exit(_sync);
            }
        }

        public bool TryPeekInProgress([MaybeNullWhen(false)] out IExecutionTask executionTask)
        {
            executionTask = _head;
            return executionTask is not null;
        }

        public bool TryTake([MaybeNullWhen(false)] out IExecutionTask executionTask)
        {
            var lockTaken = false;
            try
            {
                Monitor.Enter(_sync, ref lockTaken);
#if NETSTANDARD2_0
                if (_stack.Count > 0)
                {
                    executionTask = _stack.Pop();
                    MarkInProgress(executionTask);
                    IsEmpty = _stack.Count == 0;

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
                    IsEmpty = _stack.Count == 0;

                    if (IsEmpty)
                    {
                        BacklogEmpty?.Invoke(this, EventArgs.Empty);
                    }

                    return true;
                }
#endif
                return false;
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_sync);
            }
        }

        public int Push(IExecutionTask executionTask)
        {
            if (executionTask is null)
            {
                throw new ArgumentNullException(nameof(executionTask));
            }

            var lockTaken = false;

            try
            {
                Monitor.Enter(_sync, ref lockTaken);
                _stack.Push(executionTask);
                IsEmpty = false;
                return _stack.Count;
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_sync);
            }
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

        public void ClearUnsafe()
        {
            _stack.Clear();
            _head = null;
        }
    }
}
