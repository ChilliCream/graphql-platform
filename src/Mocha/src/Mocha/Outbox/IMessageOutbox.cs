using Mocha.Middlewares;

namespace Mocha.Outbox;

/// <summary>
/// Stores outgoing message envelopes for later dispatch.
/// </summary>
public interface IMessageOutbox
{
    /// <summary>
    /// Stores or stages the specified message envelope in the outbox.
    /// </summary>
    /// <param name="envelope">The message envelope to persist, containing headers and payload.</param>
    /// <param name="cancellationToken">A token to cancel the persistence operation.</param>
    /// <returns>A task that completes when the envelope has been stored or staged.</returns>
    ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken);
}
