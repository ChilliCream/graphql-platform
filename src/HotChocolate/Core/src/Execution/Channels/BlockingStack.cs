using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Channels
{
    internal sealed class BlockingStack<T>
    {
        private readonly Stack<T> _list = new Stack<T>();
        private SpinLock _lock = new SpinLock(Debugger.IsAttached);
        /// <summary>Generated whenever the amount of items in the stack becomes 0</summary>
        /// <remarks>_lock is allowed, but not required to be acquired while this event is triggered</remarks>
        private event EventHandler? StackEmptied;

        public bool TryPop([MaybeNullWhen(false)]out T item)
        {
            bool success = false;
            var lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
#if NETSTANDARD2_0
                if (_list.Count > 0)
                {
                    item = _list.Pop();
                    IsEmpty = _list.Count == 0;
                    if (IsEmpty) StackEmptied?.Invoke(this, EventArgs.Empty);
                    return true;
                }

                item = default;
#else
                if (_list.TryPop(out item))
                {
                    IsEmpty = _list.Count == 0;
                    if (IsEmpty) StackEmptied?.Invoke(this, EventArgs.Empty);
                    return true;
                }
#endif
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }

            return false;
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

        public async Task WaitTillEmpty(CancellationToken? ctx = null)
        {
            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
            var ctxRegistration = ctx?.Register(() => completion.TrySetCanceled());
            EventHandler completionHandler = (source, args) => {
                try
                {
                    if (ctx?.IsCancellationRequested ?? false)
                    {
                        completion.TrySetCanceled();
                    }
                    else if (IsEmpty)
                    {
                        completion.TrySetResult(true);
                    }
                }
                catch (Exception e)
                {
                    completion.TrySetException(e);
                }
            };
            StackEmptied += completionHandler;

            if (ctx?.IsCancellationRequested ?? false)
            {
                completion.TrySetCanceled();
            }
            else if (IsEmpty)
            {
                completion.TrySetResult(true);
            }

            await completion.Task;
            ctxRegistration?.Dispose();
            StackEmptied -= completionHandler;
        }

        public int Count => _list.Count;
    }
}
