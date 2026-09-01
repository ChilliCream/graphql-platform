using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Properties;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal static class ThrowHelper
{
    public static InvalidOperationException PolicyNameEmpty()
        => new(FusionExecutionResources.PolicyCollection_PolicyNameEmpty);

    public static InvalidOperationException PolicyNameDuplicate(string policyName)
        => new(string.Format(
            FusionExecutionResources.PolicyCollection_PolicyNameDuplicate,
            policyName));

    public static KeyNotFoundException PolicyNameNotFound(string policyName)
        => new(string.Format(
            FusionExecutionResources.PolicyCollection_PolicyNameNotFound,
            policyName));

    public static InvalidOperationException PolicyRequirementsChanged(string policyName)
        => new($"Authorization policy '{policyName}' requirements changed after planning.");

    public static InvalidOperationException PolicyOperationPlanMissing()
        => new("There is no operation plan available for policy evaluation.");

    public static InvalidOperationException MissingBooleanVariable(string variableName)
        => new(string.Format(
            FusionExecutionResources.ExecutionNode_MissingBooleanVariable,
            variableName));

    public static KeyNotFoundException NodeNotFound(int id)
        => new(string.Format(
            FusionExecutionResources.OperationPlan_NodeNotFound,
            id));

    public static InvalidOperationException InvalidOperationPlan(string message)
        => new(message);

    public static InvalidOperationException IncrementalPlanParentNotFound(SelectionPath path)
        => new(string.Format(
            FusionExecutionResources.OperationPlan_IncrementalPlanParentNotFound,
            path));

    public static InvalidOperationException DeferredPolicyTargetNotSupported(string coordinate)
        => new(
            "The deferred incremental plan contains an uncovered policy target at "
            + $"'{coordinate}'. Data-bearing deferred policy targets require deferred policy planning support.");

    public static InvalidOperationException DeferredSelectionPathCannotBeRebased(string responseName)
        => new(
            $"The deferred result path segment '{responseName}' cannot be resolved in the child operation.");

    public static InvalidOperationException DeferredRequirementNotImported()
        => new(
            "A deferred incremental plan fetch references a requirement that was not imported.");

    public static InvalidOperationException MixedDeferredRequirementScopes(
        IEnumerable<string> imported,
        IEnumerable<string> local)
        => new(
            "A deferred incremental plan fetch references a mix of imported parent-sourced and local "
            + "requirement keys. Imported parent keys: ["
            + string.Join(", ", imported)
            + "]. Local requested keys: ["
            + string.Join(", ", local)
            + "].");

    public static InvalidOperationException PolicyRequirementAuthorizationCycle(
        string policyName,
        string coordinate)
        => new(
            $"Authorization policy '{policyName}' requires protected field "
            + $"'{coordinate}', which would create an authorization cycle.");

    public static InvalidOperationException UnsupportedPolicyRequirementSelection(string policyName)
        => new($"Authorization policy '{policyName}' has an unsupported requirement selection.");

    public static InvalidOperationException PolicyRequirementFieldMissing(
        string policyName,
        string responseName)
        => new(
            $"Authorization policy '{policyName}' requires field '{responseName}', "
            + "but the execution plan did not provide it.");

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

    public static InvalidOperationException InvalidAliasBatchResponse(string schemaName)
        => new(string.Format(
            FusionExecutionResources.HttpSourceSchemaClient_InvalidAliasBatchResponse,
            schemaName));

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

    public static GraphQLException VariableNotFound(
        string variableName) =>
        new(ErrorBuilder.New()
            .SetMessage(
                "The variable with the name `{0}` does not exist.",
                variableName)
            .Build());

    public static GraphQLException VariableNotOfType(
        string variableName,
        Type type) =>
        new(ErrorBuilder.New()
            .SetMessage(
                "The variable with the name `{0}` is not of the requested type `{1}`.",
                variableName,
                type.FullName ?? string.Empty)
            .Build());

    public static GraphQLException NonNullVariableIsNull(
        VariableDefinitionNode variableDefinition)
    {
        return new(
            ErrorBuilder.New()
                .SetMessage(
                    "Variable `{0}` is required.",
                    variableDefinition.Variable.Name.Value)
                .SetCode(ErrorCodes.Execution.NonNullViolation)
                .SetExtension("variable", variableDefinition.Variable.Name.Value)
                .AddLocation(variableDefinition)
                .Build());
    }

    public static GraphQLException VariableIsNotAnInputType(
        VariableDefinitionNode variableDefinition)
    {
        return new(
            ErrorBuilder.New()
                .SetMessage(
                    "Variable `{0}` is not an input type.",
                    variableDefinition.Variable.Name.Value)
                .SetCode(ErrorCodes.Execution.MustBeInputType)
                .SetExtension("variable", variableDefinition.Variable.Name.Value)
                .SetExtension("type", variableDefinition.Type.ToString())
                .AddLocation(variableDefinition)
                .Build());
    }

    public static GraphQLException FieldDoesNotExistOnType(
        FieldNode fieldNode,
        string typeName)
    {
        return new(
            ErrorBuilder.New()
                .SetMessage(
                    FusionExecutionResources.DocumentRewriter_FieldDoesNotExistOnType,
                    fieldNode.Name.Value,
                    typeName)
                .SetCode(ErrorCodes.Validation.FieldDoesNotExist)
                .SetExtension("type", typeName)
                .SetExtension("field", fieldNode.Name.Value)
                .AddLocation(fieldNode)
                .Build());
    }

    public static GraphQLException InvalidTypeConditionOnInlineFragment(
        InlineFragmentNode inlineFragment,
        string parentTypeName)
    {
        var typeName = inlineFragment.TypeCondition!.Name.Value;

        return new(
            ErrorBuilder.New()
                .SetMessage(
                    FusionExecutionResources.DocumentRewriter_InvalidTypeConditionOnInlineFragment,
                    parentTypeName,
                    typeName)
                .SetCode(ErrorCodes.Validation.FragmentTypeConditionUnknown)
                .SetExtension("typeCondition", typeName)
                .AddLocation(inlineFragment)
                .Build());
    }

    public static GraphQLException InvalidTypeConditionOnFragment(
        FragmentSpreadNode fragmentSpread,
        string typeName)
    {
        return new(
            ErrorBuilder.New()
                .SetMessage(
                    FusionExecutionResources.DocumentRewriter_InvalidTypeConditionOnFragment,
                    fragmentSpread.Name.Value,
                    typeName)
                .SetCode(ErrorCodes.Validation.FragmentTypeConditionUnknown)
                .SetExtension("fragment", fragmentSpread.Name.Value)
                .SetExtension("typeCondition", typeName)
                .AddLocation(fragmentSpread)
                .Build());
    }

    public static GraphQLException FragmentDoesNotExist(
        FragmentSpreadNode fragmentSpread)
    {
        return new(
            ErrorBuilder.New()
                .SetMessage(
                    FusionExecutionResources.DocumentRewriter_FragmentDoesNotExist,
                    fragmentSpread.Name.Value)
                .SetCode(ErrorCodes.Validation.FragmentDoesNotExist)
                .SetExtension("fragment", fragmentSpread.Name.Value)
                .AddLocation(fragmentSpread)
                .Build());
    }
}
