using System.Text.Json;
using Mocha.Resources;

namespace Mocha;

/// <summary>
/// <see cref="MochaResource"/> describing an outbound route from a message type to a destination
/// or dispatch endpoint.
/// </summary>
internal sealed class MochaOutboundRouteResource : MochaResource
{
    private readonly string _id;
    private readonly OutboundRouteKind _kind;
    private readonly string _messageTypeIdentity;
    private readonly string? _destination;
    private readonly EndpointReferenceDescription? _endpoint;

    public MochaOutboundRouteResource(string instanceId, int index, OutboundRouteDescription description)
    {
        _kind = description.Kind;
        _messageTypeIdentity = description.MessageTypeIdentity;
        _destination = description.Destination;
        _endpoint = description.Endpoint;
        _id = MochaUrn.Create(
            "core",
            "outbound_route",
            instanceId,
            description.MessageTypeIdentity,
            index.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public override string Kind => "mocha.outbound_route";

    public override string Id => _id;

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteString("kind", _kind.ToString());
        writer.WriteString("message_type_identity", _messageTypeIdentity);

        if (_destination is not null)
        {
            writer.WriteString("destination", _destination);
        }

        if (_endpoint is not null)
        {
            writer.WriteString("endpoint_name", _endpoint.Name);
            writer.WriteString("endpoint_transport_name", _endpoint.TransportName);
            if (_endpoint.Address is not null)
            {
                writer.WriteString("endpoint_address", _endpoint.Address);
            }
        }
    }
}
