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
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                if (_list.TryPop(out item))
                {
                    IsEmpty = _list.Count == 0;
                    return true;
                }

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
