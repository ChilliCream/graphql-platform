using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions.InMemory
{
    internal sealed class EventTopic<TMessage>
        : IEventTopic
            , IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Channel<TMessage> _incoming = Channel.CreateUnbounded<TMessage>();
        private readonly List<Channel<TMessage>> _outgoing = new List<Channel<TMessage>>();
        private bool _disposed;

        public event EventHandler<EventArgs>? Unsubscribed;

        public EventTopic()
        {
            BeginProcessing();
        }

        public void TryWrite(TMessage message)
        {
            _incoming.Writer.TryWrite(message);
        }

        public async ValueTask CompleteAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            if (_outgoing.Count > 0)
            {
                for (var i = 0; i < _outgoing.Count; i++)
                {
                    _outgoing[i].Writer.TryComplete();
                }
                _outgoing.Clear();
            }

            Dispose();
        }

        public async ValueTask<InMemorySourceStream<TMessage>> SubscribeAsync(
            CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var channel = Channel.CreateUnbounded<TMessage>();
                var stream = new InMemorySourceStream<TMessage>(channel);
                _outgoing.Add(channel);

                return stream;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> TryClose()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_outgoing.Count > 0)
                {
                    ImmutableHashSet<Channel<TMessage>> closedChannel =
                        ImmutableHashSet<Channel<TMessage>>.Empty;

                    for (var i = 0; i < _outgoing.Count; i++)
                    {
                        if (_outgoing[i].Reader.Completion.IsCompleted)
                        {
                            closedChannel = closedChannel.Add(_outgoing[i]);
                        }
                    }

                    _outgoing.RemoveAll(c => closedChannel.Contains(c));
                }

                if (_outgoing.Count == 0)
                {
                    Dispose();
                    return true;
                }

                return false;
            }
            finally
            {
                if (!_disposed)
                {
                    _semaphore.Release();
                }
            }
        }

        private void BeginProcessing()
        {
            Task.Run(async () => await ProcessMessages().ConfigureAwait(false));
        }

        private async Task ProcessMessages()
        {
            while (!_incoming.Reader.Completion.IsCompleted)
            {
                if (await _incoming.Reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    await _semaphore.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        TMessage message =
                            await _incoming.Reader.ReadAsync().ConfigureAwait(false);

                        ImmutableHashSet<Channel<TMessage>> closedChannel =
                            ImmutableHashSet<Channel<TMessage>>.Empty;

                        for (var i = 0; i < _outgoing.Count; i++)
                        {
                            Channel<TMessage> channel = _outgoing[i];

                            if (!channel.Writer.TryWrite(message)
                                && channel.Reader.Completion.IsCompleted)
                            {
                                closedChannel = closedChannel.Add(channel);
                            }
                        }

                        if (closedChannel.Count > 0)
                        {
                            _outgoing.RemoveAll(c => closedChannel.Contains(c));
                        }

                        if (_outgoing.Count == 0)
                        {
                            RaiseUnsubscribedEvent();
                        }
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
            }
        }


        private void RaiseUnsubscribedEvent()
        {
            Task.Run(() => Unsubscribed?.Invoke(this, EventArgs.Empty));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    for (int i = 0; i < _outgoing.Count; i++)
                    {
                        _outgoing[i].Writer.TryComplete();
                        ;
                    }
                    _outgoing.Clear();
                }
                finally
                {
                    _incoming.Writer.TryComplete();
                    _semaphore.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
