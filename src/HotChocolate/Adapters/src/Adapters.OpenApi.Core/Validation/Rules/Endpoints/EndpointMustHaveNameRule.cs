namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that an endpoint definition has a non-null name.
/// </summary>
internal sealed class EndpointMustHaveNameRule : IOpenApiEndpointDefinitionValidationRule
{
    public OpenApiDefinitionValidationResult Validate(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context)
    {
        if (string.IsNullOrEmpty(endpoint.OperationDefinition.Name?.Value))
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    "Endpoint is missing a named GraphQL operation. Anonymous operations are not supported.",
                    endpoint));
        }

        return OpenApiDefinitionValidationResult.Success();
    }
}
