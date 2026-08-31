namespace HotChocolate.Fusion.Planning;

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
