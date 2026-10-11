#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client.Protocols;
#else
namespace HotChocolate.Transport.Sockets.Client.Protocols;
#endif

/// <summary>
/// Represents a message that is sent over a WebSocket connection for a data transfer operation
/// for GraphQL over WebSockets.
/// </summary>
internal interface IDataMessage : IOperationMessage, IDisposable
{
    /// <summary>
    /// Gets the identifier of the data message.
    /// </summary>
    string Id { get; }
}
