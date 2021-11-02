using System.Collections.Generic;
using Transport.Sockets;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

public sealed class ConnectionAckMessage : IMessage
{
    public ConnectionAckMessage(IDictionary<string, object?>? payload)
    {
        Payload = payload;
    }

    /// <inheritdoc />
    public string Type => MessageTypes.Accept;

    public IDictionary<string, object?>? Payload { get; }

    public static ConnectionAckMessage Default { get; } = new ConnectionAckMessage(null);
}
