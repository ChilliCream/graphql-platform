using System.Diagnostics.CodeAnalysis;
using HotChocolate.Validation;

namespace HotChocolate.Exporters.OpenApi.Validation;

/// <summary>
/// Provides context for OpenAPI document validation.
/// </summary>
public interface IOpenApiValidationContext
{
    ISchemaDefinition Schema { get; }

    DocumentValidator DocumentValidator { get; }

    ValueTask<OpenApiFragmentDocument?> GetFragmentAsync(string fragmentName);

    ValueTask<OpenApiOperationDocument?> GetOperationAsync(string operationName);
}
