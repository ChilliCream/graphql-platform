using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using static HotChocolate.ExecutionContextData;

namespace HotChocolate.AspNetCore;

/// <summary>
/// The HTTP request interceptor allows to manipulate the GraphQL
/// request creation and the GraphQL request response creation.
/// </summary>
public class DefaultHttpRequestInterceptor : IHttpRequestInterceptor
{
    /// <inheritdoc cref="IHttpRequestInterceptor.OnCreateAsync"/>
    public virtual ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var userState = new UserState(context.User);

        if (context.Features.Get<IFileLookup>() is { } featureLookup)
        {
            requestBuilder.Features.Set(featureLookup);
        }

        requestBuilder.Features.Set(userState);
        requestBuilder.Features.Set(context);
        requestBuilder.Features.Set(context.User);

        requestBuilder.TrySetServices(context.RequestServices);
        requestBuilder.TryAddGlobalState(nameof(HttpContext), context);
        requestBuilder.TryAddGlobalState(nameof(ClaimsPrincipal), userState.User);

        if (context.IncludeOperationPlan())
        {
            requestBuilder.TryAddGlobalState(IncludeOperationPlan, true);
        }

        if (context.TryGetCostSwitch() is { } costSwitch)
        {
            requestBuilder.TryAddGlobalState(costSwitch, true);
        }

        return default;
    }
}
