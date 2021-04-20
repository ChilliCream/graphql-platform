using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Channels
{
    /// <summary>Provides a buffered channel of Unsorted capacity.</summary>    
    internal sealed partial class UnsortedChannel<T>
    {
        private sealed class UnsortedChannelWriter : ChannelWriter<T>
        {
            internal readonly UnsortedChannel<T> _parent;
            internal UnsortedChannelWriter(UnsortedChannel<T> parent) => _parent = parent;

            public override bool TryComplete(Exception? error)
            {
                UnsortedChannel<T> parent = _parent;
                bool completeTask;

                lock (parent.SyncObj)
                {
                    parent.AssertInvariants();

                    // If we've already marked the channel as completed, bail.
                    if (parent._doneWriting != null)
                    {
                        return false;
                    }

                    // Mark that we're done writing.
                    parent._doneWriting = error ?? ChannelUtilities.s_doneWritingSentinel;
                    completeTask = parent._items.IsEmpty;
                }

                // If there are no items in the queue, complete the channel's task,
                // as no more data can possibly arrive at this point.  We do this outside
                // of the lock in case we'll be running synchronous completions, and we
                // do it before completing blocked/waiting readers, so that when they
                // wake up they'll see the task as being completed.
                if (completeTask)
                {
                    ChannelUtilities.Complete(parent._completion, error);
                }

                // At this point, _blockedReaders and _waitingReaders will not be mutated:
                // they're only mutated by readers while holding the lock, and only if _doneWriting is null.
                // freely manipulate _blockedReaders and _waitingReaders without any concurrency concerns.
                ChannelUtilities.FailOperations<AsyncOperation<T>, T>(parent._blockedReaders, ChannelUtilities.CreateInvalidCompletionException(error));
                ChannelUtilities.WakeUpWaiters(ref parent._waitingReadersTail, result: false, error: error);

                // Successfully transitioned to completed.
                return true;
            }

            public override bool TryWrite(T item)
            {
                UnsortedChannel<T> parent = _parent;
                while (true)
                {
                    AsyncOperation<T>? blockedReader = null;
                    AsyncOperation<bool>? waitingReadersTail = null;
                    lock (parent.SyncObj)
                    {
                        // If writing has already been marked as done, fail the write.
                        parent.AssertInvariants();
                        if (parent._doneWriting != null)
                        {
                            return false;
                        }

                        // If there aren't any blocked readers, just add the data to the queue,
                        // and let any waiting readers know that they should try to read it.
                        // We can only complete such waiters here under the lock if they run
                        // continuations asynchronously (otherwise the synchronous continuations
                        // could be invoked under the lock).  If we don't complete them here, we
                        // need to do so outside of the lock.
                        if (parent._blockedReaders.IsEmpty)
                        {
                            parent._items.Push(item);
                            waitingReadersTail = parent._waitingReadersTail;
                            if (waitingReadersTail is null)
                            {
                                return true;
                            }
                            parent._waitingReadersTail = null;
                        }
                        else
                        {
                            // There were blocked readers.  Grab one, and then complete it outside of the lock.
                            blockedReader = parent._blockedReaders.DequeueHead();
                        }
                    }

                    if (blockedReader != null)
                    {
                        // Complete the reader.  It's possible the reader was canceled, in which
                        // case we loop around to try everything again.
                        if (blockedReader.TrySetResult(item))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // Wake up all of the waiters.  Since we've released the lock, it's possible
                        // we could cause some spurious wake-ups here, if we tell a waiter there's
                        // something available but all data has already been removed.  It's a benign
                        // race condition, though, as consumers already need to account for such things.
                        ChannelUtilities.WakeUpWaiters(ref waitingReadersTail, result: true);
                        return true;
                    }
                }
            }

            public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken)
            {
                Exception? doneWriting = _parent._doneWriting;
                return
                    cancellationToken.IsCancellationRequested ? new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken)) :
                    doneWriting is null ? new ValueTask<bool>(true) : // Unsorted writing can always be done if we haven't completed
                    doneWriting != ChannelUtilities.s_doneWritingSentinel ? new ValueTask<bool>(Task.FromException<bool>(doneWriting)) :
                    default;
            }

            public override ValueTask WriteAsync(T item, CancellationToken cancellationToken) =>
                cancellationToken.IsCancellationRequested ? new ValueTask(Task.FromCanceled(cancellationToken)) :
                TryWrite(item) ? default :
                new ValueTask(Task.FromException(ChannelUtilities.CreateInvalidCompletionException(_parent._doneWriting)));

            /// <summary>Gets the number of items in the channel. This should only be used by the debugger.</summary>
            private int ItemsCountForDebugger => _parent._items.Count;
        }
    }
}
