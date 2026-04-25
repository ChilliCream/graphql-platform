using System.Text.Json;

namespace Mocha.Resources;

/// <summary>
/// <see cref="MochaResource"/> describing a transport topic (e.g. an in-memory topic, a Postgres
/// topic table).
/// </summary>
public sealed class MochaTopicResource : MochaResource
{
    private readonly string _id;
    private readonly string _system;
    private readonly string _transportId;
    private readonly string _name;
    private readonly string? _address;
    private readonly bool? _autoProvision;

    /// <summary>
    /// Initializes a new <see cref="MochaTopicResource"/>.
    /// </summary>
    /// <param name="system">The transport system identifier (e.g. <c>memory</c>, <c>postgres</c>).</param>
    /// <param name="transportId">The transport instance identifier (typically the topology address).</param>
    /// <param name="name">The topic name as known to the broker.</param>
    /// <param name="address">An optional URI describing the topic's address within the topology.</param>
    /// <param name="autoProvision">Whether the topic is auto-provisioned by the transport, or <see langword="null"/> if not applicable.</param>
    public MochaTopicResource(
        string system,
        string transportId,
        string name,
        string? address = null,
        bool? autoProvision = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(system);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _system = system;
        _transportId = transportId;
        _name = name;
        _address = address;
        _autoProvision = autoProvision;
        _id = MochaUrn.Create(system, "topic", transportId, name);
    }

    /// <inheritdoc />
    public override string Kind => "mocha.topic";

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

        if (_autoProvision is { } autoProvision)
        {
            writer.WriteBoolean("auto_provision", autoProvision);
        }
    }
}
