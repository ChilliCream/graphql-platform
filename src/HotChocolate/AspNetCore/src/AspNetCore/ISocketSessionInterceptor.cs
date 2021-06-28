using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore
{
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
}
