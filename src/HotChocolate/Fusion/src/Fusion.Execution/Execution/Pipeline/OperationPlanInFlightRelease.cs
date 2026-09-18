using HotChocolate.Caching.Memory;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution.Pipeline;

/// <summary>
/// Held on the leader's request context for as long as its operation plan is in-flight.
/// Whenever a plan is assigned to that context, regardless of which middleware assigns it,
/// this releases every coalesced follower with the plan and caches it, right at that moment
/// instead of waiting for the leader's own downstream execution to finish.
/// </summary>
internal sealed class OperationPlanInFlightRelease
{
    private readonly string _operationId;
    private readonly TaskCompletionSource<OperationPlan> _completionSource;
    private readonly Cache<OperationPlan> _cache;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;

    public OperationPlanInFlightRelease(
        string operationId,
        TaskCompletionSource<OperationPlan> completionSource,
        Cache<OperationPlan> cache,
        IFusionExecutionDiagnosticEvents diagnosticEvents)
    {
        _operationId = operationId;
        _completionSource = completionSource;
        _cache = cache;
        _diagnosticEvents = diagnosticEvents;
    }

    /// <summary>
    /// Caches <paramref name="plan"/> and releases every follower coalesced onto this
    /// operation with it. A no-op once the in-flight entry has already been resolved,
    /// so this is safe to call from more than one place without releasing twice.
    /// </summary>
    public void TryRelease(RequestContext context, OperationPlan plan)
    {
        if (_completionSource.Task.IsCompleted)
        {
            return;
        }

        _cache.TryAdd(_operationId, plan);

        // Followers are released before the diagnostic event is raised so that a faulty
        // listener throwing from it cannot re-admit the stacking wait this exists to avoid.
        _completionSource.TrySetResult(plan);
        _diagnosticEvents.AddedOperationPlanToCache(context, _operationId);
    }
}
