using static HotChocolate.Fusion.Properties.FusionExecutionResources;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Represents a planner budget/guardrail violation.
/// </summary>
public sealed class OperationPlannerGuardrailException : Exception
{
    public OperationPlannerGuardrailException(
        string operationId,
        OperationPlannerGuardrailReason reason,
        long limit,
        long observed)
        : base(string.Format(
            OperationPlannerGuardrailException_GuardrailExceeded,
            operationId,
            reason,
            limit,
            observed))
    {
        ArgumentException.ThrowIfNullOrEmpty(operationId);

        OperationId = operationId;
        Reason = reason;
        Limit = limit;
        Observed = observed;
    }

    /// <summary>
    /// Gets the operation identifier for which the guardrail was exceeded.
    /// </summary>
    public string OperationId { get; }

    /// <summary>
    /// Gets the guardrail reason.
    /// </summary>
    public OperationPlannerGuardrailReason Reason { get; }

    /// <summary>
    /// Gets the configured guardrail limit.
    /// </summary>
    public long Limit { get; }

    /// <summary>
    /// Gets the observed value at breach time.
    /// </summary>
    public long Observed { get; }
}

/// <summary>
/// Identifies which planner guardrail was exceeded.
/// </summary>
public enum OperationPlannerGuardrailReason
{
    MaxPlanningTimeExceeded,
    MaxExpandedNodesExceeded,
    MaxQueueSizeExceeded,
    MaxGeneratedOptionsPerWorkItemExceeded
}
