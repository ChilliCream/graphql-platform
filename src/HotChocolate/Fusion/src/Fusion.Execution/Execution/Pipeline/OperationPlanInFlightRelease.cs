using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Caching;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution.Pipeline;

/// <summary>
/// Makes a completed operation plan available to requests waiting for it.
/// </summary>
internal sealed class OperationPlanInFlightRelease
{
    private readonly string _operationId;
    private readonly TaskCompletionSource<OperationPlan> _completionSource;
    private readonly OperationPlanCache _cache;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;

    public OperationPlanInFlightRelease(
        string operationId,
        TaskCompletionSource<OperationPlan> completionSource,
        OperationPlanCache cache,
        IFusionExecutionDiagnosticEvents diagnosticEvents)
    {
        _operationId = operationId;
        _completionSource = completionSource;
        _cache = cache;
        _diagnosticEvents = diagnosticEvents;
    }

    /// <summary>
    /// Caches the plan and completes waiting requests with it.
    /// Does nothing if those requests have already received a result or error.
    /// </summary>
    public void TryRelease(RequestContext context, OperationPlan plan)
    {
        if (_completionSource.Task.IsCompleted)
        {
            return;
        }

        _cache.TryAddPlan(_operationId, plan);

        // Release waiting requests before diagnostics so a throwing listener cannot leave them blocked.
        _completionSource.TrySetResult(plan);
        _diagnosticEvents.AddedOperationPlanToCache(context, _operationId);
    }
}
