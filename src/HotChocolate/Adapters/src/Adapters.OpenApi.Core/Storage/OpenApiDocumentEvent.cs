namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// Represents an event that occurs when OpenAPI documents are updated.
/// </summary>
public sealed record OpenApiDocumentEvent(
    OpenApiDocumentEventType Type)
{
    /// <summary>
    /// Creates an event indicating that OpenAPI documents have been updated.
    /// </summary>
    public static OpenApiDocumentEvent Updated()
        => new(OpenApiDocumentEventType.Updated);
}
