namespace HotChocolate.Exporters.OpenApi;

public sealed class OpenApiValidationError( string message, IOpenApiDocument document)
{
    /// <summary>
    /// Gets the document.
    /// </summary>
    public IOpenApiDocument Document { get; } = document;

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; } = message;

    public override string ToString()
    {
        return Message;
    }
}
