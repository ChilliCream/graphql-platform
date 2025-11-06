namespace HotChocolate.Exporters.OpenApi.Validation;

/// <summary>
/// Represents a validation error.
/// </summary>
public sealed class OpenApiValidationError
{
    /// <summary>
    /// Initializes a new instance of <see cref="OpenApiValidationError" />.
    /// </summary>
    public OpenApiValidationError(string message, string? documentId = null, string? documentName = null)
    {
        Message = message;
        DocumentId = documentId;
        DocumentName = documentName;
    }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the document ID where the error occurred, if applicable.
    /// </summary>
    public string? DocumentId { get; }

    /// <summary>
    /// Gets the document name where the error occurred, if applicable.
    /// </summary>
    public string? DocumentName { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        if (DocumentName is not null)
        {
            return $"[{DocumentName}] {Message}";
        }

        if (DocumentId is not null)
        {
            return $"[{DocumentId}] {Message}";
        }

        return Message;
    }
}
