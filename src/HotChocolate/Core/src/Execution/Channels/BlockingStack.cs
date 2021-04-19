using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Channels
{
    internal sealed class BlockingStack<T>
    {
        private readonly ConcurrentStack<T> _list = new();
        private readonly object _lock = new();
        /// <summary>Generated whenever the amount of items in the stack becomes 0</summary>
        private event EventHandler? StackEmptied;
        /// <summary>The amount of items in _list but safe to access from multiple threads (without locking)</summary>
        private int _count = 0;

        public bool TryPop([MaybeNullWhen(false)] out T item)
        {
            if (_list.TryPop(out item))
            {
                var value = Interlocked.Decrement(ref _count);
                if (value == 0)
                {
                    lock (_lock)
                    {
                        StackEmptied?.Invoke(this, EventArgs.Empty);
                    }
                }
                return true;
            }
            return false;
        }

        public void Push(T item)
        {
            _list.Push(item);
            Interlocked.Increment(ref _count);
        }
        
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

            lock(_lock)
            { 
                StackEmptied += completionHandler;

                if (ctx?.IsCancellationRequested ?? false)
                {
                    completion.TrySetCanceled();
                }
                else if (_count == 0)
                {
                    completion.TrySetResult(true);
                }
            }

            try
            {
                await completion.Task.ConfigureAwait(false);
            }
            finally
            {
                ctxRegistration?.Dispose();
                lock (_lock)
                {
                    StackEmptied -= completionHandler;
                }
            }
        }

        public int Count => _count;
    }
}
