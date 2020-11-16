using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore
{
    public class DefaultHttpRequestInterceptor : IHttpRequestInterceptor
    {
        public virtual ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            requestBuilder.TrySetServices(context.RequestServices);
            requestBuilder.TryAddProperty(nameof(HttpContext), context);
            requestBuilder.TryAddProperty(nameof(ClaimsPrincipal), context.User);
            requestBuilder.TryAddProperty(nameof(CancellationToken), context.RequestAborted);

            if (context.IsTracingEnabled())
            {
                requestBuilder.TryAddProperty(WellKnownContextData.EnableTracing, true);
            }

            return default;
        }
    }
}
