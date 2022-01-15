using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions.Messages;

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
            "Connection terminated by user.",
            SocketCloseStatus.NormalClosure,
            cancellationToken);

        await _socketSessionInterceptor.OnCloseAsync(connection, cancellationToken);
    }
}
