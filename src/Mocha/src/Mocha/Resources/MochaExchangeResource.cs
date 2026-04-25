using System.Text.Json;

namespace Mocha.Resources;

/// <summary>
/// <see cref="MochaResource"/> describing a transport exchange (e.g. RabbitMQ direct/fanout/topic
/// exchange).
/// </summary>
public sealed class MochaExchangeResource : MochaResource
{
    private readonly string _id;
    private readonly string _system;
    private readonly string _transportId;
    private readonly string _name;
    private readonly string? _address;
    private readonly string? _exchangeType;
    private readonly bool? _durable;
    private readonly bool? _autoDelete;
    private readonly bool? _autoProvision;

    /// <summary>
    /// Initializes a new <see cref="MochaExchangeResource"/>.
    /// </summary>
    /// <param name="system">The transport system identifier (e.g. <c>rabbitmq</c>).</param>
    /// <param name="transportId">The transport instance identifier (typically the topology address).</param>
    /// <param name="name">The exchange name as known to the broker.</param>
    /// <param name="address">An optional URI describing the exchange's address within the topology.</param>
    /// <param name="exchangeType">The exchange type (e.g. <c>direct</c>, <c>fanout</c>, <c>topic</c>, <c>headers</c>).</param>
    /// <param name="durable">Whether the exchange survives broker restarts, or <see langword="null"/> if not applicable.</param>
    /// <param name="autoDelete">Whether the exchange is automatically deleted when no longer in use, or <see langword="null"/> if not applicable.</param>
    /// <param name="autoProvision">Whether the exchange is auto-provisioned by the transport, or <see langword="null"/> if not applicable.</param>
    public MochaExchangeResource(
        string system,
        string transportId,
        string name,
        string? address = null,
        string? exchangeType = null,
        bool? durable = null,
        bool? autoDelete = null,
        bool? autoProvision = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(system);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _system = system;
        _transportId = transportId;
        _name = name;
        _address = address;
        _exchangeType = exchangeType;
        _durable = durable;
        _autoDelete = autoDelete;
        _autoProvision = autoProvision;
        _id = MochaUrn.Create(system, "exchange", transportId, name);
    }

    /// <inheritdoc />
    public override string Kind => "mocha.exchange";

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

        if (_exchangeType is not null)
        {
            writer.WriteString("exchange_type", _exchangeType);
        }

        if (_durable is { } durable)
        {
            writer.WriteBoolean("durable", durable);
        }

        if (_autoDelete is { } autoDelete)
        {
            writer.WriteBoolean("auto_delete", autoDelete);
        }

        if (_autoProvision is { } autoProvision)
        {
            writer.WriteBoolean("auto_provision", autoProvision);
        }
    }
}
