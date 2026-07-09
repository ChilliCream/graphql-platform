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
    /// <returns>
    /// An opaque token string in the format <c>"provider:value"</c> that can be used to cancel
    /// the scheduled message.
    /// </returns>
    ValueTask<string> PersistAsync(
        MessageEnvelope envelope,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a scheduled message using the opaque token returned by <see cref="PersistAsync"/>.
    /// </summary>
    /// <param name="token">The opaque scheduling token returned by a prior <see cref="PersistAsync"/> call.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the message was cancelled; <c>false</c> if not found or already dispatched.</returns>
    ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken);
}
