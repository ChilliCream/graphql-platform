using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

public delegate ValueTask HttpRequestInterceptorDelegate(
    HttpContext context,
    IRequestExecutor requestExecutor,
    IQueryRequestBuilder requestBuilder,
    CancellationToken cancellationToken);
