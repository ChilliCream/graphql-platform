using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Utilities
{
    public class DefaultSocketSessionInterceptor : ISocketSessionInterceptor
    {
        public virtual ValueTask<ConnectionStatus> OnConnectAsync(
            ISocketConnection connection,
            InitializeConnectionMessage message,
            CancellationToken cancellationToken) =>
            new ValueTask<ConnectionStatus>(ConnectionStatus.Accept());

        public ValueTask OnRequestAsync(
            ISocketConnection connection,
            IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            HttpContext context = connection.HttpContext;
            requestBuilder.TrySetServices(connection.RequestServices);
            requestBuilder.TryAddProperty(nameof(HttpContext), context);
            requestBuilder.TryAddProperty(nameof(ClaimsPrincipal), context.User);
            requestBuilder.TryAddProperty(nameof(CancellationToken), context.RequestAborted);

            if (connection.HttpContext.IsTracingEnabled())
            {
                requestBuilder.TryAddProperty(ContextDataKeys.EnableTracing, true);
            }

            return default;
        }

        public ValueTask OnCloseAsync(
            ISocketConnection connection,
            CancellationToken cancellationToken) =>
            default;
    }
}
