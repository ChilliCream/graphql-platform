using System.Collections.Concurrent;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Caching;
using HotChocolate.Fusion.Execution.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationPlanCacheMiddleware
{
    private readonly OperationPlanCache _cache;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
    private readonly ConcurrentDictionary<string, Lazy<TaskCompletionSource<OperationPlan>>> _inFlightPlans =
        new(StringComparer.Ordinal);

    private OperationPlanCacheMiddleware(OperationPlanCache cache, IFusionExecutionDiagnosticEvents diagnosticEvents)
    {
        _cache = cache;
        _diagnosticEvents = diagnosticEvents;
    }

    public async ValueTask InvokeAsync(RequestContext context, RequestDelegate next)
    {
        var operationId = context.GetOperationId();

        var retried = false;
        var resolved = false;
        Lazy<TaskCompletionSource<OperationPlan>>? leaderEntry = null;
        OperationPlanInFlightRelease? inFlightRelease = null;

        // Retry once if the planning leader was cancelled without producing a plan.
        // Recheck the cache because another request may have completed the plan in the meantime.
        while (!resolved)
        {
            if (_cache.TryGetPlan(operationId, out var plan))
            {
                context.SetOperationPlan(plan);
                _diagnosticEvents.RetrievedOperationPlanFromCache(context, operationId);
                resolved = true;
                continue;
            }

            var candidate = new Lazy<TaskCompletionSource<OperationPlan>>(
                static () => new TaskCompletionSource<OperationPlan>(
                    TaskCreationOptions.RunContinuationsAsynchronously));
            var current = _inFlightPlans.GetOrAdd(operationId, candidate);

            if (ReferenceEquals(current, candidate))
            {
                leaderEntry = current;
                inFlightRelease =
                    new OperationPlanInFlightRelease(
                        operationId,
                        current.Value,
                        _cache,
                        _diagnosticEvents);
                context.Features.Set(current.Value);
                context.Features.Set(inFlightRelease);
                resolved = true;
                continue;
            }

            try
            {
                var coalescedPlan = await current.Value.Task
                    .WaitAsync(context.RequestAborted)
                    .ConfigureAwait(false);
                context.SetOperationPlan(coalescedPlan);
                resolved = true;
            }
            catch (OperationCanceledException ex)
                when (!retried && ex.CancellationToken != context.RequestAborted)
            {
                // Remove only the cancelled leader's entry so a replacement leader remains registered.
                retried = true;
                _inFlightPlans.TryRemove(
                    new KeyValuePair<string, Lazy<TaskCompletionSource<OperationPlan>>>(operationId, current));
            }
        }

        if (leaderEntry is null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A downstream failure must not replace a plan already delivered to waiting requests.
            if (!leaderEntry.Value.Task.IsCompleted)
            {
                if (ex is OperationCanceledException oce)
                {
                    leaderEntry.Value.TrySetCanceled(oce.CancellationToken);
                }
                else
                {
                    leaderEntry.Value.TrySetException(ex);
                }
            }

            throw;
        }
        finally
        {
            // Handle plans assigned without SetOperationPlan, or fail waiting requests if no plan was produced.
            try
            {
                // Always remove the in-flight entry, even if caching or a diagnostic listener throws.
                if (!leaderEntry.Value.Task.IsCompleted)
                {
                    if (context.GetOperationPlan() is { } operationPlan)
                    {
                        inFlightRelease!.TryRelease(context, operationPlan);
                    }
                    else
                    {
                        leaderEntry.Value.TrySetException(ThrowHelper.OperationPlanTaskCompletedWithoutResult());
                    }
                }
            }
            finally
            {
                // Remove only this leader's entry; another request may have replaced it.
                _inFlightPlans.TryRemove(
                    new KeyValuePair<string, Lazy<TaskCompletionSource<OperationPlan>>>(
                        operationId,
                        leaderEntry));
            }
        }
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            static (fc, next) =>
            {
                var cache = fc.SchemaServices.GetRequiredService<OperationPlanCache>();
                var diagnosticEvents = fc.SchemaServices.GetRequiredService<IFusionExecutionDiagnosticEvents>();
                var middleware = new OperationPlanCacheMiddleware(cache, diagnosticEvents);
                return requestContext => middleware.InvokeAsync(requestContext, next);
            },
            WellKnownRequestMiddleware.OperationPlanCacheMiddleware);
}
