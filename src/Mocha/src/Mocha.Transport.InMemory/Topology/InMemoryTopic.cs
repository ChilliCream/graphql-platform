using System.Collections.Immutable;
using Mocha.Middlewares;

namespace Mocha.Transport.InMemory;

/// <summary>
/// Represents a InMemory topic entity with its configuration.
/// </summary>
public sealed class InMemoryTopic : TopologyResource<InMemoryTopicConfiguration>, IInMemoryResource
{
    private ImmutableArray<InMemoryBinding> _bindings = [];

    /// <summary>
    /// Gets the name that uniquely identifies this topic within the topology.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the bindings attached to this topic that fan out messages to queues or other topics.
    /// </summary>
    public ImmutableArray<InMemoryBinding> Bindings => _bindings;

    protected override void OnInitialize(InMemoryTopicConfiguration configuration)
    {
        Name = configuration.Name;
    }

    protected override void OnComplete(InMemoryTopicConfiguration configuration)
    {
        var address = new UriBuilder(Topology.Address);
        address.Path = Path.Combine(address.Path, "e", configuration.Name);
        Address = address.Uri;
    }

    internal void AddBinding(InMemoryBinding binding)
    {
        ImmutableInterlocked.Update(ref _bindings, (current) => current.Add(binding));
    }

    /// <summary>
    /// Publishes a message envelope to all bindings attached to this topic, traversing
    /// topic-to-topic bindings recursively while preventing cycles.
    /// </summary>
    /// <param name="envelope">The message envelope to publish.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the envelope has been delivered to all reachable destinations.</returns>
    public async ValueTask SendAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        var alreadyPublished = new HashSet<InMemoryTopic>();
        await SendAsync(this, envelope, alreadyPublished, cancellationToken);
    }

    private async ValueTask SendAsync(
        InMemoryTopic topic,
        MessageEnvelope envelope,
        HashSet<InMemoryTopic> topics,
        CancellationToken cancellationToken)
    {
        if (!topics.Add(topic))
        {
            return;
        }

        foreach (var binding in topic._bindings)
        {
            switch (binding)
            {
                case InMemoryQueueBinding queueBinding:
                    await queueBinding.SendAsync(envelope, cancellationToken);
                    break;
                case InMemoryTopicBinding topicBinding:
                    await SendAsync(topicBinding.Destination, envelope, topics, cancellationToken);
                    break;
            }
        }
    }
}
