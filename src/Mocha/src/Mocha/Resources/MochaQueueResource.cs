using System.Text.Json;

namespace Mocha.Resources;

/// <summary>
/// <see cref="MochaResource"/> describing a transport queue (durable, exclusive, temporary, etc.).
/// </summary>
public sealed class MochaQueueResource : MochaResource
{
    private readonly string _id;
    private readonly string _system;
    private readonly string _transportId;
    private readonly string _name;
    private readonly string? _address;
    private readonly bool? _durable;
    private readonly bool? _exclusive;
    private readonly bool? _autoDelete;
    private readonly bool? _autoProvision;
    private readonly bool _temporary;

    /// <summary>
    /// Initializes a new <see cref="MochaQueueResource"/>.
    /// </summary>
    /// <param name="system">The transport system identifier (e.g. <c>rabbitmq</c>, <c>postgres</c>, <c>memory</c>).</param>
    /// <param name="transportId">The transport instance identifier (typically the topology address).</param>
    /// <param name="name">The queue name as known to the broker.</param>
    /// <param name="address">An optional URI describing the queue's address within the topology.</param>
    /// <param name="durable">Whether the queue survives broker restarts, or <see langword="null"/> if not applicable.</param>
    /// <param name="exclusive">Whether the queue is exclusive to a single connection, or <see langword="null"/> if not applicable.</param>
    /// <param name="autoDelete">Whether the queue is automatically deleted when no longer in use, or <see langword="null"/> if not applicable.</param>
    /// <param name="autoProvision">Whether the queue is auto-provisioned by the transport, or <see langword="null"/> if not applicable.</param>
    /// <param name="temporary">Whether the queue is a temporary/reply queue (defaults to <see langword="false"/>).</param>
    public MochaQueueResource(
        string system,
        string transportId,
        string name,
        string? address = null,
        bool? durable = null,
        bool? exclusive = null,
        bool? autoDelete = null,
        bool? autoProvision = null,
        bool temporary = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(system);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _system = system;
        _transportId = transportId;
        _name = name;
        _address = address;
        _durable = durable;
        _exclusive = exclusive;
        _autoDelete = autoDelete;
        _autoProvision = autoProvision;
        _temporary = temporary;
        _id = MochaUrn.Create(system, "queue", transportId, name);
    }

    /// <inheritdoc />
    public override string Kind => "mocha.queue";

    /// <inheritdoc />
    public override string Id => _id;

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteString("system", _system);
        writer.WriteString("transport_id", _transportId);
        writer.WriteString("name", _name);

        if (_address is not null)
        {
            writer.WriteString("address", _address);
        }

        if (_durable is { } durable)
        {
            writer.WriteBoolean("durable", durable);
        }

        if (_exclusive is { } exclusive)
        {
            writer.WriteBoolean("exclusive", exclusive);
        }

        if (_autoDelete is { } autoDelete)
        {
            writer.WriteBoolean("auto_delete", autoDelete);
        }

        if (_autoProvision is { } autoProvision)
        {
            writer.WriteBoolean("auto_provision", autoProvision);
        }

        if (_temporary)
        {
            writer.WriteBoolean("temporary", true);
        }
    }
}
