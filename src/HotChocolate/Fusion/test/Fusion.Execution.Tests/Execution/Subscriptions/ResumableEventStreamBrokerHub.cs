using System.Buffers;
using System.Text;
using System.Threading.Channels;

namespace HotChocolate.Fusion.Subscriptions;

/// <summary>
/// An in-memory broker hub for tests that records every published event per topic and replays the
/// recorded history to new subscribers. When a subscriber supplies a resume cursor, the hub replays
/// only the events that follow the event carrying that cursor, which lets a test assert the
/// resumable stream continues after a previously received event.
/// </summary>
public sealed class ResumableEventStreamBrokerHub : IInMemoryEventStreamPublisher
{
    private readonly object _sync = new();
    private readonly Dictionary<string, List<RecordedEvent>> _history = [];
    private readonly Dictionary<string, List<ChannelWriter<EventMessage>>> _topics = [];
    private readonly Dictionary<string, string?> _lastCursors = [];

    public ValueTask PublishAsync(
        string topic,
        EventMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(topic);
        ArgumentNullException.ThrowIfNull(message);

        cancellationToken.ThrowIfCancellationRequested();

        ChannelWriter<EventMessage>[] subscribers;

        using (message)
        {
            lock (_sync)
            {
                if (!_history.TryGetValue(topic, out var recorded))
                {
                    recorded = [];
                    _history.Add(topic, recorded);
                }

                recorded.Add(RecordedEvent.Capture(message));

                subscribers = _topics.TryGetValue(topic, out var writers)
                    ? [.. writers]
                    : [];
            }

            for (var i = 0; i < subscribers.Length; i++)
            {
                if (!subscribers[i].TryWrite(RecordedEvent.Capture(message).ToMessage()))
                {
                    // The reader has completed; drop the event.
                }
            }
        }

        return ValueTask.CompletedTask;
    }

    internal void Subscribe(string topic, string? cursor, ChannelWriter<EventMessage> writer)
    {
        lock (_sync)
        {
            _lastCursors[topic] = cursor;

            ReplayHistory(topic, cursor, writer);

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

    internal int GetTotalSubscriberCount()
    {
        lock (_sync)
        {
            var total = 0;

            foreach (var subscribers in _topics.Values)
            {
                total += subscribers.Count;
            }

            return total;
        }
    }

    /// <summary>
    /// Gets the resume cursor that the most recent subscription forwarded for the given topic, or
    /// <c>null</c> when no subscriber supplied one (or the topic was never subscribed).
    /// </summary>
    internal string? GetLastSubscribedCursor(string topic)
    {
        lock (_sync)
        {
            return _lastCursors.TryGetValue(topic, out var cursor)
                ? cursor
                : null;
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

    private void ReplayHistory(string topic, string? cursor, ChannelWriter<EventMessage> writer)
    {
        if (string.IsNullOrEmpty(cursor)
            || !_history.TryGetValue(topic, out var recorded))
        {
            return;
        }

        var startIndex = 0;

        for (var i = 0; i < recorded.Count; i++)
        {
            if (recorded[i].CursorEquals(cursor))
            {
                startIndex = i + 1;
                break;
            }
        }

        for (var i = startIndex; i < recorded.Count; i++)
        {
            writer.TryWrite(recorded[i].ToMessage());
        }
    }

    private readonly struct RecordedEvent
    {
        private readonly byte[] _body;
        private readonly byte[] _cursor;

        private RecordedEvent(byte[] body, byte[] cursor)
        {
            _body = body;
            _cursor = cursor;
        }

        public static RecordedEvent Capture(EventMessage message)
            => new(message.Body.ToArray(), message.Cursor.ToArray());

        public bool CursorEquals(string cursor)
            => _cursor.Length > 0
                && Encoding.UTF8.GetString(_cursor).Equals(cursor, StringComparison.Ordinal);

        public EventMessage ToMessage()
        {
            var length = _body.Length + _cursor.Length;
            var owner = MemoryPool<byte>.Shared.Rent(length);

            _body.CopyTo(owner.Memory.Span);
            _cursor.CopyTo(owner.Memory.Span[_body.Length..]);

            return new EventMessage(
                owner,
                0.._body.Length,
                _body.Length..length);
        }
    }
}
