using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore
{
    public class DefaultSocketSessionInterceptor : ISocketSessionInterceptor
    {
        public virtual ValueTask<ConnectionStatus> OnConnectAsync(
            ISocketConnection connection,
            InitializeConnectionMessage message,
            CancellationToken cancellationToken) =>
            new ValueTask<ConnectionStatus>(ConnectionStatus.Accept());

        public virtual ValueTask OnRequestAsync(
            ISocketConnection connection,
            IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            HttpContext context = connection.HttpContext;
            requestBuilder.TrySetServices(connection.RequestServices);
            requestBuilder.TryAddProperty(nameof(CancellationToken), connection.RequestAborted);
            requestBuilder.TryAddProperty(nameof(HttpContext), context);
            requestBuilder.TryAddProperty(nameof(ClaimsPrincipal), context.User);

            if (connection.HttpContext.IsTracingEnabled())
            {
                requestBuilder.TryAddProperty(WellKnownContextData.EnableTracing, true);
            }

            return default;
        }

        public virtual ValueTask OnCloseAsync(
            ISocketConnection connection,
            CancellationToken cancellationToken) =>
            default;
    }
}
