using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Features;
using HotChocolate.Language;
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
        var documentInfo = context.GetOperationDocumentInfo();

        if (documentInfo.Id.IsEmpty)
        {
            await _next(context).ConfigureAwait(false);
        }
        else
        {
            var addToCache = true;
            var operationInfo = context.GetOperationInfo();

            operationInfo.OperationId ??= context.CreateCacheId();

            if (_operationCache.TryGetOperation(operationInfo.OperationId, out var operation))
            {
                operationInfo.Operation = operation;
                addToCache = false;
                _diagnosticEvents.RetrievedOperationFromCache(context);
            }

            await _next(context).ConfigureAwait(false);

            if (addToCache
                && !documentInfo.Id.IsEmpty
                && documentInfo.Document is not null
                && documentInfo.IsValidated
                && operationInfo.Operation is not null)
            {
                _operationCache.TryAddOperation(operationInfo.OperationId, operationInfo.Operation);
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

public sealed class OperationInfo : RequestContextFeature
{
    public string? OperationId { get; set; }

    public OperationDefinitionNode? OperationDefinition { get; set; }

    public IOperation? Operation { get; set; }

    public override void Reset()
    {
        OperationId = null;
        OperationDefinition = null;
        Operation = null;
    }
}

public static class HotChocolateExecutionRequestContextExtensions
{
    public static OperationInfo GetOperationInfo(
        this RequestContext context)
        => context.Features.GetOrSet<OperationInfo>();
}
