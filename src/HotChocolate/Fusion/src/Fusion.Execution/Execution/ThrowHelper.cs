using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Properties;

namespace HotChocolate.Fusion.Execution;

internal static class ThrowHelper
{
    public static InvalidOperationException AuthorizationPolicyNameEmpty()
        => new(FusionExecutionResources.AuthorizationPolicyCollection_PolicyNameEmpty);

    public static InvalidOperationException AuthorizationPolicyNameDuplicate(string policyName)
        => new(string.Format(
            FusionExecutionResources.AuthorizationPolicyCollection_PolicyNameDuplicate,
            policyName));

    public static KeyNotFoundException AuthorizationPolicyNotFound(string policyName)
        => new(string.Format(
            FusionExecutionResources.AuthorizationPolicyCollection_PolicyNameNotFound,
            policyName));

    public static InvalidOperationException MissingBooleanVariable(string variableName)
        => new(string.Format(
            FusionExecutionResources.ExecutionNode_MissingBooleanVariable,
            variableName));

    public static KeyNotFoundException NodeNotFound(int id)
        => new(string.Format(
            FusionExecutionResources.OperationPlan_NodeNotFound,
            id));

    public static InvalidOperationException IncrementalPlanParentNotFound(SelectionPath path)
        => new(string.Format(
            FusionExecutionResources.OperationPlan_IncrementalPlanParentNotFound,
            path));

    public static InvalidOperationException MissingBatchResult(int operationId)
        => new(string.Format(
            FusionExecutionResources.OperationBatchExecutionNode_MissingBatchResult,
            operationId));

    public static InvalidOperationException NodeLookupNotFound(string typeName)
        => new(string.Format(
            FusionExecutionResources.PlanQueue_NodeLookupNotFound,
            typeName));

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

    public static InvalidOperationException InvalidTargetValueKind(
        SelectionPath selectionPath,
        Path resultPath,
        JsonValueKind valueKind)
        => new(string.Format(
            FusionExecutionResources.FetchResultStore_InvalidTargetValueKind,
            selectionPath,
            resultPath,
            valueKind));

    public static InvalidOperationException InvalidRepresentationResultKind(
        SelectionPath sourcePath,
        JsonValueKind valueKind)
        => new(string.Format(
            FusionExecutionResources.FetchResultStore_InvalidRepresentationResultKind,
            sourcePath,
            valueKind));

    public static InvalidOperationException RepresentationResultCountMismatch(
        SelectionPath sourcePath,
        int actualCount,
        int expectedCount)
        => new(string.Format(
            FusionExecutionResources.FetchResultStore_RepresentationResultCountMismatch,
            sourcePath,
            actualCount,
            expectedCount));
}
