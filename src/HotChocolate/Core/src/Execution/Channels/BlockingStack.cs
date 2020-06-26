using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace HotChocolate.Execution.Channels
{
    internal sealed class BlockingStack<T>
    {
        private readonly Stack<T> _list = new Stack<T>();
        private SpinLock _lock = new SpinLock(Debugger.IsAttached);

        public bool TryPop([MaybeNullWhen(false)]out T item)
        {
            var lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
#if NETSTANDARD2_0
                if (_list.Count > 0)
                {
                    item = _list.Pop();
                    IsEmpty = _list.Count == 0;
                    return true;
                }

                item = default;
#else
                if (_list.TryPop(out item))
                {
                    IsEmpty = _list.Count == 0;
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
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                _list.Push(item);
                IsEmpty = false;
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }

        public bool IsEmpty { get; private set; } = true;

        public int Count => _list.Count;
    }
}
