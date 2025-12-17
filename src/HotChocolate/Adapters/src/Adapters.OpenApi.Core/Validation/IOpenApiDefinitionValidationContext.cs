using HotChocolate.Validation;

namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// Provides context for OpenAPI definition validation.
/// </summary>
public interface IOpenApiDefinitionValidationContext
{
    ISchemaDefinition Schema { get; }

    DocumentValidator DocumentValidator { get; }

    ValueTask<OpenApiModelDefinition?> GetModelAsync(string modelName);

    ValueTask<OpenApiEndpointDefinition?> GetEndpointAsync(string endpointName);

    ValueTask<OpenApiEndpointDefinition?> GetEndpointByRouteAndMethodAsync(string routePattern, string httpMethod);
}
