using System.Diagnostics.CodeAnalysis;
using Mocha.Middlewares;
using Mocha.Scheduling;

namespace Mocha.Transport.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IScheduledMessageStore"/> that holds scheduled message
/// envelopes in process memory until they are due for delivery. State is not durable: scheduled
/// messages are lost when the process stops.
/// </summary>
public sealed class InMemoryScheduledMessageStore(ISchedulerSignal signal) : IScheduledMessageStore
{
    private const string TokenPrefix = "in-memory-scheduler:";
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly Dictionary<Guid, Entry> _entries = [];

    /// <inheritdoc />
    public ValueTask<string> PersistAsync(
        MessageEnvelope envelope,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        lock (_lock)
        {
            _entries[id] = new Entry(envelope, scheduledTime);
        }

        signal.Notify(scheduledTime);

        return new ValueTask<string>($"{TokenPrefix}{id:D}");
    }

    /// <inheritdoc />
    public ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
    {
        var value = token.StartsWith(TokenPrefix, StringComparison.Ordinal)
            ? token[TokenPrefix.Length..]
            : token;

        if (!Guid.TryParse(value, out var id))
        {
            return new ValueTask<bool>(false);
        }

        lock (_lock)
        {
            return new ValueTask<bool>(_entries.Remove(id));
        }
    }

    /// <summary>
    /// Removes and returns the earliest entry whose scheduled time is at or before <paramref name="now"/>.
    /// </summary>
    public bool TryTakeDue(DateTimeOffset now, [NotNullWhen(true)] out MessageEnvelope? envelope)
    {
        lock (_lock)
        {
            var foundId = Guid.Empty;
            var foundTime = DateTimeOffset.MaxValue;
            MessageEnvelope? foundEnvelope = null;

            foreach (var (id, entry) in _entries)
            {
                if (entry.ScheduledTime <= now && entry.ScheduledTime < foundTime)
                {
                    foundId = id;
                    foundTime = entry.ScheduledTime;
                    foundEnvelope = entry.Envelope;
                }
            }

            if (foundEnvelope is null)
            {
                envelope = null;
                return false;
            }

            _entries.Remove(foundId);
            envelope = foundEnvelope;
            return true;
        }
    }

    /// <summary>
    /// Returns the earliest scheduled time across all pending entries, or <c>null</c> when empty.
    /// </summary>
    public DateTimeOffset? NextDueTime()
    {
        lock (_lock)
        {
            DateTimeOffset? next = null;

            foreach (var entry in _entries.Values)
            {
                if (next is null || entry.ScheduledTime < next)
                {
                    next = entry.ScheduledTime;
                }
            }

            return next;
        }
    }

    private readonly record struct Entry(MessageEnvelope Envelope, DateTimeOffset ScheduledTime);
}
