namespace HotChocolate.Transport.Sockets.Client.Protocols;

/// <summary>
/// Represents a message that is sent over a WebSocket connection for a data transfer operation
/// for GraphQL over WebSockets.
/// </summary>
internal interface IDataMessage : IOperationMessage
{
    /// <summary>
    /// Gets the identifier of the data message.
    /// </summary>
    string Id { get; }
}
