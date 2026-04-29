using HotChocolate.Execution;
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

    public static InvalidOperationException DeferredSubPlanParentNotFound(SelectionPath path)
        => new(string.Format(
            FusionExecutionResources.OperationPlan_DeferredSubPlanParentNotFound,
            path));

    public static InvalidOperationException MissingBatchResult(int operationId)
        => new(string.Format(
            FusionExecutionResources.OperationBatchExecutionNode_MissingBatchResult,
            operationId));

    public static InvalidOperationException SingleOperationRequired()
        => new(FusionExecutionResources.JsonOperationPlanParser_SingleOperationRequired);

    public static InvalidOperationException RequestIndexOutOfRange(int requestIndex)
        => new(string.Format(
            FusionExecutionResources.HttpSourceSchemaClient_InvalidRequestIndex,
            requestIndex));

    public static InvalidOperationException VariableIndexOutOfRange(int variableIndex)
        => new(string.Format(
            FusionExecutionResources.HttpSourceSchemaClient_VariableIndexOutOfRange,
            variableIndex));

    public static ArgumentException InvalidClientConfiguration(Type expected, Type actual)
        => new($"Expected client configuration of type '{expected.Name}' but received '{actual.Name}'.");
}
