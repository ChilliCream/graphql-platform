using System.Collections.Generic;
using Transport.Sockets;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

/// <summary>
/// Indicates that the client wants to establish a connection within the existing socket. 
/// </summary>
public sealed class ConnectionInitMessage : IMessage
{
    /// <summary>
    /// Creates a new instance of <see cref="ConnectionInitMessage" />.
    /// </summary>
    /// <param name="payload">
    /// Optional payload that can be be passed in with the initialization message.
    /// </param>
    public ConnectionInitMessage(IDictionary<string, object?>? payload)
    {
        Payload = payload;
    }

    /// <inheritdoc />
    public string Type => MessageTypes.Initialize;

    /// <summary>
    /// Gets the optional payload that was passed in with the initialization message.
    /// </summary>
    public IDictionary<string, object?>? Payload { get; }

    /// <summary>
    /// Gets the default initialization message that is used whenever there is no payload.
    /// </summary>
    public static ConnectionInitMessage Default { get; } = new ConnectionInitMessage(null);
}
