using System.Collections.Generic;
using Transport.Sockets;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

/// <summary>
/// The response message to the Ping message. 
/// Must be sent as soon as the Ping message is received.
/// 
/// The Pong message can be sent at any time within the established socket. 
/// Furthermore, the Pong message may even be sent unsolicited as an 
/// unidirectional heartbeat.
/// </summary>
public sealed class PongMessage : IMessage
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="payload">
    /// The optional payload can be used to transfer additional 
    /// details about the pong.
    /// </param>
    public PongMessage(IDictionary<string, object?>? payload)
    {
        Payload = payload;
    }

    /// <inheritdoc />
    public string Type => MessageTypes.Pong;

    /// <summary>
    /// Gets the optional payload field that can be used to 
    /// transfer additional details about the pong.
    /// </summary>
    public IDictionary<string, object?>? Payload { get; }

    /// <summary>
    /// Gets the default pong message that is used whenever there is no payload.
    /// </summary>
    public static PongMessage Default { get; } = new PongMessage(null);
}
