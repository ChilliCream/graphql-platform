using System.Buffers;

namespace HotChocolate.Transport.Sockets.Client.Protocols;

/// <summary>
/// Represents an abstraction for GraphQL over WebSocket protocols.
/// </summary>
internal interface IProtocolHandler
{
    /// <summary>
    /// Gets the name of the protocol handled by this protocol handler.
    /// </summary>
    /// <remarks>
    /// This property should return a unique name that identifies the protocol.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Initializes the protocol handler with the specified payload.
    /// </summary>
    /// <typeparam name="T">The type of the payload.</typeparam>
    /// <param name="context">
    /// The <see cref="SocketClientContext"/> object representing the WebSocket
    /// client context.
    /// </param>
    /// <param name="payload">
    /// The payload to use when initializing the protocol handler.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> that represents the asynchronous operation.
    /// </returns>
    ValueTask InitializeAsync<T>(
        SocketClientContext context,
        T payload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the specified operation request using the protocol handler.
    /// </summary>
    /// <param name="context">
    /// The <see cref="SocketClientContext"/> object representing the WebSocket
    /// client context.
    /// </param>
    /// <param name="request">
    /// The <see cref="OperationRequest"/> object representing the operation request
    /// to execute.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="SocketResult"/> that represents the asynchronous operation.
    /// The <see cref="OperationRequest"/> object that represents the result of the
    /// operation is the result of the task.
    /// </returns>
    ValueTask<SocketResult> ExecuteAsync(
        SocketClientContext context,
        OperationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a message is received from the server.
    /// </summary>
    /// <param name="context">
    /// The <see cref="SocketClientContext"/> object representing the WebSocket
    /// client context.
    /// </param>
    /// <param name="message">
    /// A <see cref="ReadOnlySequence{T}"/> object representing the message received
    /// from the server.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ReadOnlySequence{T}"/> that represents the asynchronous operation.
    /// </returns>
    ValueTask OnReceiveAsync(
        SocketClientContext context,
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken = default);
}
