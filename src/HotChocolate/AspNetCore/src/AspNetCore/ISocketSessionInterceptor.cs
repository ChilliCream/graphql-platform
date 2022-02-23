using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

namespace HotChocolate.AspNetCore;

public interface ISocketSessionInterceptor
{
    ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketConnection connection,
        InitializeConnectionMessage message,
        CancellationToken cancellationToken);

    ValueTask OnRequestAsync(
        ISocketConnection connection,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken);

    ValueTask OnCloseAsync(
        ISocketConnection connection,
        CancellationToken cancellationToken);
}

public interface ISocketSessionInterceptor2
{
    ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketConnection connection,
        IProtocolHandler protocolHandler,
        IConnectMessage message,
        CancellationToken cancellationToken);

    ValueTask OnRequestAsync(
        ISocketConnection connection,
        IProtocolHandler protocolHandler,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken);

    ValueTask OnCloseAsync(
        ISocketConnection connection,
        IProtocolHandler protocolHandler,
        CancellationToken cancellationToken);
}
