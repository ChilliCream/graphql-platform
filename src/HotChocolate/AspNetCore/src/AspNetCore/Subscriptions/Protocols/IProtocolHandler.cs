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
    Task OnReceiveAsync(
        ISocketSession session,
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken);

    Task SendKeepAliveMessageAsync(
        ISocketSession session,
        CancellationToken cancellationToken);

    Task SendResultMessageAsync(
        ISocketSession session,
        string operationSessionId,
        IQueryResult result,
        CancellationToken cancellationToken);

    Task SendErrorMessageAsync(
        ISocketSession session,
        string operationSessionId,
        IReadOnlyList<IError> errors,
        CancellationToken cancellationToken);

    Task SendCompleteMessageAsync(
        ISocketSession session,
        string operationSessionId,
        CancellationToken cancellationToken);
}
