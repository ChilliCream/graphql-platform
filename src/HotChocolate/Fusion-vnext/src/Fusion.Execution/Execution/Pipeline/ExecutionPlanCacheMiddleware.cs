using HotChocolate.Caching.Memory;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

public class ExecutionPlanCacheMiddleware(Cache<OperationPlan> cache)
{
    private readonly Cache<OperationPlan> _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public async ValueTask InvokeAsync(RequestContext context, RequestDelegate next)
    {
        var documentInfo = context.OperationDocumentInfo;
        var planKey = $"{documentInfo.Hash}.{context.Request.OperationName ?? "Default"}";
        var isPlanCached = false;

        if(_cache.TryGet(planKey, out var plan))
        {
            context.SetExecutionPlan(plan);
            isPlanCached = true;
        }

        await next(context);

        // if the plan is already cache, we can exit early.
        if (isPlanCached)
        {
            return;
        }

        // Otherwise we retrieve the execution plan from the context.
        // If there is no execution plan, we can exit early as something must have
        // gone wrong in the pipeline. If we get, however, an execution plan,
        // we try to cache it.
        var executionPlan = context.GetExecutionPlan();

        if (executionPlan is not null)
        {
            _cache.TryAdd(planKey, executionPlan);
        }
    }

    public static RequestMiddleware Create()
    {
        return static (factoryContext, next) =>
        {
            var cache = factoryContext.Services.GetRequiredService<Cache<OperationPlan>>();
            var middleware = new ExecutionPlanCacheMiddleware(cache);
            return requestContext => middleware.InvokeAsync(requestContext, next);
        };
    }
}
