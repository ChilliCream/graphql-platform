using HotChocolate.Exporters.OpenApi.Validation;

namespace HotChocolate.Exporters.OpenApi;

/// <summary>
/// Provides diagnostic events for the OpenAPI integration.
/// </summary>
public interface IOpenApiDiagnosticEvents
{
    /// <summary>
    /// Called when errors occur while validating an open api document.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    void ValidationErrors(IReadOnlyList<OpenApiValidationError> errors);
}
