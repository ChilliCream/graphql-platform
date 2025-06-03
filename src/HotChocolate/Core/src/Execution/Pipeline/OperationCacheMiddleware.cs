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
        if (context.GetOperationDocumentInfo().Id.IsEmpty)
        {
            await _next(context).ConfigureAwait(false);
        }
        else
        {
            var addToCache = true;
            var operationId = context.OperationId;

            if (operationId is null)
            {
                operationId = context.CreateCacheId();
                context.OperationId = operationId;
            }

            if (_operationCache.TryGetOperation(operationId, out var operation))
            {
                context.Operation = operation;
                addToCache = false;
                _diagnosticEvents.RetrievedOperationFromCache(context);
            }

            await _next(context).ConfigureAwait(false);

            if (addToCache &&
                context.Operation is not null &&
                !OperationDocumentId.IsNullOrEmpty(context.DocumentId) &&
                context.Document is not null &&
                context.IsValidDocument)
            {
                _operationCache.TryAddOperation(operationId, context.Operation);
                _diagnosticEvents.AddedOperationToCache(context);
            }
        }
    }

    public static RequestCoreMiddlewareConfiguration Create()
        => new RequestCoreMiddlewareConfiguration(
            (core, next) =>
            {
                var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
                var cache = core.SchemaServices.GetRequiredService<IPreparedOperationCache>();
                var middleware = new OperationCacheMiddleware(next, diagnosticEvents, cache);
                return context => middleware.InvokeAsync(context);
            },
            nameof(OperationCacheMiddleware));
}
