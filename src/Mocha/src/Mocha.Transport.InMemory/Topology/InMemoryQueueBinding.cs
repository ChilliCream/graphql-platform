using Mocha.Middlewares;

namespace Mocha.Transport.InMemory;

/// <summary>
/// A binding that routes messages from a source topic to a destination queue.
/// </summary>
public sealed class InMemoryQueueBinding : InMemoryBinding
{
    /// <summary>
    /// Gets the destination queue that receives messages through this binding.
    /// </summary>
    public InMemoryQueue Destination { get; private set; } = null!;

    protected override void OnInitialize(InMemoryBindingConfiguration configuration) { }

    protected override void OnComplete(InMemoryBindingConfiguration configuration)
    {
        var builder = new UriBuilder(Topology.Address);
        builder.Path = Path.Combine(builder.Path, "b", "t", Source.Name, "q", Destination.Name);
        Address = builder.Uri;
    }

    internal void SetDestination(InMemoryQueue destination)
    {
        Destination = destination;
    }

    public override ValueTask SendAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        return Destination.SendAsync(envelope, cancellationToken);
    }
}
