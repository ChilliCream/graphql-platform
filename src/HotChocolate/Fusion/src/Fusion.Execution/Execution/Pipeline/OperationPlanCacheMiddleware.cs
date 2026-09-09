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
    private readonly ConcurrentDictionary<string, Lazy<TaskCompletionSource<OperationPlan?>>> _inFlightPlans =
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

        Lazy<TaskCompletionSource<OperationPlan?>>? inFlightPlan = null;

        while (true)
        {
            if (_cache.TryGet(operationId, out var plan))
            {
                context.SetOperationPlan(plan);
                _diagnosticEvents.RetrievedOperationPlanFromCache(context, operationId);
                break;
            }

            var candidate = new Lazy<TaskCompletionSource<OperationPlan?>>(
                static () => new TaskCompletionSource<OperationPlan?>(
                    TaskCreationOptions.RunContinuationsAsynchronously));
            var current = _inFlightPlans.GetOrAdd(operationId, candidate);

            if (ReferenceEquals(current, candidate))
            {
                inFlightPlan = candidate;
                context.Features.Set(candidate.Value);
                break;
            }

            var coalescedPlan = await current.Value.Task
                .WaitAsync(context.RequestAborted)
                .ConfigureAwait(false);

            if (coalescedPlan is not null)
            {
                context.SetOperationPlan(coalescedPlan);
                break;
            }
        }

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            if (inFlightPlan is not null)
            {
                RemoveInFlightPlan(operationId, inFlightPlan);

                if (ex is OperationCanceledException cancellationException)
                {
                    inFlightPlan.Value.TrySetCanceled(cancellationException.CancellationToken);
                }
                else
                {
                    inFlightPlan.Value.TrySetException(ex);
                }
            }

            throw;
        }

        if (inFlightPlan is not null)
        {
            try
            {
                if (context.GetOperationPlan() is { } operationPlan)
                {
                    _cache.TryAdd(operationId, operationPlan);
                    _diagnosticEvents.AddedOperationPlanToCache(context, operationId);
                    RemoveInFlightPlan(operationId, inFlightPlan);
                    inFlightPlan.Value.TrySetResult(operationPlan);
                }
                else
                {
                    RemoveInFlightPlan(operationId, inFlightPlan);
                    inFlightPlan.Value.TrySetResult(null);
                }
            }
            catch (Exception ex)
            {
                RemoveInFlightPlan(operationId, inFlightPlan);

                if (ex is OperationCanceledException cancellationException)
                {
                    inFlightPlan.Value.TrySetCanceled(cancellationException.CancellationToken);
                }
                else
                {
                    inFlightPlan.Value.TrySetException(ex);
                }

                throw;
            }
        }
    }

    private void RemoveInFlightPlan(
        string operationId,
        Lazy<TaskCompletionSource<OperationPlan?>> inFlightPlan)
        => ((ICollection<KeyValuePair<string, Lazy<TaskCompletionSource<OperationPlan?>>>>)_inFlightPlans)
            .Remove(new KeyValuePair<string, Lazy<TaskCompletionSource<OperationPlan?>>>(operationId, inFlightPlan));

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
