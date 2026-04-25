using System.Text.Json;

namespace Mocha.Resources;

/// <summary>
/// Generic <see cref="MochaResource"/> for a <see cref="TopologyEntityDescription"/> whose
/// kind does not map to a typed transport resource (queue/exchange/topic/binding).
/// </summary>
internal sealed class MochaTopologyEntityResource : MochaResource
{
    private readonly string _id;
    private readonly string _transportId;
    private readonly TopologyEntityDescription _description;

    public MochaTopologyEntityResource(string system, string transportId, TopologyEntityDescription description)
    {
        _transportId = transportId;
        _description = description;
        var nameSegment = description.Name ?? description.Address ?? description.Kind;
        _id = MochaUrn.Create(system, "topology_entity", transportId, nameSegment);
    }

    public override string Kind => "mocha.topology_entity";

    public override string Id => _id;

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteString("transport_id", _transportId);
        writer.WriteString("entity_kind", _description.Kind);

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
