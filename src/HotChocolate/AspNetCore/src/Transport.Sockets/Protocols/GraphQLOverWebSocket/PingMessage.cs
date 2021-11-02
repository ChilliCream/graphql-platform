using System.Collections.Generic;
using Transport.Sockets;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

/// <summary>
/// A message for detecting failed connections, 
/// displaying latency metrics or other types 
/// of network probing.
/// </summary>
public sealed class PingMessage : IMessage
{
    /// <summary>
    /// Creates a new instance of <see cref="PingMessage" />.
    /// </summary>
    /// <param name="payload">
    /// The optional payload can be used to transfer additional 
    /// details about the ping.
    /// </param>
    public PingMessage(IDictionary<string, object?>? payload)
    {
        Payload = payload;
    }

    /// <inheritdoc />
    public string Type => MessageTypes.Ping;

    /// <summary>
    /// Gets the optional payload field that can be used to 
    /// transfer additional details about the ping.
    /// </summary>
    public IDictionary<string, object?>? Payload { get; }

    /// <summary>
    /// Gets the default ping message that is used whenever there is no payload.
    /// </summary>
    public static PingMessage Default { get; } = new PingMessage(null);
}
