using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// Represents a parsing error that occurred while parsing an OpenAPI document.
/// </summary>
public sealed class OpenApiDocumentParsingError : IOpenApiError
{
    /// <summary>
    /// Initializes a new instance of <see cref="OpenApiDocumentParsingError" />.
    /// </summary>
    public OpenApiDocumentParsingError(string message, DocumentNode document)
    {
        Message = message;
        Document = document;
    }

    /// <inheritdoc />
    public string Message { get; }

    /// <summary>
    /// Gets the document node where the error occurred.
    /// </summary>
    public DocumentNode Document { get; }

    public override string ToString()
    {
        return Message;
    }
}
