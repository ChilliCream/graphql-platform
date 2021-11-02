using System.Collections.Generic;
using Transport.Sockets;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

public sealed class PongMessage : IMessage
{
    public PongMessage(IDictionary<string, object?>? payload)
    {
        Payload = payload;
    }

    public string Type => MessageTypes.Pong;

    public IDictionary<string, object?>? Payload { get; }

    public static PongMessage Default { get; } = new PongMessage(null);
}
