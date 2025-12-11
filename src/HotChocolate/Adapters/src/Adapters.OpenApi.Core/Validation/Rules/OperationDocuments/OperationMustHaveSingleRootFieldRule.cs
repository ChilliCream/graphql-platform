using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that operation documents can only define a single root field selection.
/// </summary>
internal sealed class OperationMustHaveSingleRootFieldRule : IOpenApiOperationDocumentValidationRule
{
    public ValueTask<OpenApiDocumentValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiDocumentValidationContext context,
        CancellationToken cancellationToken)
    {
        var selectionSet = document.OperationDefinition.SelectionSet;
        OpenApiDocumentValidationResult result;

        if (selectionSet.Selections.Count != 1)
        {
            result = OpenApiDocumentValidationResult.Failure(
                new OpenApiDocumentValidationError(
                    $"Operation '{document.Name}' must have exactly one root field selection, but found {selectionSet.Selections.Count}.",
                    document));
        }
        else if (selectionSet.Selections[0] is not FieldNode)
        {
            result = OpenApiDocumentValidationResult.Failure(
                new OpenApiDocumentValidationError(
                    $"Operation '{document.Name}' must have a single root field selection, but found a fragment spread or inline fragment.",
                    document));
        }
        else
        {
            result = OpenApiDocumentValidationResult.Success();
        }

        return ValueTask.FromResult(result);
    }
}
