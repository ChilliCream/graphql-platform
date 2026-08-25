using HotChocolate.Execution;
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

/// <summary>
/// Thrown when a <c>@defer</c>d fragment is anchored on a mutation result whose
/// type cannot be re-resolved through any lookup in the composite schema. The
/// incremental plan would have no way to source the deferred fields other than
/// re-running the mutation root field a second time, which would duplicate its
/// side effects, so planning fails instead of doing that silently.
/// </summary>
public sealed class DeferredMutationLookupRequiredException : Exception
{
    public DeferredMutationLookupRequiredException(SelectionPath path, string typeName)
        : base(
            $"The @defer fragment at path '{path}' cannot be planned: its anchor type "
            + $"'{typeName}' has no lookup in the composite schema, so the deferred fields "
            + "could only be sourced by re-running the mutation root field a second time, "
            + "which would duplicate its side effects.")
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        Path = path;
        TypeName = typeName;
    }

    /// <summary>
    /// Gets the selection path of the unresolvable defer anchor.
    /// </summary>
    public SelectionPath Path { get; }

    /// <summary>
    /// Gets the name of the anchor type that has no lookup.
    /// </summary>
    public string TypeName { get; }
}
