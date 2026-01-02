namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that an endpoint definition has a non-null name.
/// </summary>
internal sealed class EndpointMustHaveOperationNameRule : IOpenApiEndpointDefinitionValidationRule
{
    public OpenApiDefinitionValidationResult Validate(OpenApiEndpointDefinition endpoint)
    {
        if (string.IsNullOrEmpty(endpoint.OperationDefinition.Name?.Value))
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    "The endpoint must have an operation name."));
        }

        return OpenApiDefinitionValidationResult.Success();
    }
}
