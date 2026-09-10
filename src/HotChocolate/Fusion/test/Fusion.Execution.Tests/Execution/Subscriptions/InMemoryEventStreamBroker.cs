using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace HotChocolate.Fusion.Subscriptions;

public sealed class InMemoryEventStreamBroker(InMemoryEventStreamBrokerHub hub) : IEventStreamBroker
{
    private readonly List<Channel<EventMessage>> _channels = [];
    private bool _disposed;

    public IAsyncEnumerable<EventMessage> SubscribeAsync(
        ISubscriptionFieldContext context,
        string[] topics,
        string? cursor,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(topics);
        ArgumentOutOfRangeException.ThrowIfZero(topics.Length);

        for (var i = 0; i < topics.Length; i++)
        {
            ArgumentException.ThrowIfNullOrEmpty(topics[i]);
        }

        if (!string.IsNullOrEmpty(cursor))
        {
            throw new InvalidEventMessageCursorException();
        }

        return SubscribeCoreAsync(topics, cancellationToken);
    }

    private async IAsyncEnumerable<EventMessage> SubscribeCoreAsync(
        string[] topics,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<EventMessage>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        lock (_channels)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _channels.Add(channel);
        }

        for (var i = 0; i < topics.Length; i++)
        {
            hub.Subscribe(topics[i], channel.Writer);
        }

        try
        {
            await foreach (var message in channel.Reader
                .ReadAllAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                yield return message;
            }
        }
        finally
        {
            for (var i = 0; i < topics.Length; i++)
            {
                hub.Unsubscribe(topics[i], channel.Writer);
            }

            lock (_channels)
            {
                _channels.Remove(channel);
            }

            channel.Writer.TryComplete();
            DisposeQueuedMessages(channel);
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        Channel<EventMessage>[] channels;

        lock (_channels)
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            _disposed = true;
            channels = [.. _channels];
            _channels.Clear();
        }

        foreach (var channel in channels)
        {
            channel.Writer.TryComplete();
            DisposeQueuedMessages(channel);
        }

        return ValueTask.CompletedTask;
    }

    private static void DisposeQueuedMessages(Channel<EventMessage> channel)
    {
        while (channel.Reader.TryRead(out var message))
        {
            message.Dispose();
        }
    }
}
