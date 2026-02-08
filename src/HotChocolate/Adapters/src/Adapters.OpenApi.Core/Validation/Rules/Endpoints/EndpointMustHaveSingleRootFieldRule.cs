using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that an endpoint definition can only define a single root field selection.
/// </summary>
internal sealed class EndpointMustHaveSingleRootFieldRule : IOpenApiEndpointDefinitionValidationRule
{
    public OpenApiDefinitionValidationResult Validate(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context)
    {
        var selectionSet = endpoint.OperationDefinition.SelectionSet;

        if (selectionSet.Selections is not [FieldNode])
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    "Endpoint must select exactly one root field.",
                    endpoint));
        }

        return OpenApiDefinitionValidationResult.Success();
    }
}
