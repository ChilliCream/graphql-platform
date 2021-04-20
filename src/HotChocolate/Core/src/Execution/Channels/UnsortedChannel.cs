using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Channels
{
    /// <summary>Provides a buffered channel of Unsorted capacity.</summary>    
    internal sealed partial class UnsortedChannel<T> 
    {
        /// <summary>Task that indicates the channel has completed.</summary>
        private readonly TaskCompletionSource<bool> _completion;
        /// <summary>The items in the channel.</summary>
        private readonly BlockingStack<T> _items = new BlockingStack<T>();
        /// <summary>Readers blocked reading from the channel.</summary>
        private readonly Deque<AsyncOperation<T>> _blockedReaders = new Deque<AsyncOperation<T>>();
        /// <summary>Whether to force continuations to be executed asynchronously from producer writes.</summary>
        private readonly bool _runContinuationsAsynchronously;
        private readonly UnsortedChannelReader _reader;
        private readonly UnsortedChannelWriter _writer;

        /// <summary>Readers waiting for a notification that data is available.</summary>
        private AsyncOperation<bool>? _waitingReadersTail;
        /// <summary>Set to non-null once Complete has been called.</summary>
        private Exception? _doneWriting;

        /// <summary>Initialize the channel.</summary>
        internal UnsortedChannel(bool runContinuationsAsynchronously)
        {
            _runContinuationsAsynchronously = runContinuationsAsynchronously;
            _completion = new TaskCompletionSource<bool>(
                runContinuationsAsynchronously
                    ? TaskCreationOptions.RunContinuationsAsynchronously
                    : TaskCreationOptions.None);
            _reader = new UnsortedChannelReader(this);
            _writer = new UnsortedChannelWriter(this);
        }

        /// <summary>Gets the object used to synchronize access to all state on this instance.</summary>
        private object SyncObj => _items;

        public bool IsEmpty => _items.IsEmpty;

        public bool IsIdle => _completion.Task.IsCompleted || _items.IsEmpty;

        public Task WaitTillIdle(CancellationToken? ctx = null)
        {
            var completion = _completion.Task;
            if (completion.IsCompleted)
            {
                return completion;
            }

            var itemsEmpty = _items.WaitTillEmpty(ctx);
            if (itemsEmpty.IsCompleted)
            {
                return itemsEmpty; 
            }

            return Task.WhenAny(itemsEmpty, completion);
        }

        public bool TryRead([MaybeNullWhen(false)] out T item)
        {
            return _reader.TryRead(out item);
        }

        public bool TryRead(Action<T> receiver)
        {
            return _reader.TryRead(receiver);
        }

        public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
        {
            return _reader.WaitToReadAsync(cancellationToken);
        }

        public bool TryWrite(T item)
        {
            return _writer.TryWrite(item);
        }

        public void Complete(Exception? error = null)
        {
            _writer.Complete(error);
        }

        [Conditional("DEBUG")]
        private void AssertInvariants()
        {
            Debug.Assert(SyncObj != null, "The sync obj must not be null.");
            Debug.Assert(Monitor.IsEntered(SyncObj), "Invariants can only be validated while holding the lock.");

            if (!_items.IsEmpty)
            {
                if (_runContinuationsAsynchronously)
                {
                    Debug.Assert(_blockedReaders.IsEmpty, "There's data available, so there shouldn't be any blocked readers.");
                    Debug.Assert(_waitingReadersTail is null, "There's data available, so there shouldn't be any waiting readers.");
                }
                Debug.Assert(!_completion.Task.IsCompleted, "We still have data available, so shouldn't be completed.");
            }
            if ((!_blockedReaders.IsEmpty || _waitingReadersTail != null) && _runContinuationsAsynchronously)
            {
                Debug.Assert(_items.IsEmpty, "There are blocked/waiting readers, so there shouldn't be any data available.");
            }
            if (_completion.Task.IsCompleted)
            {
                Debug.Assert(_doneWriting != null, "We're completed, so we must be done writing.");
            }
        }

        /// <summary>Gets the number of items in the channel.  This should only be used by the debugger.</summary>
        private int ItemsCountForDebugger => _items.Count;

        /// <summary>Report if the channel is closed or not. This should only be used by the debugger.</summary>
        private bool ChannelIsClosedForDebugger => _doneWriting != null;
    }
}
