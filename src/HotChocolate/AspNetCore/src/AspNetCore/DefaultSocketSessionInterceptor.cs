using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.AspNetCore;

public class DefaultSocketSessionInterceptor : ISocketSessionInterceptor
{
    public virtual ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketSession session,
        IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken = default)
        => new(ConnectionStatus.Accept());

    public virtual ValueTask OnRequestAsync(
        ISocketSession session,
        string operationSessionId,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken = default)
    {
        var context = session.Connection.HttpContext;
        requestBuilder.TrySetServices(session.Connection.RequestServices);
        requestBuilder.TryAddGlobalState(nameof(CancellationToken), session.Connection.RequestAborted);
        requestBuilder.TryAddGlobalState(nameof(HttpContext), context);
        requestBuilder.TryAddGlobalState(nameof(ClaimsPrincipal), context.User);
        requestBuilder.TryAddGlobalState(nameof(ISocketSession), session);
        requestBuilder.TryAddGlobalState(OperationSessionId, operationSessionId);

        if (context.IsTracingEnabled())
        {
            requestBuilder.TryAddGlobalState(EnableTracing, true);
        }

        if (context.IncludeQueryPlan())
        {
            requestBuilder.TryAddGlobalState(IncludeQueryPlan, true);
        }

        return default;
    }

    public virtual ValueTask<IQueryResult> OnResultAsync(
        ISocketSession session,
        string operationSessionId,
        IQueryResult result,
        CancellationToken cancellationToken = default)
        => new(result);

    public virtual ValueTask OnCompleteAsync(
        ISocketSession session,
        string operationSessionId,
        CancellationToken cancellationToken = default)
        => default;

    public virtual ValueTask<IReadOnlyDictionary<string, object?>?> OnPingAsync(
        ISocketSession session,
        IOperationMessagePayload pingMessage,
        CancellationToken cancellationToken = default)
        => new(default(IReadOnlyDictionary<string, object?>?));


    public virtual ValueTask OnPongAsync(
        ISocketSession session,
        IOperationMessagePayload pongMessage,
        CancellationToken cancellationToken = default)
        => default;

    public virtual ValueTask OnCloseAsync(
        ISocketSession session,
        CancellationToken cancellationToken = default)
        => default;
}
