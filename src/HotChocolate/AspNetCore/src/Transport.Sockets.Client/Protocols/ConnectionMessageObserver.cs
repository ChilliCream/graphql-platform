using System.Net.WebSockets;

namespace HotChocolate.Transport.Sockets.Client.Protocols;

internal sealed class ConnectionMessageObserver<TConnectMessage> : IObserver<IOperationMessage>, IDisposable
{
    // The close code the client reports when the connection is lost during initialization
    // without a close frame. It mirrors the 1006 "abnormal closure" code that the WebSocket
    // protocol reserves for this condition.
    private const WebSocketCloseStatus AbnormalClosure = (WebSocketCloseStatus)1006;

    private readonly TaskCompletionSource<bool> _promise = new();
    private readonly WebSocket _socket;
    private readonly CancellationToken _cancellationToken;
    private readonly CancellationTokenRegistration _cancellationRegistration;

    public ConnectionMessageObserver(WebSocket socket, CancellationToken cancellationToken)
    {
        _socket = socket;
        _cancellationToken = cancellationToken;
        _cancellationRegistration =
            cancellationToken.Register(() => _promise.TrySetCanceled(cancellationToken));
    }

    public Task<bool> Accepted => _promise.Task;

    public void OnNext(IOperationMessage value)
    {
        if (value is TConnectMessage)
        {
            _promise.TrySetResult(true);
        }
    }

    public void OnError(Exception error)
        => _promise.TrySetException(error);

    public void OnCompleted()
    {
        // the pipeline completed before the expected connection message arrived.
        if (_cancellationToken.IsCancellationRequested)
        {
            // the caller cancelled, so we keep cancellation semantics.
            _promise.TrySetCanceled(_cancellationToken);
        }
        else if (_socket.CloseStatus is not null)
        {
            // the server ended the connection with a close frame.
            _promise.TrySetException(
                new SocketClosedException(
                    _socket.CloseStatusDescription ?? "Socket was closed.",
                    _socket.CloseStatus.Value));
        }
        else
        {
            // the connection was lost without a close frame and the caller did not cancel.
            _promise.TrySetException(
                new SocketClosedException(
                    "Connection closed abnormally (no close frame received).",
                    AbnormalClosure));
        }
    }

    public void Dispose()
        => _cancellationRegistration.Dispose();
}
