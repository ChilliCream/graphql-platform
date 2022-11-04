using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OnlyPersistedQueriesAllowedMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly bool _allowAllQueries;
    private readonly IQueryResult _errorResult;
    private readonly GraphQLException _exception;
    private readonly Dictionary<string, object?> _statusCode = new()
    {
        { WellKnownContextData.HttpStatusCode, 400 }
    };

    public OnlyPersistedQueriesAllowedMiddleware(
        RequestDelegate next,
        IExecutionDiagnosticEvents diagnosticEvents,
        IPersistedQueryOptionsAccessor options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _next = next
            ?? throw new ArgumentNullException(nameof(next));
        _diagnosticEvents = diagnosticEvents
            ?? throw new ArgumentNullException(nameof(diagnosticEvents));

        // prepare options.
        _allowAllQueries = !options.OnlyAllowPersistedQueries;
        var error = options.OnlyPersistedQueriesAreAllowedError;
        _errorResult =  QueryResultBuilder.CreateError(error, _statusCode);
        _exception = new GraphQLException(error);
    }

    public ValueTask InvokeAsync(IRequestContext context)
    {
        if (_allowAllQueries ||
            context.Request.Query is null ||
            context.ContextData.ContainsKey(WellKnownContextData.NonPersistedQueryAllowed))
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
