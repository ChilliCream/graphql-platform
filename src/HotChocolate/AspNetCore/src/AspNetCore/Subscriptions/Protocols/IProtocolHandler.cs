using System.Buffers;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

/// <summary>
/// Represents a GraphQL websocket protocol handler.
/// </summary>
public interface IProtocolHandler
{
    /// <summary>
    /// Gets the protocol name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes a received request message.
    /// </summary>
    /// <param name="connection">
    /// The socket client connection.
    /// </param>
    /// <param name="message">
    /// The message.
    /// </param>
    /// <param name="cancellationToken">
    /// The request cancellation token.
    /// </param>
    Task ExecuteAsync(
        ISocketConnection connection,
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken);
}
