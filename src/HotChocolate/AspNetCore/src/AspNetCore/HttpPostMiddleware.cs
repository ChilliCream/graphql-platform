using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public sealed class HttpPostMiddleware : HttpPostMiddlewareBase
{
    public HttpPostMiddleware(
        HttpRequestDelegate next,
        IRequestExecutorResolver executorResolver,
        IHttpResponseFormatter responseFormatter,
        IHttpRequestParser requestParser,
        IServerDiagnosticEvents diagnosticEvents,
        string schemaName)
        : base(
            next,
            executorResolver,
            responseFormatter,
            requestParser,
            diagnosticEvents,
            schemaName)
    {
    }
}
