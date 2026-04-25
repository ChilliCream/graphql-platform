using System.Text.Json;

namespace Mocha.Resources;

/// <summary>
/// <see cref="MochaResource"/> describing an inbound route binding a message type to a consumer
/// and a receive endpoint.
/// </summary>
internal sealed class MochaInboundRouteResource : MochaResource
{
    private readonly string _id;
    private readonly InboundRouteKind _kind;
    private readonly string? _messageTypeIdentity;
    private readonly string? _consumerName;
    private readonly EndpointReferenceDescription? _endpoint;

    public MochaInboundRouteResource(string instanceId, int index, InboundRouteDescription description)
    {
        _kind = description.Kind;
        _messageTypeIdentity = description.MessageTypeIdentity;
        _consumerName = description.ConsumerName;
        _endpoint = description.Endpoint;
        _id = MochaUrn.Create(
            "core",
            "inbound_route",
            instanceId,
            description.ConsumerName ?? description.MessageTypeIdentity ?? index.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public override string Kind => "mocha.inbound_route";

    public override string Id => _id;

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteString("kind", _kind.ToString());

        if (_messageTypeIdentity is not null)
        {
            writer.WriteString("message_type_identity", _messageTypeIdentity);
        }

        if (_consumerName is not null)
        {
            writer.WriteString("consumer_name", _consumerName);
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
