namespace HotChocolate.Adapters.OpenApi;

internal interface IOpenApiModelDefinitionValidationRule
{
    OpenApiDefinitionValidationResult Validate(OpenApiModelDefinition model);
}
