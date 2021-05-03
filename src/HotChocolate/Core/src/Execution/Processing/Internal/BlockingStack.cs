using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace HotChocolate.Execution.Processing.Internal
{
    internal sealed class BlockingStack<T>
    {
        private readonly Stack<T> _stack = new();
        private SpinLock _lock = new(Debugger.IsAttached);

        public bool TryPop([MaybeNullWhen(false)] out T item)
        {
            var lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
#if NETSTANDARD2_0
                if (_stack.Count > 0)
                {
                    item = _stack.Pop();
                    IsEmpty = _stack.Count == 0;
                    return true;
                }

                item = default;
#else
                if (_stack.TryPop(out item))
                {
                    IsEmpty = _stack.Count == 0;
                    return true;
                }
#endif

                return false;
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }

        public void Push(T item)
        {
            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);
                _stack.Push(item);
                IsEmpty = false;
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }

        public void ClearUnsafe() =>
            _stack.Clear();

        public bool IsEmpty { get; private set; } = true;

        public int Count => _stack.Count;
    }
}
