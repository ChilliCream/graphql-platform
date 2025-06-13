using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationCacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IPreparedOperationCache _operationCache;

    private OperationCacheMiddleware(
        RequestDelegate next,
        [SchemaService] IExecutionDiagnosticEvents diagnosticEvents,
        [SchemaService] IPreparedOperationCache operationCache)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);
        ArgumentNullException.ThrowIfNull(operationCache);

        _next = next;
        _diagnosticEvents = diagnosticEvents;
        _operationCache = operationCache;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        var documentId = context.GetOperationDocumentId();

        if (documentId.IsEmpty)
        {
            await _next(context).ConfigureAwait(false);
        }
        else
        {
            var addToCache = true;
            if (!context.TryGetOperationId(out var operationId))
            {
                operationId = context.CreateCacheId();
                context.SetOperationId(operationId);
            }

            if (_operationCache.TryGetOperation(operationId, out var operation))
            {
                context.SetOperation(operation);
                addToCache = false;
                _diagnosticEvents.RetrievedOperationFromCache(context);
            }

            await _next(context).ConfigureAwait(false);

            if (addToCache && context.TryGetOperation(out operation))
            {
                _operationCache.TryAddOperation(operation.Id, operation);
                _diagnosticEvents.AddedOperationToCache(context);
            }
        }
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
                var cache = core.SchemaServices.GetRequiredService<IPreparedOperationCache>();
                var middleware = new OperationCacheMiddleware(next, diagnosticEvents, cache);
                return context => middleware.InvokeAsync(context);
            },
            nameof(OperationCacheMiddleware));
}
