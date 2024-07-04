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


    ValueTask OnReceiveAsync(
        ISocketSession session,
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Is invoked by the socket session when the connection initialization timeout is reached
    /// and must close the socket connection.
    /// </summary>
    /// <param name="session">
    /// The socket session.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    ValueTask OnConnectionInitTimeoutAsync(
        ISocketSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a keep alive message to the client.
    /// </summary>
    /// <param name="session">
    /// The socket session.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    ValueTask SendKeepAliveMessageAsync(
        ISocketSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a query result message to the client.
    /// </summary>
    /// <param name="session">
    /// The socket session.
    /// </param>
    /// <param name="operationSessionId">
    /// The operation session id.
    /// </param>
    /// <param name="result">
    /// The query result object.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    ValueTask SendResultMessageAsync(
        ISocketSession session,
        string operationSessionId,
        IOperationResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a validation error message to the client.
    /// </summary>
    /// <param name="session">
    /// The socket session.
    /// </param>
    /// <param name="operationSessionId">
    /// The operation session id.
    /// </param>
    /// <param name="errors">
    /// The validation or syntax errors.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    ValueTask SendErrorMessageAsync(
        ISocketSession session,
        string operationSessionId,
        IReadOnlyList<IError> errors,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an operation complete message to the client which signals to the client that
    /// no more results will be send for the server for the specified operation session.
    /// </summary>
    /// <param name="session">
    /// The socket session.
    /// </param>
    /// <param name="operationSessionId">
    /// The operation session id.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    ValueTask SendCompleteMessageAsync(
        ISocketSession session,
        string operationSessionId,
        CancellationToken cancellationToken = default);
}
