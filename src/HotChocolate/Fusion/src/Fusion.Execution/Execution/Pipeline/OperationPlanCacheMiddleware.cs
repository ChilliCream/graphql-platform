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
        var operationId = context.GetOperationId();

        if (_cache.TryGet(operationId, out var plan))
        {
            context.SetOperationPlan(plan);
            _diagnosticEvents.RetrievedOperationPlanFromCache(context, operationId);
            await next(context).ConfigureAwait(false);
            return;
        }

        var retried = false;
        var resolved = false;
        Lazy<TaskCompletionSource<OperationPlan>>? leaderEntry = null;
        Lazy<TaskCompletionSource<OperationPlan>>? cancelledLeader = null;

        // A follower whose leader is cancelled before it produces a plan gets exactly one
        // opportunity to step up as the new leader candidate instead of failing outright;
        // any cancellation after that (including one observed on the retry) propagates.
        while (!resolved)
        {
            var candidate = new Lazy<TaskCompletionSource<OperationPlan>>(
                static () => new TaskCompletionSource<OperationPlan>(
                    TaskCreationOptions.RunContinuationsAsynchronously));
            var current = _inFlightPlans.GetOrAdd(operationId, candidate);

            if (ReferenceEquals(current, candidate))
            {
                leaderEntry = current;
                context.Features.Set(current.Value);
                resolved = true;
                continue;
            }

            if (ReferenceEquals(current, cancelledLeader))
            {
                // The leadership decision was already made (this request is retrying as a
                // candidate); the cancelled leader's own entry just has not been removed
                // yet. Yield for its cleanup to run and look again - this is not another
                // retry of the operation, only a wait for that removal to land.
                await Task.Yield();
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
                // The leader was cancelled before it produced a plan; this request's own
                // token was not the cause, so it retries once as a leader candidate.
                retried = true;
                cancelledLeader = current;
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
        finally
        {
            // The leader alone owns this entry: added it, and removes it here regardless of
            // whether planning succeeded, failed, or was cancelled.
            _inFlightPlans.TryRemove(
                new KeyValuePair<string, Lazy<TaskCompletionSource<OperationPlan>>>(operationId, leaderEntry));
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
