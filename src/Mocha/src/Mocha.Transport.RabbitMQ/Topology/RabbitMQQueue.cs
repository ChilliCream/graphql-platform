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

    /// <summary>
    /// Merges an incoming configuration into this entity, applying the 3.5 merge rules:
    /// declared non-null scalar wins; convention fills the rest; Arguments union per key;
    /// AutoProvision strengthens (true wins); origin upgrades convention to endpoint to declared.
    /// A shape conflict (both sides declared, different scalar value) throws
    /// <see cref="RabbitMQTopologyShapeConflictException"/>.
    /// </summary>
    /// <param name="configuration">The incoming configuration to merge from.</param>
    /// <exception cref="RabbitMQTopologyShapeConflictException">
    /// Thrown when both this entity and the incoming configuration carry explicitly declared
    /// values for the same scalar property and those values differ.
    /// </exception>
    internal void MergeFrom(RabbitMQQueueConfiguration configuration)
    {
        var incomingOrigin = configuration.Origin ?? TopologyOrigin.Convention;
        var existingIsDeclared = Origin == TopologyOrigin.Declared;
        var incomingIsDeclared = incomingOrigin == TopologyOrigin.Declared;

        // Scalar shape: declared non-null incoming wins; both declared + different = conflict.
        if (configuration.Durable is not null)
        {
            if (existingIsDeclared && incomingIsDeclared && Durable != configuration.Durable.Value)
            {
                throw new RabbitMQTopologyShapeConflictException("queue", Name, "Durable", Durable, configuration.Durable.Value);
            }

            Durable = configuration.Durable.Value;
        }

        if (configuration.Exclusive is not null)
        {
            if (existingIsDeclared && incomingIsDeclared && Exclusive != configuration.Exclusive.Value)
            {
                throw new RabbitMQTopologyShapeConflictException("queue", Name, "Exclusive", Exclusive, configuration.Exclusive.Value);
            }

            Exclusive = configuration.Exclusive.Value;
        }

        if (configuration.AutoDelete is not null)
        {
            if (existingIsDeclared && incomingIsDeclared && AutoDelete != configuration.AutoDelete.Value)
            {
                throw new RabbitMQTopologyShapeConflictException("queue", Name, "AutoDelete", AutoDelete, configuration.AutoDelete.Value);
            }

            AutoDelete = configuration.AutoDelete.Value;
        }

        // Arguments: union per key; incoming key wins on collision.
        if (configuration.Arguments is not null)
        {
            var builder = Arguments.ToBuilder();

            foreach (var (key, value) in configuration.Arguments)
            {
                builder[key] = value;
            }

            Arguments = builder.ToImmutable();
        }

        // AutoProvision: strengthen (true wins over null or false).
        if (AutoProvision is null)
        {
            AutoProvision = configuration.AutoProvision;
        }
        else if (configuration.AutoProvision == true)
        {
            AutoProvision = true;
        }

        // Origin: upgrade only, never downgrade.
        MergeOrigin(configuration);
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
