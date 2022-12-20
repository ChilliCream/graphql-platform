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
