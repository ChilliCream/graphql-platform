using Mocha.Middlewares;

namespace Mocha.Transport.InMemory;

/// <summary>
/// Represents an in-memory topology resource (topic or queue) that can accept message envelopes.
/// </summary>
public interface IInMemoryResource
{
    /// <summary>
    /// Sends a message envelope to this resource for delivery or further routing.
    /// </summary>
    /// <param name="envelope">The message envelope to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the resource has accepted the envelope.</returns>
    ValueTask SendAsync(MessageEnvelope envelope, CancellationToken cancellationToken);
}
