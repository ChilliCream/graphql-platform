namespace HotChocolate.Adapters.OpenApi;

internal interface IOpenApiModelDefinitionValidationRule
{
    ValueTask<OpenApiDefinitionValidationResult> ValidateAsync(
        OpenApiModelDefinition model,
        IOpenApiDefinitionValidationContext context,
        CancellationToken cancellationToken);
}
