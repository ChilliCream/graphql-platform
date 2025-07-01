using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public sealed class HttpPostMiddleware(
    HttpRequestDelegate next,
    IRequestExecutorProvider executorProvider,
    IRequestExecutorEvents executorEvents,
    IHttpResponseFormatter responseFormatter,
    IHttpRequestParser requestParser,
    IServerDiagnosticEvents diagnosticEvents,
    string schemaName)
    : HttpPostMiddlewareBase(
        next,
        executorProvider,
        executorEvents,
        responseFormatter,
        requestParser,
        diagnosticEvents,
        schemaName);
