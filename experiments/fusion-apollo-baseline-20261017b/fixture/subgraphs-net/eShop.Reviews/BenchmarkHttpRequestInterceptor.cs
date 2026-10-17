using System.Security.Claims;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;

public sealed class BenchmarkHttpRequestInterceptor : IHttpRequestInterceptor
{
    public ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var userState = new UserState(context.User);

        requestBuilder.Features.Set(userState);
        requestBuilder.Features.Set(context);
        requestBuilder.Features.Set(context.User);

        requestBuilder.TrySetServices(context.RequestServices);
        requestBuilder.TryAddGlobalState(nameof(HttpContext), context);
        requestBuilder.TryAddGlobalState(nameof(ClaimsPrincipal), userState.User);

        return default;
    }
}
