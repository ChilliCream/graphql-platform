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
                    var completionTookLock = false;
                    try
                    {
                        if (!_lock.IsHeld)
                        {
                            _lock.Enter(ref completionTookLock);
                        }
                        if (!completion.Task.IsCompleted)
                        {
                            try
                            {
                                if (ctx?.IsCancellationRequested ?? false)
                                {
                                    completion.SetCanceled();
                                }
                                else
                                {
                                    completion.SetResult(true);
                                }
                            }
                            catch (Exception e)
                            {
                                completion.SetException(e);
                            }
                            finally
                            {
                                StackEmptied -= completionHandler;
                            }
                        }
                    }
                    finally
                    {
                        if (completionTookLock) _lock.Exit(false);
                    }
             

                    
                };
                StackEmptied += completionHandler;
                ctx?.Register(() => completionHandler(this, EventArgs.Empty));
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
