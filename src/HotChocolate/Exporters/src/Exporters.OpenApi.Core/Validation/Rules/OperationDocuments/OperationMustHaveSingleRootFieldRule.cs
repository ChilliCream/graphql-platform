using HotChocolate.Language;

namespace HotChocolate.Exporters.OpenApi.Validation;

/// <summary>
/// Validates that operation documents can only define a single root field selection.
/// </summary>
internal sealed class OperationMustHaveSingleRootFieldRule : IOpenApiOperationDocumentValidationRule
{
    public ValueTask<OpenApiValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken)
    {
        var selectionSet = document.OperationDefinition.SelectionSet;
        OpenApiValidationResult result;

        if (selectionSet.Selections.Count != 1)
        {
            result = OpenApiValidationResult.Failure(
                new OpenApiValidationError(
                    $"Operation '{document.Name}' must have exactly one root field selection, but found {selectionSet.Selections.Count}.",
                    document));
        }
        else if (selectionSet.Selections[0] is not FieldNode)
        {
            result = OpenApiValidationResult.Failure(
                new OpenApiValidationError(
                    $"Operation '{document.Name}' must have a single root field selection, but found a fragment spread or inline fragment.",
                    document));
        }
        else
        {
            result = OpenApiValidationResult.Success();
        }

        return ValueTask.FromResult(result);
    }
}
