using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Execution.Pipeline;

internal sealed class PersistedQueryNotFoundMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly Dictionary<string, object?> _statusCode = new() { { HttpStatusCode, 400 }, };
    
    private PersistedQueryNotFoundMiddleware(
        RequestDelegate next,
        [SchemaService] IExecutionDiagnosticEvents diagnosticEvents)
    {
        _next = next
            ?? throw new ArgumentNullException(nameof(next));
        _diagnosticEvents = diagnosticEvents
            ?? throw new ArgumentNullException(nameof(diagnosticEvents));
    }

    public ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Document is not null || context.Request.Document is not null)
        {
            return _next(context);
        }

        var requestedKey =
            context.Request.DocumentId ??
            context.DocumentId ??
            context.DocumentHash ??
            context.Request.DocumentHash;

        // we know that the key is not null since otherwise the request would have
        // failed already since no query is specified.
        var error = ErrorHelper.PersistedQueryNotFound(requestedKey!.Value);
        _diagnosticEvents.RequestError(context, new GraphQLException(error));
        context.Result = OperationResultBuilder.CreateError(error, _statusCode);

        return default;
    }
    
    public static RequestCoreMiddleware Create()
        => (core, next) =>
        {
            var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
            var middleware = new PersistedQueryNotFoundMiddleware(next, diagnosticEvents);
            return context => middleware.InvokeAsync(context);
        };
}
