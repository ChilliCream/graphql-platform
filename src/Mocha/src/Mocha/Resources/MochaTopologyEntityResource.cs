using System.Text.Json;
using Mocha.Resources;

namespace Mocha;

/// <summary>
/// Default <see cref="MochaResource"/> for a <see cref="TopologyEntityDescription"/>.
/// </summary>
/// <remarks>
/// Produced by <see cref="MessagingTransport.ContributeMochaResources"/>'s default
/// implementation. Transports with richer topology semantics (RabbitMQ exchanges with
/// arguments, Postgres durability flags, …) typically replace this fallback with
/// transport-specific subclasses for each entity kind.
/// </remarks>
internal sealed class MochaTopologyEntityResource : MochaResource
{
    private readonly string _id;
    private readonly string _kind;
    private readonly string _transportId;
    private readonly TopologyEntityDescription _description;

    public MochaTopologyEntityResource(string system, string transportId, TopologyEntityDescription description)
    {
        _transportId = transportId;
        _description = description;
        _kind = "mocha." + description.Kind;
        var nameSegment = description.Name ?? description.Address ?? description.Kind;
        _id = MochaUrn.Create(system, description.Kind, transportId, nameSegment);
    }

    public override string Kind => _kind;

    public override string Id => _id;

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteString("transport_id", _transportId);
        if (_description.Name is not null)
        {
            writer.WriteString("name", _description.Name);
        }

        if (_description.Address is not null)
        {
            writer.WriteString("address", _description.Address);
        }

        if (_description.Flow is not null)
        {
            writer.WriteString("flow", _description.Flow);
        }
    }
}
