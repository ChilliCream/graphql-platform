namespace StrawberryShake.Transport.WebSockets;

internal class DefaultSocketConnectionInterceptor : ISocketConnectionInterceptor
{
    public ValueTask<object?> CreateConnectionInitPayload(
        ISocketProtocol protocol,
        CancellationToken cancellationToken)
    {
        return default;
    }

    public virtual void OnConnectionOpened(ISocketClient client)
    {
    }

    public virtual void OnConnectionClosed(ISocketClient client)
    {
    }

    public static readonly DefaultSocketConnectionInterceptor Instance = new();
}
