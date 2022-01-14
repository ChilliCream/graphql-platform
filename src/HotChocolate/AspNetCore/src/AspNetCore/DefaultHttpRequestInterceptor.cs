using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

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

        if (context.IncludeQueryPlan())
        {
            requestBuilder.TryAddProperty(WellKnownContextData.IncludeQueryPlan, true);
        }

        return default;
    }
}
