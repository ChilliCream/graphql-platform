namespace HotChocolate.Adapters.OpenApi;

internal interface IOpenApiEndpointDefinitionValidationRule
{
    ValueTask<OpenApiDefinitionValidationResult> ValidateAsync(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context,
        CancellationToken cancellationToken);
}
