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

        var retried = false;
        var resolved = false;
        Lazy<TaskCompletionSource<OperationPlan>>? leaderEntry = null;

        // A follower whose leader is cancelled before it produces a plan evicts the
        // cancelled leader's entry and gets exactly one opportunity to step up as the new
        // leader candidate instead of failing outright; any cancellation after that
        // (including one observed on the retry) propagates. Re-checking the cache on every
        // iteration also covers the plan having been produced and cached by someone else
        // while this request was coalesced onto the now-cancelled leader.
        while (!resolved)
        {
            if (_cache.TryGet(operationId, out var plan))
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
                context.Features.Set(current.Value);
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
                // The leader was cancelled before it produced a plan; this request's own
                // token was not the cause, so it evicts the cancelled leader's entry and
                // retries once as a leader candidate.
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
            // Propagate the failure to followers only if nothing has resolved the TCS yet
            // (OperationPlanMiddleware already completes it once a plan is produced).
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
            // OperationPlanMiddleware already caches the plan and releases followers right
            // after planning succeeds; that is the primary path and this is a no-op then.
            // This is the fallback for a plan that reached the context some other way (e.g.
            // a custom middleware ahead of OperationPlanMiddleware): cache whatever plan the
            // context carries when the pipeline returns and release followers with it. Only
            // when the pipeline returned without producing a plan at all, and without
            // throwing, does this fault the followers so they do not wait forever.
            try
            {
                // Guard against a faulty diagnostic event handler preventing cleanup: a throw
                // from the cache or a listener here must not leak the in-flight entry.
                if (!leaderEntry.Value.Task.IsCompleted)
                {
                    if (context.GetOperationPlan() is { } operationPlan)
                    {
                        _cache.TryAdd(operationId, operationPlan);
                        _diagnosticEvents.AddedOperationPlanToCache(context, operationId);
                        leaderEntry.Value.TrySetResult(operationPlan);
                    }
                    else
                    {
                        leaderEntry.Value.TrySetException(ThrowHelper.OperationPlanTaskCompletedWithoutResult());
                    }
                }
            }
            finally
            {
                // The leader alone owns this entry: added it, and removes it here regardless of
                // whether planning succeeded, failed, or was cancelled.
                _inFlightPlans.TryRemove(
                    new KeyValuePair<string, Lazy<TaskCompletionSource<OperationPlan>>>(operationId, leaderEntry));
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
