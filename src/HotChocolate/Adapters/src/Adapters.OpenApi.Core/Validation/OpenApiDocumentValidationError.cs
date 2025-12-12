namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// Represents a validation error that occurred while validating an OpenAPI document.
/// </summary>
public sealed class OpenApiDocumentValidationError : IOpenApiError
{
    /// <summary>
    /// Initializes a new instance of <see cref="OpenApiDocumentValidationError" />.
    /// </summary>
    public OpenApiDocumentValidationError(string message, IOpenApiDocument document)
    {
        Message = message;
        DocumentId = document.Id;
        Document = document;
    }

    /// <inheritdoc />
    public string Message { get; }

    /// <summary>
    /// Gets the document ID where the error occurred.
    /// </summary>
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
