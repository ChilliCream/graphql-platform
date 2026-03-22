using HotChocolate.Fusion.Properties;

namespace HotChocolate.Fusion.Execution;

internal static class ThrowHelper
{
    public static InvalidOperationException MissingBooleanVariable(string variableName)
        => new(string.Format(
            FusionExecutionResources.ExecutionNode_MissingBooleanVariable,
            variableName));

    public static KeyNotFoundException NodeNotFound(int id)
        => new(string.Format(
            FusionExecutionResources.OperationPlan_NodeNotFound,
            id));

    public static InvalidOperationException MissingBatchResult(int operationId)
        => new(string.Format(
            FusionExecutionResources.OperationBatchExecutionNode_MissingBatchResult,
            operationId));

    public static InvalidOperationException SingleOperationRequired()
        => new(FusionExecutionResources.JsonOperationPlanParser_SingleOperationRequired);
}
