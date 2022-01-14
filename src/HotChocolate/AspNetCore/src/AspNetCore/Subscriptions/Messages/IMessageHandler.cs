namespace HotChocolate.AspNetCore.Subscriptions.Messages;

public interface IMessageHandler
{
    Task HandleAsync(
        ISocketConnection connection,
        OperationMessage message,
        CancellationToken cancellationToken);

    bool CanHandle(OperationMessage message);
}
