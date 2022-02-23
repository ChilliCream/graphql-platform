using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;
using static HotChocolate.AspNetCore.Subscriptions.ConnectionCloseReason;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal sealed class TerminateConnectionMessageHandler
    : MessageHandler<TerminateConnectionMessage>
{
    private readonly ISocketSessionInterceptor _socketSessionInterceptor;

    public TerminateConnectionMessageHandler(ISocketSessionInterceptor socketSessionInterceptor)
    {
        _socketSessionInterceptor = socketSessionInterceptor ??
            throw new ArgumentNullException(nameof(socketSessionInterceptor));
    }

    protected override async Task HandleAsync(
        ISocketConnection connection,
        TerminateConnectionMessage message,
        CancellationToken cancellationToken)
    {
        await connection.CloseAsync(
            TerminateConnectionMessageHandler_Message,
            NormalClosure,
            cancellationToken);

        await _socketSessionInterceptor.OnCloseAsync(connection, cancellationToken);
    }
}
