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
        /// <summary>Generated whenever the amount of items in the stack changes</summary>
        /// <remarks>_lock will be acquired while this event is generated</remarks>
        private event EventHandler? CountChanged;

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
                    CountChanged?.Invoke(this, EventArgs.Empty);
                    return true;
                }

                item = default;
#else
                if (_list.TryPop(out item))
                {
                    IsEmpty = _list.Count == 0;
                    CountChanged?.Invoke(this, EventArgs.Empty);
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
                CountChanged?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }

        public bool IsEmpty { get; private set; } = true;

        public async ValueTask WaitTillEmpty(CancellationToken? ctx = null)
        {
            if (IsEmpty)
            {
                return;
            }

            TaskCompletionSource<bool> completion = default!;

            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);

                completion = new TaskCompletionSource<bool>();
                EventHandler completionHandler = default!;
                completionHandler = (source, args) => {
                    if (!completion.Task.IsCompleted)
                    {
                        if (IsEmpty)
                        {
                            try
                            {

                                completion.SetResult(true);
                            }
                            finally
                            {
                                CountChanged -= completionHandler;
                            }
                        }
                        else if (ctx?.IsCancellationRequested ?? false)
                        {
                            try
                            {
                                completion.SetCanceled();
                            }
                            finally
                            {
                                CountChanged -= completionHandler;
                            }
                        }
                    }
                };
                ctx?.Register(() => completionHandler(this, EventArgs.Empty));
                CountChanged += completionHandler;
            }
            finally
            {
                if (lockTaken)
                    _lock.Exit(false);
            }

            await completion.Task.ConfigureAwait(false);
        }

        public int Count => _list.Count;
    }
}
