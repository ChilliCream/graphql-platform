using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions.InMemory;

internal sealed class EventTopic<TMessage> : IEventTopic
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Channel<EventMessageEnvelope<TMessage>> _incoming =
        Channel.CreateUnbounded<EventMessageEnvelope<TMessage>>();
    private readonly List<Channel<EventMessageEnvelope<TMessage>>> _outgoing = new();
    private bool _disposed;

    public event EventHandler<EventArgs>? Unsubscribed;

    public EventTopic(string topic)
    {
        Topic = topic;
        BeginProcessing();
    }

    public string Topic { get; }

    public void TryWrite(TMessage message)
        => _incoming.Writer.TryWrite(new EventMessageEnvelope<TMessage>(message));

    public async ValueTask CompleteAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);

        if (_outgoing.Count > 0)
        {
            for (var i = 0; i < _outgoing.Count; i++)
            {
                _outgoing[i].Writer.TryWrite(new EventMessageEnvelope<TMessage>());
            }
            _outgoing.Clear();
        }

        Dispose();
    }

    public async ValueTask<InMemorySourceStream<TMessage>> SubscribeAsync(
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var channel = Channel.CreateUnbounded<EventMessageEnvelope<TMessage>>();
            var stream = new InMemorySourceStream<TMessage>(channel);
            _outgoing.Add(channel);

            return stream;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void BeginProcessing()
        => Task.Factory.StartNew(
            async () => await ProcessMessages().ConfigureAwait(false),
            TaskCreationOptions.LongRunning);

    private async Task ProcessMessages()
    {
        while (!_incoming.Reader.Completion.IsCompleted)
        {
            if (await _incoming.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    var message = await _incoming.Reader.ReadAsync().ConfigureAwait(false);
                    var closedChannel = ImmutableHashSet<Channel<EventMessageEnvelope<TMessage>>>.Empty;
                    var outgoingCount = _outgoing.Count;

                    for (var i = 0; i < outgoingCount; i++)
                    {
                        var channel = _outgoing[i];

                        // close outgoing channel if related subscription is completed
                        // (no reader available)
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

                    // raises unsubscribed event only once when all outgoing channels
                    // (subscriptions) are removed
                    if (_outgoing.Count == 0 && outgoingCount > 0)
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
        => Task.Run(() => Unsubscribed?.Invoke(this, EventArgs.Empty));

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                for (var i = 0; i < _outgoing.Count; i++)
                {
                    _outgoing[i].Writer.TryComplete();
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
