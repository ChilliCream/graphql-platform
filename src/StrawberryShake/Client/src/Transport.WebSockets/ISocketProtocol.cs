using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Delegate that is invoked whenever a message is received by the protocol
/// </summary>
/// <param name="operationId">The ide of the operation that the message belongs to</param>
/// <param name="message">The message</param>
/// <param name="cancellationToken">A token to cancel processing the message</param>
public delegate ValueTask OnReceiveAsync(
    string operationId,
    OperationMessage message,
    CancellationToken cancellationToken);

/// <summary>
/// A protocol that can be used to communicate to a GraphQL server over
/// <see cref="ISocketClient"/>
/// </summary>
public interface ISocketProtocol : IAsyncDisposable
{
    /// <summary>
    ///  A even that is called when the <see cref="ISocketProtocol"/> is disposed
    /// </summary>
    event EventHandler Disposed;

    /// <summary>
    /// Starts a new operation on the server
    /// </summary>
    /// <param name="operationId">
    /// The id of the operation. This id must be unique!
    /// </param>
    /// <param name="request">
    /// The <see cref="OperationRequest"/> that contains the definition of the operation
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to cancel the start of the operation
    /// </param>
    /// <returns>A task that is completed once the operation is started</returns>
    Task StartOperationAsync(
        string operationId,
        OperationRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stops a operation on the server
    /// </summary>
    /// <param name="operationId">The id of the operation to stop</param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to cancel the stop of the operation
    /// </param>
    /// <returns>A task that is completed once the operation is stopped</returns>
    Task StopOperationAsync(
        string operationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Initializes the protocol over a <see cref="ISocketClient"/> on the server.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to cancel the initialization
    /// </param>
    /// <returns>A task that is completed once the protocol is established</returns>
    Task InitializeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Terminates the protocol and the communication with the server
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to cancel the termination
    /// </param>
    /// <returns>A task that is completed once the protocol is terminated</returns>
    Task TerminateAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes a listener to receive all messages that the server sends over this protocol
    /// </summary>
    /// <param name="listener">
    /// The listener that is invoked on every message
    /// </param>
    void Subscribe(OnReceiveAsync listener);

    /// <summary>
    /// Unsubscribes a listener from the protocol..
    /// </summary>
    /// <param name="listener"></param>
    void Unsubscribe(OnReceiveAsync listener);

    /// <summary>
    /// Notify the protocol to complete
    /// </summary>
    /// <param name="operationId">The id of the operation to stop</param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to cancel the notification
    /// </param>
    /// <returns>A task that is completed once the notification is completed</returns>
    ValueTask NotifyCompletion(
        string operationId,
        CancellationToken cancellationToken);
}
