using System.Text.Json;

namespace StrawberryShake.Transport.WebSockets.Protocols;

/// <summary>
/// The <see cref="GraphQLWebSocketMessage"/> is used as a data transport structure to send messages
/// over a <see cref="GraphQLWebSocketProtocol"/>
/// </summary>
internal ref struct GraphQLWebSocketMessage
{
    /// <summary>
    /// The Id of the operation this message belongs to.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The identifier of the type of the message.
    /// </summary>
    public GraphQLWebSocketMessageType Type { get; set; }

    /// <summary>
    /// The payload of the message
    /// </summary>
    public JsonDocument Payload { get; set; }
}
