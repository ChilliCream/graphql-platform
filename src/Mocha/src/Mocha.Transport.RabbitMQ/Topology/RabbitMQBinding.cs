using System.Collections.Immutable;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Base class for RabbitMQ bindings that route messages from a source exchange to a destination (queue or exchange).
/// </summary>
public abstract class RabbitMQBinding : TopologyResource<RabbitMQBindingConfiguration>, IRabbitMQResource
{
    /// <summary>
    /// Gets the source exchange from which messages are routed through this binding.
    /// </summary>
    public RabbitMQExchange Source { get; protected set; } = null!;

    /// <summary>
    /// Gets a value indicating whether this binding is automatically provisioned during topology setup.
    /// When <c>null</c>, the transport-level default is used.
    /// </summary>
    public bool? AutoProvision { get; protected set; }

    /// <summary>
    /// Gets the routing key pattern used to filter messages passing through this binding.
    /// </summary>
    public string RoutingKey { get; protected set; } = null!;

    /// <summary>
    /// Gets the additional binding arguments used for advanced routing (e.g., headers exchange matching).
    /// </summary>
    public ImmutableDictionary<string, object?> Arguments { get; protected set; } = ImmutableDictionary<string, object?>.Empty;

    internal void SetSource(RabbitMQExchange source)
    {
        Source = source;
    }

    /// <summary>
    /// Declares this binding on the broker using the specified channel.
    /// </summary>
    /// <param name="channel">The RabbitMQ channel to use for declaring the binding.</param>
    /// <param name="cancellationToken">A token to cancel the provisioning operation.</param>
    public abstract Task ProvisionAsync(IChannel channel, CancellationToken cancellationToken);
}

/// <summary>
/// Represents a binding that routes messages from a source exchange to a destination exchange.
/// </summary>
public sealed class RabbitMQExchangeBinding : RabbitMQBinding
{
    /// <summary>
    /// Gets the destination exchange that receives messages routed through this binding.
    /// </summary>
    public RabbitMQExchange Destination { get; private set; } = null!;

    protected override void OnInitialize(RabbitMQBindingConfiguration configuration)
    {
        RoutingKey = configuration.RoutingKey ?? string.Empty;
        Arguments = configuration.Arguments?.ToImmutableDictionary(kv => kv.Key, kv => (object?)kv.Value) ?? ImmutableDictionary<string, object?>.Empty;
        AutoProvision = configuration.AutoProvision;
    }

    protected override void OnComplete(RabbitMQBindingConfiguration configuration)
    {
        var builder = new UriBuilder(Topology.Address);
        builder.Path = Path.Combine(builder.Path, "b", "e", Source.Name, "e", Destination.Name);
        Address = builder.Uri;
    }

    internal void SetDestination(RabbitMQExchange destination)
    {
        Destination = destination;
    }

    /// <inheritdoc />
    public override async Task ProvisionAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeBindAsync(
            Destination.Name,
            Source.Name,
            RoutingKey,
            Arguments,
            cancellationToken: cancellationToken);
    }
}

/// <summary>
/// Represents a binding that routes messages from a source exchange to a destination queue.
/// </summary>
public sealed class RabbitMQQueueBinding : RabbitMQBinding
{
    /// <summary>
    /// Gets the destination queue that receives messages routed through this binding.
    /// </summary>
    public RabbitMQQueue Destination { get; private set; } = null!;

    protected override void OnInitialize(RabbitMQBindingConfiguration configuration)
    {
        RoutingKey = configuration.RoutingKey ?? string.Empty;
        Arguments = configuration.Arguments?.ToImmutableDictionary(kv => kv.Key, kv => (object?)kv.Value) ?? ImmutableDictionary<string, object?>.Empty;
        AutoProvision = configuration.AutoProvision;
    }

    protected override void OnComplete(RabbitMQBindingConfiguration configuration)
    {
        var builder = new UriBuilder(Topology.Address);
        builder.Path = Path.Combine(builder.Path, "b", "e", Source.Name, "q", Destination.Name);
        Address = builder.Uri;
    }

    internal void SetDestination(RabbitMQQueue destination)
    {
        Destination = destination;
    }

    /// <inheritdoc />
    public override async Task ProvisionAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.QueueBindAsync(
            Destination.Name,
            Source.Name,
            RoutingKey,
            Arguments,
            cancellationToken: cancellationToken);
    }
}
