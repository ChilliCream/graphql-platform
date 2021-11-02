using System.Collections.Generic;
using Transport.Sockets;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

/// <summary>
/// Expected response to the ConnectionInit message from the client acknowledging a successful connection with the server.
/// </summary>
public sealed class ConnectionAckMessage : IMessage
{
    /// <summary>
    /// Creates a new instance of <see cref="ConnectionInitMessage" />.
    /// </summary>
    /// <param name="payload">
    /// Optional payload that can be be passed in with the accept message.
    /// </param>
    public ConnectionAckMessage(IDictionary<string, object?>? payload)
    {
        Payload = payload;
    }

    /// <inheritdoc />
    public string Type => MessageTypes.Accept;

    /// <summary>
    /// Gets the optional payload that was passed in with the accept message.
    /// </summary>
    public IDictionary<string, object?>? Payload { get; }

    /// <summary>
    /// Gets the default accept message that is used whenever there is no payload.
    /// </summary>
    public static ConnectionAckMessage Default { get; } = new ConnectionAckMessage(null);
}
