using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Utilities;

public class TestSocketSessionInterceptor : DefaultSocketSessionInterceptor
{
    public List<IReadOnlyQueryRequest> Requests { get; } = new();

    public bool ConnectWasCalled { get; private set; }

    public bool CloseWasCalled { get; private set; }

    public override ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketConnection connection,
        InitializeConnectionMessage message,
        CancellationToken cancellationToken)
    {
        ConnectWasCalled = true;
        return base.OnConnectAsync(connection, message, cancellationToken);
    }

    public override ValueTask OnRequestAsync(
        ISocketConnection connection,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        Requests.Add(requestBuilder.Create());
        return base.OnRequestAsync(connection, requestBuilder, cancellationToken);
    }

    public override ValueTask OnCloseAsync(
        ISocketConnection connection,
        CancellationToken cancellationToken)
    {
        CloseWasCalled = true;
        return base.OnCloseAsync(connection, cancellationToken);
    }
}
