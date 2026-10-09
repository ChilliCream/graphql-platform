using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ThrowHelper = HotChocolate.Fusion.Execution.ThrowHelper;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Tracks the configured planner guardrails and the plan improvement allowance for a single
/// operation across the greedy pass, the main search and every deferred search.
/// </summary>
internal sealed class PlanningBudget
{
    private readonly TimeSpan? _maxPlanningTime;
    private readonly int? _maxExpandedNodes;
    private readonly int? _maxQueueSize;
    private readonly int? _maxGeneratedOptionsPerWorkItem;
    private readonly int? _maxPlanImprovementNodes;
    private readonly long _startedAt;
    private int _expandedNodes;
    private int _improvementNodes;

    public PlanningBudget(OperationPlannerOptions options, bool emitPlannerEvents)
    {
        ArgumentNullException.ThrowIfNull(options);

        _maxPlanningTime = options.MaxPlanningTime;
        _maxExpandedNodes = options.MaxExpandedNodes;
        _maxQueueSize = options.MaxQueueSize;
        _maxGeneratedOptionsPerWorkItem = options.MaxGeneratedOptionsPerWorkItem;
        _maxPlanImprovementNodes = options.MaxPlanImprovementNodes;
        _startedAt = _maxPlanningTime.HasValue ? Stopwatch.GetTimestamp() : 0L;
        EmitPlannerEvents = emitPlannerEvents;
    }

    /// <summary>
    /// Gets a value indicating whether planner events are written for this operation.
    /// </summary>
    public bool EmitPlannerEvents { get; }

    /// <summary>
    /// Counts one expanded node and throws when the planning time or the expanded node limit is exceeded.
    /// </summary>
    public void CountExpansion(string operationId)
    {
        _expandedNodes++;

        if (_maxPlanningTime is { } planningTimeLimit)
        {
            var elapsed = Stopwatch.GetElapsedTime(_startedAt);

            if (elapsed >= planningTimeLimit)
            {
                ThrowExceeded(
                    operationId,
                    OperationPlannerGuardrailReason.MaxPlanningTimeExceeded,
                    ToMilliseconds(planningTimeLimit),
                    ToMilliseconds(elapsed));
            }
        }

        if (_maxExpandedNodes is { } expandedNodesLimit && _expandedNodes > expandedNodesLimit)
        {
            ThrowExceeded(
                operationId,
                OperationPlannerGuardrailReason.MaxExpandedNodesExceeded,
                expandedNodesLimit,
                _expandedNodes);
        }
    }

    /// <summary>
    /// Counts one node expanded by a search that already holds a complete plan and returns
    /// <c>false</c> when the plan improvement allowance of the operation is spent.
    /// </summary>
    public bool TryCountImprovement(string operationId)
    {
        if (_maxPlanImprovementNodes is not { } improvementLimit)
        {
            return true;
        }

        if (_improvementNodes >= improvementLimit)
        {
            if (EmitPlannerEvents)
            {
                PlannerEventSource.Log.PlanImprovementBudgetExhausted(operationId, improvementLimit);
            }

            return false;
        }

        _improvementNodes++;
        return true;
    }

    /// <summary>
    /// Throws when the queue size exceeds the configured limit.
    /// </summary>
    public void EnsureQueueSize(string operationId, int queueSize)
    {
        if (_maxQueueSize is { } queueSizeLimit && queueSize > queueSizeLimit)
        {
            ThrowExceeded(
                operationId,
                OperationPlannerGuardrailReason.MaxQueueSizeExceeded,
                queueSizeLimit,
                queueSize);
        }
    }

    /// <summary>
    /// Throws when a single work item generated more options than the configured limit.
    /// </summary>
    public void EnsureGeneratedOptions(string operationId, int generatedOptions)
    {
        if (_maxGeneratedOptionsPerWorkItem is { } generatedOptionsLimit
            && generatedOptions > generatedOptionsLimit)
        {
            ThrowExceeded(
                operationId,
                OperationPlannerGuardrailReason.MaxGeneratedOptionsPerWorkItemExceeded,
                generatedOptionsLimit,
                generatedOptions);
        }
    }

    [DoesNotReturn]
    private void ThrowExceeded(
        string operationId,
        OperationPlannerGuardrailReason reason,
        long limit,
        long observed)
    {
        if (EmitPlannerEvents)
        {
            PlannerEventSource.Log.PlanGuardrailExceeded(
                operationId,
                reason.ToString(),
                limit,
                observed);
        }

        throw ThrowHelper.PlanningGuardrailExceeded(operationId, reason, limit, observed);
    }

    private static long ToMilliseconds(TimeSpan value)
        => checked((long)Math.Ceiling(value.TotalMilliseconds));
}
