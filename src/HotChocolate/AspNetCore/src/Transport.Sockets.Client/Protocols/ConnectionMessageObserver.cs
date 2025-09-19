namespace HotChocolate.Transport.Sockets.Client.Protocols;

internal sealed class ConnectionMessageObserver<TConnectMessage> : IObserver<IOperationMessage>
{
    private readonly TaskCompletionSource<bool> _promise = new();

    public ConnectionMessageObserver(CancellationToken cancellationToken)
    {
        cancellationToken.Register(() => _promise.TrySetCanceled());
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
        => _promise.TrySetCanceled();
}
