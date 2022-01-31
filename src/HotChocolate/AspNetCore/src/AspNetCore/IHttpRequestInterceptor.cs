using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

public interface IHttpRequestInterceptor
{
    ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken);
}
