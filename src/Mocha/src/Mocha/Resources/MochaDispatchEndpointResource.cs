using System.Text.Json;
using Mocha.Resources;

namespace Mocha;

/// <summary>
/// Default <see cref="MochaResource"/> for a <see cref="DispatchEndpoint"/>.
/// </summary>
internal sealed class MochaDispatchEndpointResource : MochaResource
{
    private readonly string _id;
    private readonly string _transportId;
    private readonly string _name;
    private readonly DispatchEndpointKind _kind;
    private readonly string? _address;
    private readonly string? _destinationAddress;

    public MochaDispatchEndpointResource(string system, string transportId, DispatchEndpointDescription description)
    {
        _transportId = transportId;
        _name = description.Name;
        _kind = description.Kind;
        _address = description.Address;
        _destinationAddress = description.DestinationAddress;
        _id = MochaUrn.Create(system, "dispatch_endpoint", transportId, description.Name);
    }

    public override string Kind => "mocha.dispatch_endpoint";

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

        if (_destinationAddress is not null)
        {
            writer.WriteString("destination_address", _destinationAddress);
        }
    }
}
