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
    /// Persists the message envelope from the dispatch context to the scheduled message store
    /// for future delivery at <see cref="MessageEnvelope.ScheduledTime"/>.
    /// </summary>
    /// <param name="context">
    /// The dispatch context. <see cref="IDispatchContext.Envelope"/> is non-null when the
    /// scheduling middleware invokes this method, and <see cref="MessageEnvelope.ScheduledTime"/>
    /// is set on the envelope.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the persistence operation.</param>
    /// <returns>
    /// An opaque token string in the format <c>"provider:value"</c> that can be used to cancel
    /// the scheduled message.
    /// </returns>
    ValueTask<string> PersistAsync(
        IDispatchContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a scheduled message using the opaque token returned by <see cref="PersistAsync"/>.
    /// </summary>
    /// <param name="token">The opaque scheduling token returned by a prior <see cref="PersistAsync"/> call.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the message was cancelled; <c>false</c> if not found or already dispatched.</returns>
    ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken);
}
