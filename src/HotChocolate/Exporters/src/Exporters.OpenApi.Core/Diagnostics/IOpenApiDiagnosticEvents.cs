namespace HotChocolate.Exporters.OpenApi;

/// <summary>
/// Provides diagnostic events for the OpenAPI integration.
/// </summary>
public interface IOpenApiDiagnosticEvents
{
    /// <summary>
    /// Called when errors occur while parsing or validating an OpenAPI document.
    /// </summary>
    /// <param name="errors">The errors.</param>
    void ValidationErrors(IReadOnlyList<IOpenApiError> errors);
}
