using Mocha.Middlewares;

namespace Mocha.Scheduling;

/// <summary>
/// Defines the contract for persisting outgoing message envelopes to a durable scheduled message store.
/// </summary>
/// <remarks>
/// Implementations are responsible for transactionally storing envelopes so they can be
/// dispatched at the specified scheduled time, providing at-least-once delivery guarantees.
/// </remarks>
public interface IScheduledMessageStore
{
    /// <summary>
    /// Persists the specified message envelope to the scheduled message store for future delivery.
    /// </summary>
    /// <param name="envelope">The message envelope to persist, containing headers and payload.</param>
    /// <param name="scheduledTime">The time at which the message should be dispatched.</param>
    /// <param name="cancellationToken">A token to cancel the persistence operation.</param>
    /// <returns>A value task that completes when the envelope has been durably stored.</returns>
    ValueTask PersistAsync(
        MessageEnvelope envelope,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken);
}
