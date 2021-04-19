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
        private event EventHandler? StackEmptied;

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
                    var value = Interlocked.Decrement(ref _count);
                    if (value == 0) StackEmptied?.Invoke(this, EventArgs.Empty);
                    return true;
                }

                item = default;
#else
                if (_list.TryPop(out item))
                {
                    var value = Interlocked.Decrement(ref _count);
                    if (value == 0) StackEmptied?.Invoke(this, EventArgs.Empty);
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
                Interlocked.Increment(ref _count);
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }

        private int _count = 0;
        public bool IsEmpty => _count == 0;

        public async Task WaitTillEmpty(CancellationToken? ctx = null)
        {
            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
            CancellationTokenRegistration? ctxRegistration = ctx?.Register(() => completion.TrySetCanceled());
            EventHandler completionHandler = (source, args) =>
            {
                try
                {
                    if (ctx?.IsCancellationRequested ?? false)
                    {
                        completion.TrySetCanceled();
                    }
                    else
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

            try
            {
                await completion.Task.ConfigureAwait(false);
            }
            finally
            {
                ctxRegistration?.Dispose();
                StackEmptied -= completionHandler;
            }
        }

        public int Count => _count;
    }
}
