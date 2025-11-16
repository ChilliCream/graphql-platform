namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// Represents an error that occurred during OpenAPI document processing (parsing or validation).
/// </summary>
public interface IOpenApiError
{
    /// <summary>
    /// Gets the error message.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets the document ID where the error occurred.
    /// </summary>
    string DocumentId { get; }
}
