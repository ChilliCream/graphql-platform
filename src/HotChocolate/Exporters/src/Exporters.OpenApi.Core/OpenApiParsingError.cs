using HotChocolate.Language;

namespace HotChocolate.Exporters.OpenApi;

/// <summary>
/// Represents a parsing error that occurred while parsing an OpenAPI document.
/// </summary>
public sealed class OpenApiParsingError : IOpenApiError
{
    /// <summary>
    /// Initializes a new instance of <see cref="OpenApiParsingError" />.
    /// </summary>
    public OpenApiParsingError(string message, string documentId, DocumentNode document)
    {
        Message = message;
        DocumentId = documentId;
        Document = document;
    }

    /// <inheritdoc />
    public string Message { get; }

    /// <inheritdoc />
    public string DocumentId { get; }

    /// <summary>
    /// Gets the document node where the error occurred.
    /// </summary>
    public DocumentNode Document { get; }

    public override string ToString()
    {
        return Message;
    }
}
