using System.Collections.Immutable;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Represents a RabbitMQ exchange entity with its configuration.
/// </summary>
public sealed class RabbitMQExchange : TopologyResource<RabbitMQExchangeConfiguration>
{
    private ImmutableArray<RabbitMQBinding> _bindings = [];

    /// <summary>
    /// Gets the name of this exchange as declared in RabbitMQ.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the cached string representation of the exchange name, optimized for repeated use in publish operations.
    /// </summary>
    public CachedString CachedName { get; private set; } = null!;

    /// <summary>
    /// Gets a value indicating whether this exchange is automatically provisioned during topology setup.
    /// When <c>null</c>, the transport-level default is used.
    /// </summary>
    public bool? AutoProvision { get; private set; }

    /// <summary>
    /// Gets the exchange type (e.g., "direct", "fanout", "topic", "headers").
    /// </summary>
    public string Type { get; private set; } = null!;

    /// <summary>
    /// Gets a value indicating whether this exchange survives broker restarts.
    /// </summary>
    public bool Durable { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this exchange is automatically deleted when no longer in use.
    /// </summary>
    public bool AutoDelete { get; private set; }

    /// <summary>
    /// Gets the additional exchange arguments for advanced configuration (e.g., alternate-exchange).
    /// </summary>
    public ImmutableDictionary<string, object?> Arguments { get; private set; } = ImmutableDictionary<string, object?>.Empty;

    /// <summary>
    /// Gets the bindings attached to this exchange (both outgoing and incoming).
    /// </summary>
    public ImmutableArray<RabbitMQBinding> Bindings => _bindings;

    protected override void OnInitialize(RabbitMQExchangeConfiguration configuration)
    {
        Name = configuration.Name;
        CachedName = new CachedString(Name);
        Durable = configuration.Durable ?? true;
        Type = configuration.Type ?? "fanout";
        AutoDelete = configuration.AutoDelete ?? false;
        Arguments = configuration.Arguments?.ToImmutableDictionary(kv => kv.Key, kv => (object?)kv.Value) ?? ImmutableDictionary<string, object?>.Empty;
        AutoProvision = configuration.AutoProvision;
    }

    protected override void OnComplete(RabbitMQExchangeConfiguration configuration)
    {
        var address = new UriBuilder(Topology.Address);
        address.Path = Path.Combine(address.Path, "e", configuration.Name);
        Address = address.Uri;
    }

    internal void AddBinding(RabbitMQBinding binding)
    {
        ImmutableInterlocked.Update(ref _bindings, (current) => current.Add(binding));
    }

    /// <summary>
    /// Declares this exchange on the broker using the specified channel.
    /// </summary>
    /// <param name="channel">The RabbitMQ channel to use for declaring the exchange.</param>
    /// <param name="cancellationToken">A token to cancel the provisioning operation.</param>
    public async Task ProvisionAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            CachedName.Value,
            Type,
            Durable,
            AutoDelete,
            Arguments,
            cancellationToken: cancellationToken);
    }
}
