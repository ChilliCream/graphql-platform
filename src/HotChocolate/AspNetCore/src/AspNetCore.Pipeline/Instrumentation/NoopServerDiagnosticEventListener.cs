using Microsoft.AspNetCore.Http;
using HotChocolate.Language;
using static HotChocolate.AspNetCore.Instrumentation.ServerDiagnosticEventListener;

namespace HotChocolate.AspNetCore.Instrumentation;

internal sealed class NoopServerDiagnosticEventListener : IServerDiagnosticEventListener
{
    public IDisposable ExecuteHttpRequest(HttpContext context, HttpRequestKind kind) => EmptyScope;

    public void StartSingleRequest(HttpContext context, GraphQLRequest request)
    {
    }

    public void StartBatchRequest(HttpContext context, IReadOnlyList<GraphQLRequest> batch)
    {
    }

    public void StartOperationBatchRequest(
        HttpContext context,
        GraphQLRequest request,
        IReadOnlyList<string> operations)
    {
    }

    public void HttpRequestError(HttpContext context, IError error)
    {
    }

    public void HttpRequestError(HttpContext context, Exception exception)
    {
    }

    public IDisposable ParseHttpRequest(HttpContext context) => EmptyScope;

    public void ParserErrors(HttpContext context, IReadOnlyList<IError> errors)
    {
    }

    public IDisposable FormatHttpResponse(HttpContext context, IOperationResult result) => EmptyScope;

    public IDisposable WebSocketSession(HttpContext context) => EmptyScope;

    public void WebSocketSessionError(HttpContext context, Exception exception)
    {
    }
}
