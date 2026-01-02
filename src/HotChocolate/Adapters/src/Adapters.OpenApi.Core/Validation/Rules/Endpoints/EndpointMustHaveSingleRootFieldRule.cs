using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that an endpoint definition can only define a single root field selection.
/// </summary>
internal sealed class EndpointMustHaveSingleRootFieldRule : IOpenApiEndpointDefinitionValidationRule
{
    public OpenApiDefinitionValidationResult Validate(OpenApiEndpointDefinition endpoint)
    {
        var selectionSet = endpoint.OperationDefinition.SelectionSet;

        if (selectionSet.Selections.Count != 1)
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    $"Endpoint '{endpoint.OperationDefinition.Name!.Value}' must have exactly one root field selection, but found {selectionSet.Selections.Count}."));
        }

        if (selectionSet.Selections[0] is not FieldNode)
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    $"Endpoint '{endpoint.OperationDefinition.Name!.Value}' must have a single root field selection, but found a fragment spread or inline fragment."));
        }

        return OpenApiDefinitionValidationResult.Success();
    }
}
