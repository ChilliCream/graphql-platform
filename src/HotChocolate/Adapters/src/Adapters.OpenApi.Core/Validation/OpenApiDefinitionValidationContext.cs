namespace HotChocolate.Adapters.OpenApi;

public sealed record OpenApiDefinitionValidationContext(ISchemaDefinition? Schema)
    : IOpenApiDefinitionValidationContext;
