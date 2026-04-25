using System.Text.Json;

namespace Mocha.Resources;

/// <summary>
/// <see cref="MochaResource"/> describing a transport binding/subscription that routes messages
/// from a source resource (typically an exchange or topic) to a destination resource (queue,
/// exchange, or topic).
/// </summary>
/// <remarks>
/// Intentionally <c>public</c> so transport packages
/// (<c>Mocha.Transport.RabbitMQ</c>, <c>Mocha.Transport.Postgres</c>, and any third-party transport
/// that exposes binding topology) can construct binding resources from their
/// <see cref="MessagingTransport.ContributeMochaResources"/> overrides across package boundaries.
/// Reusing the same typed resource keeps the <c>mocha.binding</c> attribute schema stable across
/// contributors.
/// </remarks>
public sealed class MochaBindingResource : MochaResource
{
    private readonly string _id;
    private readonly string _system;
    private readonly string _transportId;
    private readonly string _sourceId;
    private readonly string _destinationId;
    private readonly string? _routingKey;
    private readonly string? _address;
    private readonly bool? _autoProvision;

    /// <summary>
    /// Initializes a new <see cref="MochaBindingResource"/>.
    /// </summary>
    /// <param name="system">The transport system identifier (e.g. <c>rabbitmq</c>, <c>postgres</c>, <c>memory</c>).</param>
    /// <param name="transportId">The transport instance identifier (typically the topology address).</param>
    /// <param name="sourceName">The name of the source entity used to derive the binding's URN.</param>
    /// <param name="destinationName">The name of the destination entity used to derive the binding's URN.</param>
    /// <param name="sourceId">The cross-resource id of the source <see cref="MochaResource"/>.</param>
    /// <param name="destinationId">The cross-resource id of the destination <see cref="MochaResource"/>.</param>
    /// <param name="routingKey">The routing key pattern used by the binding, or <see langword="null"/> if not applicable.</param>
    /// <param name="address">An optional URI describing the binding's address within the topology.</param>
    /// <param name="autoProvision">Whether the binding is auto-provisioned by the transport, or <see langword="null"/> if not applicable.</param>
    public MochaBindingResource(
        string system,
        string transportId,
        string sourceName,
        string destinationName,
        string sourceId,
        string destinationId,
        string? routingKey = null,
        string? address = null,
        bool? autoProvision = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(system);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationId);

        _system = system;
        _transportId = transportId;
        _sourceId = sourceId;
        _destinationId = destinationId;
        _routingKey = routingKey;
        _address = address;
        _autoProvision = autoProvision;
        _id = MochaUrn.Create(system, "binding", transportId, sourceName, destinationName);
    }

    /// <inheritdoc />
    public override string Kind => "mocha.binding";

    /// <inheritdoc />
    public override string Id => _id;

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteString("system", _system);
        writer.WriteString("transport_id", _transportId);
        writer.WriteString("source_id", _sourceId);
        writer.WriteString("destination_id", _destinationId);

        if (_routingKey is not null)
        {
            writer.WriteString("routing_key", _routingKey);
        }

        if (_address is not null)
        {
            writer.WriteString("address", _address);
        }

        if (_autoProvision is { } autoProvision)
        {
            writer.WriteBoolean("auto_provision", autoProvision);
        }
    }
}
