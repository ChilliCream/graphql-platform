using Transport.Sockets;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

public sealed class SubscribeMessage : IMessage
{
    public SubscribeMessage(string id, SubscribePayload payload)
    {
        Id = id;
        Payload = payload;
    }

    public string Type => MessageTypes.Subscribe;

    public string Id { get; }

    public SubscribePayload Payload { get; }
}
