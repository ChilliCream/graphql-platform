using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Channels
{
    /// <summary>Provides a buffered channel of Unsorted capacity.</summary>
    internal sealed partial class UnsortedChannel<T>
    {
        private sealed class UnsortedChannelReader : ChannelReader<T>
        {
            private readonly UnsortedChannel<T> _parent;
            private readonly AsyncOperation<T> _readerSingleton;
            private readonly AsyncOperation<bool> _waiterSingleton;

            internal UnsortedChannelReader(UnsortedChannel<T> parent)
            {
                _parent = parent;
                _readerSingleton = new AsyncOperation<T>(parent._runContinuationsAsynchronously, pooled: true);
                _waiterSingleton = new AsyncOperation<bool>(parent._runContinuationsAsynchronously, pooled: true);
            }

            public override Task Completion => _parent._completion.Task;

            public override ValueTask<T> ReadAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask<T>(Task.FromCanceled<T>(cancellationToken));
                }

                // Dequeue an item if we can.
                UnsortedChannel<T> parent = _parent;
                if (parent._items.TryPop(out T item))
                {
                    CompleteIfDone(parent);
                    return new ValueTask<T>(item);
                }

                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // Try to dequeue again, now that we hold the lock.
                    if (parent._items.TryPop(out item))
                    {
                        CompleteIfDone(parent);
                        return new ValueTask<T>(item);
                    }

                    // There are no items, so if we're done writing, fail.
                    if (parent._doneWriting != null)
                    {
                        return ChannelUtilities.GetInvalidCompletionValueTask<T>(parent._doneWriting);
                    }

                    // If we're able to use the singleton reader, do so.
                    if (!cancellationToken.CanBeCanceled)
                    {
                        AsyncOperation<T> singleton = _readerSingleton;
                        if (singleton.TryOwnAndReset())
                        {
                            parent._blockedReaders.EnqueueTail(singleton);
                            return singleton.ValueTaskOfT;
                        }
                    }

                    // Otherwise, create and queue a reader.
                    var reader = new AsyncOperation<T>(parent._runContinuationsAsynchronously, cancellationToken);
                    parent._blockedReaders.EnqueueTail(reader);
                    return reader.ValueTaskOfT;
                }
            }

            public override bool TryRead([MaybeNullWhen(false)] out T item)
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

            private void CompleteIfDone(UnsortedChannel<T> parent)
            {
                if (parent._doneWriting != null && parent._items.IsEmpty)
                {
                    // If we've now emptied the items queue and we're not getting any more, complete.
                    ChannelUtilities.Complete(parent._completion, parent._doneWriting);
                }
            }

            public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
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
