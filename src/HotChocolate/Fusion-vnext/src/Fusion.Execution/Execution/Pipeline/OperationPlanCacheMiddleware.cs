using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<string, Lazy<TaskCompletionSource<OperationPlan>>> _inFlightPlans =
        new(StringComparer.Ordinal);

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

        var isSingleFlightLeader = false;
        Lazy<TaskCompletionSource<OperationPlan>>? inFlightPlan = null;

        if (_cache.TryGet(operationId, out var plan))
        {
            context.SetOperationPlan(plan);
            _diagnosticEvents.RetrievedOperationPlanFromCache(context, operationId);
        }
        else if (_inFlightPlans.TryGetValue(operationId, out inFlightPlan))
        {
            // Another request is already planning this operation.
            // Await the leader's result to avoid redundant planning work.
            var coalescedPlan = await inFlightPlan.Value.Task
                .WaitAsync(context.RequestAborted)
                .ConfigureAwait(false);
            context.SetOperationPlan(coalescedPlan);
        }
        else
        {
            // No plan is cached and no planning is in progress.
            // Use a Lazy<TCS> so that under burst conditions only one TCS is materialized
            // even if multiple requests race through GetOrAdd concurrently.
            inFlightPlan = new Lazy<TaskCompletionSource<OperationPlan>>(
                static () => new TaskCompletionSource<OperationPlan>(
                    TaskCreationOptions.RunContinuationsAsynchronously));
            var cachedInFlightPlan = _inFlightPlans.GetOrAdd(operationId, inFlightPlan);

            if (ReferenceEquals(cachedInFlightPlan, inFlightPlan))
            {
                // We won the race! This request is the single-flight leader
                // responsible for planning and signaling all followers.
                isSingleFlightLeader = true;
                context.Features.Set(inFlightPlan.Value);
            }
            else
            {
                // We lost the race! Another request claimed leadership between
                // TryGetValue and GetOrAdd. So we simply await the leader's result.
                var coalescedPlan = await cachedInFlightPlan.Value.Task
                    .WaitAsync(context.RequestAborted)
                    .ConfigureAwait(false);
                context.SetOperationPlan(coalescedPlan);
            }
        }

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Propagate the exception to all waiting followers.
            if (isSingleFlightLeader && inFlightPlan is not null)
            {
                inFlightPlan.Value.TrySetException(ex);
            }

            throw;
        }
        finally
        {
            if (isSingleFlightLeader)
            {
                // Guard against a faulty diagnostic event handler preventing cleanup.
                // Without this, a throw from the cache or diagnostics would leak the
                // in-flight entry, causing _inFlightPlans to grow indefinitely.
                try
                {
                    if (context.GetOperationPlan() is { } operationPlan)
                    {
                        // Cache the plan before removing the in-flight entry so that
                        // there is no window where the plan is in neither structure.
                        _cache.TryAdd(operationId, operationPlan);
                        _diagnosticEvents.AddedOperationPlanToCache(context, operationId);
                        inFlightPlan?.Value.TrySetResult(operationPlan);
                    }
                    else if (inFlightPlan?.Value.Task.IsCompleted == false)
                    {
                        // The pipeline completed without producing a plan and without
                        // throwing. Signal followers so they do not hang indefinitely.
                        inFlightPlan.Value.TrySetException(
                            new InvalidOperationException(
                                "The operation plan task completed without a result."));
                    }
                }
                finally
                {
                    _inFlightPlans.TryRemove(operationId, out _);
                }
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
            WellKnownRequestMiddleware.OperationPlanCacheMiddleware);
}
