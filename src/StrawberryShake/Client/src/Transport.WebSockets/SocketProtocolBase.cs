using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// A protocol that can be used to communicate to a GraphQL server over
/// <see cref="ISocketClient"/>
/// </summary>
public abstract class SocketProtocolBase : ISocketProtocol
{
    private bool _disposed;
    private readonly HashSet<OnReceiveAsync> _listeners = [];

    /// <inheritdoc />
    public event EventHandler Disposed = default!;

    /// <inheritdoc />
    public abstract Task StartOperationAsync(
        string operationId,
        OperationRequest request,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract Task StopOperationAsync(
        string operationId,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract Task InitializeAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract Task TerminateAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public void Subscribe(OnReceiveAsync listener)
    {
        _listeners.Add(listener);
    }

    /// <inheritdoc />
    public void Unsubscribe(OnReceiveAsync listener)
    {
        _listeners.Remove(listener);
    }

    /// <summary>
    /// Notify all listeners that a message is received
    /// </summary>
    /// <param name="operationId">The ide of the operation that the message belongs to</param>
    /// <param name="message">The operation message</param>
    /// <param name="cancellationToken">A token to cancel processing the message</param>
    /// <returns>A value task that is completed once all subscribers are notified</returns>
    protected async ValueTask Notify(
        string operationId,
        OperationMessage message,
        CancellationToken cancellationToken)
    {
        foreach (var listener in _listeners)
        {
            await listener(operationId, message, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask NotifyCompletion(
        string operationId,
        CancellationToken cancellationToken)
    {
        await Notify(operationId, CompleteOperationMessage.Default, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            return default;
        }

        Disposed.Invoke(this, EventArgs.Empty);

        _disposed = true;
        return default;
    }
}
