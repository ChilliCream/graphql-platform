using System.Text.Json;

namespace Mocha.Resources;

/// <summary>
/// Default <see cref="MochaResource"/> for a <see cref="ReceiveEndpoint"/>.
/// </summary>
internal sealed class MochaReceiveEndpointResource : MochaResource
{
    private readonly string _id;
    private readonly string _transportId;
    private readonly string _name;
    private readonly ReceiveEndpointKind _kind;
    private readonly string? _address;
    private readonly string? _sourceAddress;

    public MochaReceiveEndpointResource(string system, string transportId, ReceiveEndpointDescription description)
    {
        _transportId = transportId;
        _name = description.Name;
        _kind = description.Kind;
        _address = description.Address;
        _sourceAddress = description.SourceAddress;
        _id = MochaUrn.Create(system, "receive_endpoint", transportId, description.Name);
    }

    public override string Kind => "mocha.receive_endpoint";

    public override string Id => _id;

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteString("transport_id", _transportId);
        writer.WriteString("name", _name);
        writer.WriteString("kind", _kind.ToString());
        if (_address is not null)
        {
            writer.WriteString("address", _address);
        }

        if (_sourceAddress is not null)
        {
            writer.WriteString("source_address", _sourceAddress);
        }
    }
}
