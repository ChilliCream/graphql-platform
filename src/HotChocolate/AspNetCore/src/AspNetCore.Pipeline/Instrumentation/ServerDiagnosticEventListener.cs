using Microsoft.AspNetCore.Http;
using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Instrumentation;

/// <summary>
/// Inherit form this class if you want to receive server diagnostic events.
/// </summary>
[DiagnosticEventSource(typeof(IServerDiagnosticEventListener))]
public class ServerDiagnosticEventListener : IServerDiagnosticEventListener
{
    /// <summary>
    /// A no-op activity scope that can be returned from
    /// event methods that are not interested in when the scope is disposed.
    /// </summary>
    protected internal static IDisposable EmptyScope { get; } = new EmptyActivityScope();

    /// <inheritdoc />
    public virtual IDisposable ExecuteHttpRequest(HttpContext context, HttpRequestKind kind)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void StartSingleRequest(HttpContext context, GraphQLRequest request)
    {
    }

    /// <inheritdoc />
    public virtual void StartBatchRequest(HttpContext context, IReadOnlyList<GraphQLRequest> batch)
    {
    }

    /// <inheritdoc />
    public virtual void StartOperationBatchRequest(
        HttpContext context,
        GraphQLRequest request,
        IReadOnlyList<string> operations)
    {
    }

    /// <inheritdoc />
    public virtual void HttpRequestError(HttpContext context, IError error)
    {
    }

    /// <inheritdoc />
    public virtual void HttpRequestError(HttpContext context, Exception exception)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable ParseHttpRequest(HttpContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void ParserErrors(HttpContext context, IReadOnlyList<IError> errors)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable FormatHttpResponse(HttpContext context, IOperationResult result)
        => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable WebSocketSession(HttpContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void WebSocketSessionError(HttpContext context, Exception exception)
    {
    }

    private sealed class EmptyActivityScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
