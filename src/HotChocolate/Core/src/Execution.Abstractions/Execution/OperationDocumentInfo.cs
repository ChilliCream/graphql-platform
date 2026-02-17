using HotChocolate.Language;

namespace HotChocolate.Execution;

/// <summary>
/// Provides information about the GraphQL operation document.
/// </summary>
public sealed class OperationDocumentInfo : RequestFeature
{
    /// <summary>
    /// Gets or sets the parsed query document.
    /// </summary>
    public DocumentNode? Document { get; set; }

    /// <summary>
    /// Gets or sets a unique identifier for an operation document.
    /// </summary>
    public OperationDocumentId Id { get; set; }

    /// <summary>
    /// Gets or sets the document hash.
    /// </summary>
    public OperationDocumentHash Hash { get; set; }

    /// <summary>
    /// Gets the number of operation definitions in the document.
    /// </summary>
    public int OperationCount => Document?.Definitions.Count(d => d.Kind == SyntaxKind.OperationDefinition) ?? 0;

    /// <summary>
    /// Defines that the document was retrieved from the cache.
    /// </summary>
    public bool IsCached { get; set; }

    /// <summary>
    /// Defines that the document was retrieved from a query storage.
    /// </summary>
    public bool IsPersisted { get; set; }

    /// <summary>
    /// Defines that the document has been validated.
    /// </summary>
    public bool IsValidated { get; set; }

    /// <inheritdoc />
    protected internal override void Reset()
    {
        Document = null!;
        Id = default;
        Hash = default;
        IsCached = false;
        IsPersisted = false;
        IsValidated = false;
    }
}
