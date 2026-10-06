using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Clients;

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
            session.Connection.Features.Set(new ClientConnectionInitPayload(payload.Clone()));
        }

        return base.OnConnectAsync(session, connectionInitMessage, cancellationToken);
    }

    public override async ValueTask OnRequestAsync(
        ISocketSession session,
        string operationSessionId,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken = default)
    {
        await base.OnRequestAsync(session, operationSessionId, requestBuilder, cancellationToken);

        if (session.Connection.Features.Get<ClientConnectionInitPayload>() is { } payload)
        {
            requestBuilder.Features.Set(payload);
        }
    }
}
