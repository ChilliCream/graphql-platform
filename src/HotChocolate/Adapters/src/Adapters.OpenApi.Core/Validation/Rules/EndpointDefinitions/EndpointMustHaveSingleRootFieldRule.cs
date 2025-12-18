using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that endpoint definitions can only define a single root field selection.
/// </summary>
internal sealed class EndpointMustHaveSingleRootFieldRule : IOpenApiEndpointDefinitionValidationRule
{
    public ValueTask<OpenApiDefinitionValidationResult> ValidateAsync(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context,
        CancellationToken cancellationToken)
    {
        var selectionSet = endpoint.OperationDefinition.SelectionSet;
        OpenApiDefinitionValidationResult result;

        if (selectionSet.Selections.Count != 1)
        {
            result = OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    $"Endpoint '{endpoint.OperationDefinition.Name!.Value}' must have exactly one root field selection, but found {selectionSet.Selections.Count}.",
                    endpoint));
        }
        else if (selectionSet.Selections[0] is not FieldNode)
        {
            result = OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    $"Endpoint '{endpoint.OperationDefinition.Name!.Value}' must have a single root field selection, but found a fragment spread or inline fragment.",
                    endpoint));
        }
        else
        {
            result = OpenApiDefinitionValidationResult.Success();
        }

        return ValueTask.FromResult(result);
    }
}
