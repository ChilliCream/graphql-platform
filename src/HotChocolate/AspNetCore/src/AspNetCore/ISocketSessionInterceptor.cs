using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;

namespace HotChocolate.AspNetCore;

public interface ISocketSessionInterceptor
{
    ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketSession session,
        IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken = default);

    ValueTask OnRequestAsync(
        ISocketSession session,
        string operationSessionId,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken = default);

    ValueTask<IQueryResult> OnResultAsync(
        ISocketSession session,
        string operationSessionId,
        IQueryResult result,
        CancellationToken cancellationToken = default);

    ValueTask OnCompleteAsync(
        ISocketSession session,
        string operationSessionId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyDictionary<string, object?>?> OnPingAsync(
        ISocketSession session,
        IOperationMessagePayload pingMessage,
        CancellationToken cancellationToken = default);

    ValueTask OnPongAsync(
        ISocketSession session,
        IOperationMessagePayload pongMessage,
        CancellationToken cancellationToken = default);

    ValueTask OnCloseAsync(
        ISocketSession session,
        CancellationToken cancellationToken = default);
}
