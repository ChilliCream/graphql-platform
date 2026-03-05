using System.Collections.Immutable;
using Mocha.Middlewares;

namespace Mocha.Transport.InMemory;

/// <summary>
/// Base class for a binding that routes messages from a source topic to a destination resource
/// within the in-memory topology.
/// </summary>
public abstract class InMemoryBinding : TopologyResource<InMemoryBindingConfiguration>, IInMemoryResource
{
    /// <summary>
    /// Gets the source topic from which this binding receives messages.
    /// </summary>
    public InMemoryTopic Source { get; protected set; } = null!;

    internal void SetSource(InMemoryTopic source)
    {
        Source = source;
    }

    /// <summary>
    /// Forwards a message envelope to the binding's destination resource.
    /// </summary>
    /// <param name="envelope">The message envelope to forward.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the message has been accepted by the destination.</returns>
    public abstract ValueTask SendAsync(MessageEnvelope envelope, CancellationToken cancellationToken);
}
