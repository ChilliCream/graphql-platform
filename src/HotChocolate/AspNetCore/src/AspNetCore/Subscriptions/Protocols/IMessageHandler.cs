using HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

internal interface IMessageHandler
{
    Task HandleAsync(
        ISocketConnection connection,
        OperationMessage message,
        CancellationToken cancellationToken);

    bool CanHandle(OperationMessage message);
}
