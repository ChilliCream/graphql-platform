using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;

namespace HotChocolate.AspNetCore;

/// <summary>
/// The socket interceptor allows to customize the GraphQL over Websocket behavior.
/// </summary>
public interface ISocketSessionInterceptor
{
    /// <summary>
    /// Invoked when the socket connection initialization message is received.
    /// </summary>
    /// <param name="session">
    /// The socket session.
    /// </param>
    /// <param name="connectionInitMessage">
    /// The connection init message that was received.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the connection status that specifies if the socket connection shall be accepted
    /// or if it shall be refused.
    /// </returns>
    ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketSession session,
        IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoked when a GraphQL request is registered with the socket session.
    /// </summary>
    /// <param name="session">
    /// The socket session.
    /// </param>
    /// <param name="operationSessionId">
    /// The user-provided unique operation session id.
    /// </param>
    /// <param name="requestBuilder">
    /// The GraphQL request builder.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    ValueTask OnRequestAsync(
        ISocketSession session,
        string operationSessionId,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoked before the result is serialized and send to the client.
    /// This interception method allows to amend the result object.
    /// </summary>
    /// <param name="session">
    /// The socket session.
    /// </param>
    /// <param name="operationSessionId">
    /// The user-provided unique operation session id.
    /// </param>
    /// <param name="result">
    /// The result produced by executing the GraphQL request.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the result that shall be send to the client.
    /// </returns>
    ValueTask<IOperationResult> OnResultAsync(
        ISocketSession session,
        string operationSessionId,
        IOperationResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoked when the execution of a GraphQL request is completed.
    /// This interception method is guaranteed to be invoked even if the operation
    /// fails or the connection is closed.
    ///
    /// The cancellation token might be cancelled if the connection is closed.
    /// </summary>
    /// <param name="session">
    /// The socket session.
    /// </param>
    /// <param name="operationSessionId">
    /// The user-provided unique operation session id.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    ValueTask OnCompleteAsync(
        ISocketSession session,
        string operationSessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoked when a ping message is received and allows to produce the payload
    /// for the pong message.
    /// </summary>
    /// <param name="session">
    /// The socket session.
    /// </param>
    /// <param name="pingMessage">
    /// The ping message.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the Pong-message payload.
    /// </returns>
    ValueTask<IReadOnlyDictionary<string, object?>?> OnPingAsync(
        ISocketSession session,
        IOperationMessagePayload pingMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoked when a pong message is received.
    /// </summary>
    /// <param name="session">
    /// The socket session.
    /// </param>
    /// <param name="pongMessage">
    /// The pong message.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    ValueTask OnPongAsync(
        ISocketSession session,
        IOperationMessagePayload pongMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoked when the connection is closed.
    /// </summary>
    /// <param name="session">
    /// The socket session.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    ValueTask OnCloseAsync(
        ISocketSession session,
        CancellationToken cancellationToken = default);
}
