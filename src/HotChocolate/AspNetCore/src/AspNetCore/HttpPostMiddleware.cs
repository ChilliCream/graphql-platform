using HotChocolate.AspNetCore.Serialization;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public sealed class HttpPostMiddleware : HttpPostMiddlewareBase
{
    public HttpPostMiddleware(
        HttpRequestDelegate next,
        IRequestExecutorResolver executorResolver,
        IHttpResultSerializer resultSerializer,
        IHttpRequestParser requestParser,
        NameString schemaName)
        : base(next, executorResolver, resultSerializer, requestParser, schemaName)
    {
    }
}
