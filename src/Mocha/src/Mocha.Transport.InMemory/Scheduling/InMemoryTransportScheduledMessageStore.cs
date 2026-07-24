using System.Diagnostics.CodeAnalysis;
using Mocha.Middlewares;
using Mocha.Scheduling;

namespace Mocha.Transport.InMemory.Scheduling;

/// <summary>
/// An in-process <see cref="IScheduledMessageStore"/> for the in-memory transport. Holds scheduled
/// envelopes ordered by due time and signals a dedicated worker when the earliest due time changes.
/// </summary>
internal sealed class InMemoryTransportScheduledMessageStore(TimeProvider timeProvider)
    : IScheduledMessageStore, IDisposable
{
    /// <summary>
    /// The opaque-token prefix identifying scheduling tokens owned by this store.
    /// </summary>
    public const string TokenPrefix = "in-memory-transport:";

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    private readonly SortedSet<ScheduledEntry> _ordered = new(ScheduledEntryComparer.Instance);
    private readonly Dictionary<Guid, ScheduledEntry> _byId = [];
    private readonly MessageBusSchedulerSignal _signal = new(timeProvider);

    /// <summary>
    /// Gets the dedicated signal the worker waits on. Not the shared scheduler singleton.
    /// </summary>
    public ISchedulerSignal Signal => _signal;

    public ValueTask<string> PersistAsync(IDispatchContext context, CancellationToken cancellationToken)
    {
        if (context.Envelope is not { } envelope)
        {
            throw new InvalidOperationException(
                "The dispatch context does not carry a serialized envelope.");
        }

        if (context.ScheduledTime is not { } scheduledTime)
        {
            throw new InvalidOperationException(
                "The dispatch context does not carry a scheduled time.");
        }

        var token = Add(envelope, scheduledTime);

        return new ValueTask<string>(token);
    }

    /// <summary>
    /// Deep-copies the envelope and stores it under a new token. The stored copy is independent of
    /// the caller's envelope, so mutating the original body or headers afterward does not affect it.
    /// </summary>
    internal string Add(MessageEnvelope envelope, DateTimeOffset scheduledTime)
    {
        var id = Guid.NewGuid();
        var copy = new MessageEnvelope(envelope) { Body = envelope.Body.ToArray() };
        var entry = new ScheduledEntry(id, copy, scheduledTime);

        lock (_lock)
        {
            _ordered.Add(entry);
            _byId[id] = entry;
        }

        _signal.Notify(scheduledTime);

        return TokenPrefix + id.ToString("D");
    }

    public ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
    {
        if (!token.StartsWith(TokenPrefix, StringComparison.Ordinal)
            || !Guid.TryParse(token.AsSpan(TokenPrefix.Length), out var id))
        {
            return new ValueTask<bool>(false);
        }

        lock (_lock)
        {
            if (_byId.Remove(id, out var entry))
            {
                _ordered.Remove(entry);

                return new ValueTask<bool>(true);
            }
        }

        return new ValueTask<bool>(false);
    }

    /// <summary>
    /// Atomically removes and returns the earliest entry whose scheduled time is at or before
    /// <paramref name="now"/>, if any.
    /// </summary>
    public bool TryTakeDue(DateTimeOffset now, [NotNullWhen(true)] out ScheduledEntry? entry)
    {
        lock (_lock)
        {
            if (_ordered.Count > 0)
            {
                var min = _ordered.Min!;

                if (min.ScheduledTime <= now)
                {
                    _ordered.Remove(min);
                    _byId.Remove(min.Id);
                    entry = min;

                    return true;
                }
            }
        }

        entry = null;

        return false;
    }

    /// <summary>
    /// Gets the earliest scheduled time currently held, or <c>null</c> if the store is empty.
    /// </summary>
    public DateTimeOffset? NextDueTime()
    {
        lock (_lock)
        {
            return _ordered.Count > 0 ? _ordered.Min!.ScheduledTime : null;
        }
    }

    public void Dispose()
    {
        _signal.Dispose();
    }
}

/// <summary>
/// A scheduled envelope held by the in-memory store.
/// </summary>
internal sealed class ScheduledEntry(Guid id, MessageEnvelope envelope, DateTimeOffset scheduledTime)
{
    public Guid Id { get; } = id;

    public MessageEnvelope Envelope { get; } = envelope;

    public DateTimeOffset ScheduledTime { get; } = scheduledTime;
}

/// <summary>
/// Orders entries by scheduled time, then by ID, so distinct entries with the same due time coexist
/// in the <see cref="SortedSet{T}"/> (a total order is required, otherwise adds are dropped).
/// </summary>
internal sealed class ScheduledEntryComparer : IComparer<ScheduledEntry>
{
    public static readonly ScheduledEntryComparer Instance = new();

    public int Compare(ScheduledEntry? x, ScheduledEntry? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var byTime = x.ScheduledTime.CompareTo(y.ScheduledTime);

        return byTime != 0 ? byTime : x.Id.CompareTo(y.Id);
    }
}
