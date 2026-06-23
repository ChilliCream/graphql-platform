using System.Buffers;
using System.Threading.Channels;

namespace HotChocolate.Fusion.Subscriptions;

public sealed class InMemoryEventStreamBrokerHub : IInMemoryEventStreamPublisher
{
    private readonly object _sync = new();
    private readonly Dictionary<string, List<ChannelWriter<EventMessage>>> _topics = [];

    public ValueTask PublishAsync(
        string topic,
        EventMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(topic);
        ArgumentNullException.ThrowIfNull(message);

        cancellationToken.ThrowIfCancellationRequested();

        ChannelWriter<EventMessage>[] subscribers;

        lock (_sync)
        {
            subscribers = _topics.TryGetValue(topic, out var writers)
                ? [.. writers]
                : [];
        }

        if (subscribers.Length == 0)
        {
            message.Dispose();
            return ValueTask.CompletedTask;
        }

        for (var i = 0; i < subscribers.Length; i++)
        {
            var published = i == subscribers.Length - 1
                ? message
                : Clone(message);

            if (!subscribers[i].TryWrite(published))
            {
                published.Dispose();
            }
        }

        return ValueTask.CompletedTask;
    }

    internal void Subscribe(string topic, ChannelWriter<EventMessage> writer)
    {
        lock (_sync)
        {
            if (!_topics.TryGetValue(topic, out var subscribers))
            {
                subscribers = [];
                _topics.Add(topic, subscribers);
            }

            subscribers.Add(writer);
        }
    }

    internal int GetSubscriberCount(string topic)
    {
        lock (_sync)
        {
            return _topics.TryGetValue(topic, out var subscribers)
                ? subscribers.Count
                : 0;
        }
    }

    internal void Unsubscribe(string topic, ChannelWriter<EventMessage> writer)
    {
        lock (_sync)
        {
            if (!_topics.TryGetValue(topic, out var subscribers))
            {
                return;
            }

            subscribers.Remove(writer);

            if (subscribers.Count == 0)
            {
                _topics.Remove(topic);
            }
        }
    }

    private static EventMessage Clone(EventMessage message)
    {
        var body = message.Body;
        var cursor = message.Cursor;
        var length = body.Length + cursor.Length;
        var owner = MemoryPool<byte>.Shared.Rent(length);

        body.CopyTo(owner.Memory.Span);
        cursor.CopyTo(owner.Memory.Span[body.Length..]);

        return new EventMessage(
            owner,
            0..body.Length,
            body.Length..length);
    }
}
