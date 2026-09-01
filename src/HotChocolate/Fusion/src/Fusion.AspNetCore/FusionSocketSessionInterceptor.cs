using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;

namespace HotChocolate.Fusion.AspNetCore;

internal sealed class FusionSocketSessionInterceptor : DefaultSocketSessionInterceptor
{
    public override ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketSession session,
        IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken = default)
    {
        if (connectionInitMessage.Payload is { } payload)
        {
            session.Connection.Features.Set(payload);
        }

        return base.OnConnectAsync(session, connectionInitMessage, cancellationToken);
    }
}
