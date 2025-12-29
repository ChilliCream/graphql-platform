namespace HotChocolate.Adapters.OpenApi;

internal interface IOpenApiEndpointDefinitionValidationRule
{
    OpenApiDefinitionValidationResult Validate(OpenApiEndpointDefinition endpoint);
}
