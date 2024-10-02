namespace HotChocolate.Language;

/// <summary>
/// Represents a cached document.
/// </summary>
/// <param name="body"></param>
/// <param name="isPersisted"></param>
public sealed class CachedDocument(
    DocumentNode body,
    bool isPersisted)
{
    /// <summary>
    /// Gets the actual GraphQL syntax tree.
    /// </summary>
    public DocumentNode Body { get; } = body;

    /// <summary>
    /// Defines if the document is a persisted document.
    /// </summary>
    public bool IsPersisted { get; } = isPersisted;
}
