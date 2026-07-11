using Mocha.Middlewares;

namespace Mocha.Transport.InMemory;

/// <summary>
/// A binding that routes messages from a source topic to a destination topic.
/// </summary>
public sealed class InMemoryTopicBinding : InMemoryBinding
{
    /// <summary>
    /// Gets the destination topic that receives messages through this binding.
    /// </summary>
    public InMemoryTopic Destination { get; private set; } = null!;

    protected override void OnInitialize(InMemoryBindingConfiguration configuration) { }

    protected override void OnComplete(InMemoryBindingConfiguration configuration)
    {
        var builder = new UriBuilder(Topology.Address);
        builder.Path = Topology.Address.AbsolutePath.TrimEnd('/') + "/b/t/" + Source.Name + "/t/" + Destination.Name;
        Address = builder.Uri;
    }

    internal void SetDestination(InMemoryTopic destination)
    {
        Destination = destination;
    }

    public override ValueTask SendAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        return Destination.SendAsync(envelope, cancellationToken);
    }
}
