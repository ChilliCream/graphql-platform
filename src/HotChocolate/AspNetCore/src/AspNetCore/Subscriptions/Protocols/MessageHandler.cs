using HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

public abstract class MessageHandler<T> : IMessageHandler
    where T : OperationMessage
{
    public bool CanHandle(OperationMessage message)
    {
        return message is T m && CanHandle(m);
    }

    protected virtual bool CanHandle(T message) => true;

    public Task HandleAsync(
        ISocketConnection connection,
        OperationMessage message,
        CancellationToken cancellationToken)
    {
        if (message is T m)
        {
            return HandleAsync(connection, m, cancellationToken);
        }

        throw new NotSupportedException("The specified message type is not supported.");
    }

    protected abstract Task HandleAsync(
        ISocketConnection connection,
        T message,
        CancellationToken cancellationToken);
}
