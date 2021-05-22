using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace HotChocolate.Execution.Processing.Internal
{
    internal sealed class DeferredWorkQueue
    {
        private SpinLock _lock = new(Debugger.IsAttached);
        private IDeferredExecutionTask? _head;

        public bool IsEmpty { get; private set; } = true;

        public int Count { get; private set; }

        public bool TryDequeue([MaybeNullWhen(false)] out IDeferredExecutionTask executionTask)
        {
            var lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);

                if (_head is null)
                {
                    executionTask = null;
                    return false;
                }

                executionTask = _head;

                if (_head == _head.Next)
                {
                    _head = null;
                    IsEmpty = true;
                    Count = 0;
                }
                else
                {
                    _head = executionTask.Next!;
                    _head.Previous = executionTask.Previous;
                    _head.Previous!.Next = _head;
                    Count--;
                }

                executionTask.Next = null;
                executionTask.Previous = null;

                return true;
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }

        public int Enqueue(IDeferredExecutionTask executionTask)
        {
            if (executionTask is null)
            {
                throw new ArgumentNullException(nameof(executionTask));
            }

            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_head is null)
                {
                    _head = executionTask;
                    _head.Next = executionTask;
                    _head.Previous = executionTask;

                    IsEmpty = false;
                    Count = 1;
                    return Count;
                }

                executionTask.Next = _head;
                executionTask.Previous = _head.Previous;
                _head.Previous!.Next = executionTask;
                _head.Previous = executionTask;

                return ++Count;
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }

        public void ClearUnsafe()
        {
            _head = null;
            IsEmpty = true;
            Count = 0;
        }
    }
}
