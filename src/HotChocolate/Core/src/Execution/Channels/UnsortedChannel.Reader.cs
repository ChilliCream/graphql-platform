using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Channels
{
    /// <summary>Provides a buffered channel of Unsorted capacity.</summary>    
    internal sealed partial class UnsortedChannel<T>
    {
        private sealed class UnsortedChannelReader
        {
            internal readonly UnsortedChannel<T> _parent;
            private readonly AsyncOperation<T> _readerSingleton;
            private readonly AsyncOperation<bool> _waiterSingleton;

            internal UnsortedChannelReader(UnsortedChannel<T> parent)
            {
                _parent = parent;
                _readerSingleton = new AsyncOperation<T>(parent._runContinuationsAsynchronously, pooled: true);
                _waiterSingleton = new AsyncOperation<bool>(parent._runContinuationsAsynchronously, pooled: true);
            }

            public bool TryRead([MaybeNullWhen(false)] out T item)
            {
                UnsortedChannel<T> parent = _parent;

                // Dequeue an item if we can
                if (parent._items.TryPop(out item))
                {
                    CompleteIfDone(parent);
                    return true;
                }

                item = default!;
                return false;
            }

            public bool TryRead(Action<T> receiver)
            {
                UnsortedChannel<T> parent = _parent;

                // Dequeue an item if we can
                if (parent._items.TryPop(receiver))
                {
                    CompleteIfDone(parent);
                    return true;
                }

                return false;
            }

            private void CompleteIfDone(UnsortedChannel<T> parent)
            {
                if (parent._doneWriting != null && parent._items.IsEmpty)
                {
                    // If we've now emptied the items queue and we're not getting any more, complete.
                    ChannelUtilities.Complete(parent._completion, parent._doneWriting);
                }
            }

            public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken));
                }

                if (!_parent._items.IsEmpty)
                {
                    return new ValueTask<bool>(true);
                }

                UnsortedChannel<T> parent = _parent;

                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // Try again to read now that we're synchronized with writers.
                    if (!parent._items.IsEmpty)
                    {
                        return new ValueTask<bool>(true);
                    }

                    // There are no items, so if we're done writing, there's never going to be data available.
                    if (parent._doneWriting != null)
                    {
                        return parent._doneWriting != ChannelUtilities.s_doneWritingSentinel ?
                            new ValueTask<bool>(Task.FromException<bool>(parent._doneWriting)) :
                            default;
                    }

                    // If we're able to use the singleton waiter, do so.
                    if (!cancellationToken.CanBeCanceled)
                    {
                        AsyncOperation<bool> singleton = _waiterSingleton;
                        if (singleton.TryOwnAndReset())
                        {
                            ChannelUtilities.QueueWaiter(ref parent._waitingReadersTail, singleton);
                            return singleton.ValueTaskOfT;
                        }
                    }

                    // Otherwise, create and queue a waiter.
                    var waiter = new AsyncOperation<bool>(parent._runContinuationsAsynchronously, cancellationToken);
                    ChannelUtilities.QueueWaiter(ref parent._waitingReadersTail, waiter);
                    return waiter.ValueTaskOfT;
                }
            }
        }
    }
}
