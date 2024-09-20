namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Represents a manager for socket operations. This manager can be used to start and stop
/// operations on a socket.
/// </summary>
public interface ISession : IAsyncDisposable
{
    /// <summary>
    /// The name of the underlying socket client
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Opens a session over the socket
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation</param>
    Task OpenSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a new operation over the socket
    /// </summary>
    /// <param name="request">The request that opens the operations</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation</param>
    Task<ISocketOperation> StartOperationAsync(
        OperationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a new operation over the socket
    /// </summary>
    /// <param name="operationId">The id of the operation to stop</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation</param>
    Task StopOperationAsync(
        string operationId,
        CancellationToken cancellationToken = default);
}
