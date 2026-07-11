using Mocha.Middlewares;

namespace Mocha.Outbox;

/// <summary>
/// Defines the contract for persisting outgoing message envelopes to a durable outbox store.
/// </summary>
/// <remarks>
/// Implementations are responsible for transactionally storing envelopes so they can be
/// relayed to the transport at a later time, providing at-least-once delivery guarantees.
/// </remarks>
public interface IMessageOutbox
{
    /// <summary>
    /// Persists the specified message envelope to the outbox store.
    /// </summary>
    /// <param name="envelope">The message envelope to persist, containing headers and payload.</param>
    /// <param name="cancellationToken">A token to cancel the persistence operation.</param>
    /// <returns>A value task that completes when the envelope has been durably stored.</returns>
    ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken);
}
