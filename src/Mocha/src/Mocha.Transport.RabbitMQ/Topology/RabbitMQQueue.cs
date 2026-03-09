using System.Collections.Immutable;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Represents a RabbitMQ queue entity with its configuration.
/// </summary>
public sealed class RabbitMQQueue : TopologyResource<RabbitMQQueueConfiguration>, IRabbitMQResource
{
    private ImmutableArray<RabbitMQBinding> _bindings = [];

    /// <summary>
    /// Gets the name of this queue as declared in RabbitMQ.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the cached string representation of the queue name, optimized for repeated use in publish operations.
    /// </summary>
    public CachedString CachedName { get; private set; } = null!;

    /// <summary>
    /// Gets the bindings attached to this queue from source exchanges.
    /// </summary>
    public ImmutableArray<RabbitMQBinding> Bindings => _bindings;

    /// <summary>
    /// Gets a value indicating whether this queue is automatically provisioned during topology setup.
    /// When <c>null</c>, the transport-level default is used.
    /// </summary>
    public bool? AutoProvision { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this queue survives broker restarts.
    /// </summary>
    public bool Durable { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this queue is exclusive to the connection that created it.
    /// </summary>
    public bool Exclusive { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this queue is automatically deleted when no longer in use.
    /// </summary>
    public bool AutoDelete { get; private set; }

    /// <summary>
    /// Gets the additional queue arguments for advanced configuration (e.g., x-message-ttl, x-max-length).
    /// </summary>
    public ImmutableDictionary<string, object?> Arguments { get; private set; } = ImmutableDictionary<string, object?>.Empty;

    protected override void OnInitialize(RabbitMQQueueConfiguration configuration)
    {
        Name = configuration.Name!;
        CachedName = new CachedString(Name);
        Durable = configuration.Durable ?? true;
        Exclusive = configuration.Exclusive ?? false;
        AutoDelete = configuration.AutoDelete ?? false;
        Arguments = configuration.Arguments?.ToImmutableDictionary(kv => kv.Key, kv => (object?)kv.Value) ?? ImmutableDictionary<string, object?>.Empty;
        AutoProvision = configuration.AutoProvision;
    }

    protected override void OnComplete(RabbitMQQueueConfiguration configuration)
    {
        var address = new UriBuilder(Topology.Address);
        address.Path = Path.Combine(address.Path, "q", configuration.Name!);
        Address = address.Uri;
    }

    internal void AddBinding(RabbitMQBinding binding)
    {
        ImmutableInterlocked.Update(ref _bindings, (current) => current.Add(binding));
    }

    // TODO: this is a bit lost here
    /// <summary>
    /// Declares this queue on the broker using the specified channel.
    /// </summary>
    /// <param name="channel">The RabbitMQ channel to use for declaring the queue.</param>
    /// <param name="cancellationToken">A token to cancel the provisioning operation.</param>
    public async Task ProvisionAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(
            CachedName.Value,
            Durable,
            Exclusive,
            AutoDelete,
            Arguments,
            cancellationToken: cancellationToken);
    }
}
