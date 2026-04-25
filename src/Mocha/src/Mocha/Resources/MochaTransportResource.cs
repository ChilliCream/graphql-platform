using System.Text.Json;

namespace Mocha.Resources;

/// <summary>
/// Default <see cref="MochaResource"/> for a <see cref="MessagingTransport"/>.
/// </summary>
/// <remarks>
/// Produced by <see cref="MessagingTransport.ContributeMochaResources"/>'s default
/// implementation when a transport has not overridden it. Transports that contribute
/// richer kinds (queue/exchange/topic/binding) typically replace this fallback.
/// </remarks>
internal sealed class MochaTransportResource : MochaResource
{
    private readonly string _id;
    private readonly string _name;
    private readonly string _schema;
    private readonly string _transportType;
    private readonly string _topologyAddress;

    public MochaTransportResource(TransportDescription description)
    {
        _id = MochaUrn.Create(description.Schema, "transport", description.Identifier);
        _name = description.Name;
        _schema = description.Schema;
        _transportType = description.TransportType;
        _topologyAddress = description.Identifier;
    }

    public override string Kind => "mocha.transport";

    public override string Id => _id;

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteString("name", _name);
        writer.WriteString("schema", _schema);
        writer.WriteString("transport_type", _transportType);
        writer.WriteString("topology_address", _topologyAddress);
    }
}
