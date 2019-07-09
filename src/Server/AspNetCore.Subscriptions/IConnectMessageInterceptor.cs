using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public interface IConnectMessageInterceptor
    {
        Task<ConnectionStatus> OnReceiveAsync(
            ISocketConnection connection,
            InitializeConnectionMessage message,
            CancellationToken cancellationToken);
    }

    public interface ICreateRequestInterceptor
    {
        Task OnCreateAsync(
            ISocketConnection connection,
            IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken);
    }

}
