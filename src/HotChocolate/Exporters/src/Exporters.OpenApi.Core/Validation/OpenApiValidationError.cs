namespace HotChocolate.Exporters.OpenApi;

/// <summary>
/// Represents a validation error that occurred while validating an OpenAPI document.
/// </summary>
public sealed class OpenApiValidationError : IOpenApiError
{
    /// <summary>
    /// Initializes a new instance of <see cref="OpenApiValidationError" />.
    /// </summary>
    public OpenApiValidationError(string message, IOpenApiDocument document)
    {
        Message = message;
        DocumentId = document.Id;
        Document = document;
    }

    /// <inheritdoc />
    public string Message { get; }

    /// <inheritdoc />
    public string DocumentId { get; }

    /// <summary>
    /// Gets the document where the error occurred.
    /// </summary>
    public IOpenApiDocument Document { get; }

    public override string ToString()
    {
        return Message;
    }
}
