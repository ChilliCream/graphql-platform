using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Server
{
    public interface ISocketConnectionInterceptor<TContext>
    {
        Task<ConnectionStatus> OnConnectWebSocketAsync(
            TContext requestContext,
            IReadOnlyDictionary<string, object> properties,
            CancellationToken cancellationToken);
    }

    public interface IQueryRequestInterceptor<TContext>
    {
        Task OnCreateRequestAsync(
            TContext requestContext,
            IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken);
    }
}
