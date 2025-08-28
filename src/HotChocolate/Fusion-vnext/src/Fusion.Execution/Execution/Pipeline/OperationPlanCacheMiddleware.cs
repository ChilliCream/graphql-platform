using HotChocolate.Caching.Memory;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationPlanCacheMiddleware
{
    private readonly Cache<OperationPlan> _cache;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;

    private OperationPlanCacheMiddleware(Cache<OperationPlan> cache, IFusionExecutionDiagnosticEvents diagnosticEvents)
    {
        _cache = cache;
        _diagnosticEvents = diagnosticEvents;
    }

    public async ValueTask InvokeAsync(RequestContext context, RequestDelegate next)
    {
        var documentInfo = context.OperationDocumentInfo;

        if (documentInfo.Hash.IsEmpty)
        {
            context.Result = ErrorHelper.StateInvalidForOperationPlanCache();
            return;
        }

        var operationId = documentInfo.OperationCount == 1
            ? documentInfo.Hash.Value
            : $"{documentInfo.Hash.Value}.{context.Request.OperationName ?? "Default"}";
        context.SetOperationId(operationId);

        var isPlanCached = false;

        if (_cache.TryGet(operationId, out var plan))
        {
            context.SetOperationPlan(plan);
            isPlanCached = true;
            _diagnosticEvents.RetrievedOperationPlanFromCache(context, operationId);
        }

        await next(context);

        if (!isPlanCached)
        {
            // We retrieve the execution plan from the context.
            // If there is no execution plan, we can exit early as something must have
            // gone wrong in the pipeline. If we get, however, an execution plan,
            // we try to cache it.
            var executionPlan = context.GetOperationPlan();

            if (executionPlan is not null)
            {
                _cache.TryAdd(operationId, executionPlan);
                _diagnosticEvents.AddedOperationPlanToCache(context, operationId);
            }
        }
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            static (fc, next) =>
            {
                var cache = fc.SchemaServices.GetRequiredService<Cache<OperationPlan>>();
                var diagnosticEvents = fc.SchemaServices.GetRequiredService<IFusionExecutionDiagnosticEvents>();
                var middleware = new OperationPlanCacheMiddleware(cache, diagnosticEvents);
                return requestContext => middleware.InvokeAsync(requestContext, next);
            },
            nameof(OperationPlanCacheMiddleware));
}
