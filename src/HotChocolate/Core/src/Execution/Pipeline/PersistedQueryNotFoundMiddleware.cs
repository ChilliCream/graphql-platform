using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Execution.Pipeline;

internal sealed class PersistedQueryNotFoundMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly Dictionary<string, object?> _statusCode;

    public PersistedQueryNotFoundMiddleware(
        RequestDelegate next,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        _next = next
            ?? throw new ArgumentNullException(nameof(next));
        _diagnosticEvents = diagnosticEvents
            ?? throw new ArgumentNullException(nameof(diagnosticEvents));
        _statusCode = new Dictionary<string, object?>
        {
            { HttpStatusCode, 400 }
        };
    }

    public ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Document is not null || context.Request.Query is not null)
        {
            return _next(context);
        }

        var requestedKey =
            context.Request.QueryId ??
            context.DocumentId ??
            context.DocumentHash ??
            context.Request.QueryHash;

        // we know that the key is not null since otherwise the request would have
        // failed already since no query is specified.
        var error = ErrorHelper.PersistedQueryNotFound(requestedKey!);
        _diagnosticEvents.RequestError(context, new GraphQLException(error));
        context.Result = QueryResultBuilder.CreateError(error, _statusCode);

        return default;
    }
}

internal sealed class OnlyPersistedQueriesAllowedMiddleware
{
    // TODO : ERROR SHOULD BE CONFIGURABLE!
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IQueryResult _errorResult;
    private readonly GraphQLException _exception;
    private readonly IError _error = ErrorHelper.OnlyPersistedQueriesAreAllowed();
    private readonly Dictionary<string, object?> _statusCode = new() { { HttpStatusCode, 400 } };

    public OnlyPersistedQueriesAllowedMiddleware(
        RequestDelegate next,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        _next = next
            ?? throw new ArgumentNullException(nameof(next));
        _diagnosticEvents = diagnosticEvents
            ?? throw new ArgumentNullException(nameof(diagnosticEvents));
        _errorResult = QueryResultBuilder.CreateError(_error, _statusCode);
        _exception = new GraphQLException(_error);
    }

    public ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Request.Query is null || context.ContextData.ContainsKey(NonPersistedQueryAllowed))
        {
            return _next(context);
        }

        // we know that the key is not null since otherwise the request would have
        // failed already since no query is specified.
        _diagnosticEvents.RequestError(context, _exception);
        context.Result = _errorResult;

        return default;
    }

}
