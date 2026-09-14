#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client.Protocols;
#else
namespace HotChocolate.Transport.Sockets.Client.Protocols;
#endif

/// <summary>
/// Represents a message that is sent over a WebSocket connection for GraphQL over WebSockets.
/// </summary>
internal interface IOperationMessage
{
    /// <summary>
    /// Gets the type of the operation message.
    /// </summary>
    /// <remarks>
    /// This property should return a string that identifies the type of the operation message.
    /// </remarks>
    string Type { get; }
}
