using System.Collections.Generic;
using Transport.Sockets;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

public sealed class PingMessage : IMessage
{
    public PingMessage(IDictionary<string, object?>? payload)
    {
        Payload = payload;
    }

    public string Type => MessageTypes.Ping;

    public IDictionary<string, object?>? Payload { get; }

    public static PingMessage Default { get; } = new PingMessage(null);
}
