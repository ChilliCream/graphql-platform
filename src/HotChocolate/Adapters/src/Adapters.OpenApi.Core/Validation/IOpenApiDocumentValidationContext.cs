using HotChocolate.Validation;

namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// Provides context for OpenAPI document validation.
/// </summary>
public interface IOpenApiDocumentValidationContext
{
    ISchemaDefinition Schema { get; }

    DocumentValidator DocumentValidator { get; }

    ValueTask<OpenApiFragmentDocument?> GetFragmentAsync(string fragmentName);

    ValueTask<OpenApiOperationDocument?> GetOperationAsync(string operationName);

    ValueTask<OpenApiOperationDocument?> GetOperationByRouteAndMethodAsync(string routePattern, string httpMethod);
}
