namespace HotChocolate.Language;

/// <summary>
/// Represents a cached document.
/// </summary>
public sealed class CachedDocument(DocumentNode body, OperationDocumentHash hash, bool isPersisted)
{
    /// <summary>
    /// Gets the actual GraphQL syntax tree.
    /// </summary>
    public DocumentNode Body { get; } = body;

    /// <summary>
    /// Gets the hash of the document.
    /// </summary>
    public OperationDocumentHash Hash { get; } = hash;

    /// <summary>
    /// Defines if the document is a persisted document.
    /// </summary>
    public bool IsPersisted { get; } = isPersisted;
}
