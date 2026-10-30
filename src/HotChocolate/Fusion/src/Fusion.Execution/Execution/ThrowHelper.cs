using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Properties;
using HotChocolate.Language;
using HotChocolate.Types;

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

    public static InvalidOperationException IncrementalPlanParentNotFound(SelectionPath path)
        => new(string.Format(
            FusionExecutionResources.OperationPlan_IncrementalPlanParentNotFound,
            path));

    public static InvalidOperationException DeferredPlanNotFound(SelectionPath path)
        => new($"No plan was found for the @defer fragment at path '{path}'.");

    public static InvalidOperationException LookupTargetPathsReleased()
        => new("The paths of lookup targets can only be created while their result document is in use.");

    public static OperationPlannerGuardrailException PlanningGuardrailExceeded(
        string operationId,
        OperationPlannerGuardrailReason reason,
        long limit,
        long observed)
        => new(operationId, reason, limit, observed);

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

    public static InvalidOperationException InvalidVariableIndex(int variableIndex, int nodeId)
        => new(string.Format(
            FusionExecutionResources.HttpSourceSchemaClient_InvalidVariableIndex,
            variableIndex,
            nodeId));

    public static InvalidOperationException MissingBatchRequestIndex()
        => new("Expected requestIndex in batch response but it was missing.");

    public static InvalidOperationException FlattenedRequestIndexOutOfRange(int flattenedIndex)
        => new($"Flattened index {flattenedIndex} is out of range.");

    public static ArgumentException InvalidClientConfiguration(Type expected, Type actual)
        => new($"Expected client configuration of type '{expected.Name}' but received '{actual.Name}'.");

    public static InvalidOperationException ClientConfigurationNotFound(string schemaName, OperationType operationType)
        => new($"No client configuration found for schema '{schemaName}' and operation type {operationType}.");

    public static InvalidOperationException CostOptionsAreReadOnly()
        => new("The cost options are read-only.");

    public static InvalidOperationException OperationDocumentNotAvailable()
        => new("The operation document is not available in the context.");

    public static KeyNotFoundException DeliveryGroupNotFound(int deliveryGroupId)
        => new($"The delivery group with the ID '{deliveryGroupId}' was not found.");

    public static InvalidOperationException DeliveryPathSegmentNotFound(string segmentName)
        => new($"Could not resolve selection path segment '{segmentName}'.");

    public static InvalidOperationException DeliveryPathSelectionSetMissing(string segmentName)
        => new($"Selection path segment '{segmentName}' has no selection set.");

    public static InvalidOperationException OperationPlanTaskCompletedWithoutResult()
        => new("The operation plan task completed without a result.");

    public static InvalidOperationException ApolloLookupOperationMissing(int stepId)
        => new($"The original Apollo lookup operation for execution step {stepId} is missing.");

    public static InvalidOperationException ApolloEntityFetchProducedMultipleResults()
        => new("Apollo entity fetches must produce a single source schema result.");

    public static InvalidOperationException UnknownOperationDefinition(Type definitionType)
        => new($"Unknown operation definition type: {definitionType.Name}");

    public static NotSupportedException ValueCompletionTypeNotSupported(IType type)
        => new($"The type {type} is not supported.");

    public static NotSupportedException ValueCompletionDepthNotAllowed(int depth)
        => new($"The depth {depth} is not allowed.");

    public static ArgumentOutOfRangeException InvalidCostOptionValue(
        string optionName,
        double value)
        => new(
            optionName,
            value,
            "The value must be a non-negative finite number or positive infinity.");

    public static InvalidOperationException ImportedPathSelectionNotFound(string responseName)
        => new($"The field `{responseName}` of the enclosing result path is not selected by the incremental plan.");

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

    public static GraphQLException DeferIfVariableIsNull(string variableName)
        => new(ErrorBuilder.New()
            .SetMessage(
                "The variable `{0}` is null, but the `if` argument of the `@defer` directive must not be null.",
                variableName)
            .SetCode(ErrorCodes.Execution.NonNullViolation)
            .SetExtension("variable", variableName)
            .SetExtension("directive", DirectiveNames.Defer.Name)
            .Build());

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

    public static GraphQLException MutationFieldHasTooManyConditionVariants(
        FieldNode fieldNode,
        int maxConditions)
    {
        var responseName = fieldNode.Alias?.Value ?? fieldNode.Name.Value;

        return new(
            ErrorBuilder.New()
                .SetMessage(
                    FusionExecutionResources.DocumentRewriter_MutationFieldHasTooManyConditionVariants,
                    responseName,
                    maxConditions)
                .SetExtension("responseName", responseName)
                .AddLocation(fieldNode)
                .Build());
    }

    public static GraphQLException OperationCompiler_TooManySelections()
        => new(
            ErrorBuilder.New()
                .SetMessage("The operation exceeds the maximum allowed number of selections and selection sets.")
                .Build());
}
