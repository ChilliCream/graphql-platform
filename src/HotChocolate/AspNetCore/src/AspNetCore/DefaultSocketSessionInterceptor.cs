using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;

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
        HttpContext context = session.Connection.HttpContext;
        requestBuilder.TrySetServices(session.Connection.RequestServices);
        requestBuilder.TryAddProperty(nameof(CancellationToken), session.Connection.RequestAborted);
        requestBuilder.TryAddProperty(nameof(HttpContext), context);
        requestBuilder.TryAddProperty(nameof(ClaimsPrincipal), context.User);
        requestBuilder.TryAddProperty(nameof(ISocketSession), session);

        if (context.IsTracingEnabled())
        {
            requestBuilder.TryAddProperty(WellKnownContextData.EnableTracing, true);
        }

        if (context.IncludeQueryPlan())
        {
            requestBuilder.TryAddProperty(WellKnownContextData.IncludeQueryPlan, true);
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
