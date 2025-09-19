using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

internal sealed class DelegateHttpRequestInterceptor(
    Func<HttpContext, IRequestExecutor, OperationRequestBuilder, CancellationToken, ValueTask> handler)
    : DefaultHttpRequestInterceptor
{
    public override async ValueTask OnCreateAsync(HttpContext context, IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        await handler(context, requestExecutor, requestBuilder, cancellationToken);
        await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}
