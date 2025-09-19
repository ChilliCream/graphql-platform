namespace StrawberryShake;

/// <summary>
/// Represents a GraphQL query document.
/// </summary>
public interface IDocument
{
    /// <summary>
    /// Defines operation kind.
    /// </summary>
    OperationKind Kind { get; }

    /// <summary>
    /// Gets the GraphQL document body.
    /// </summary>
    ReadOnlySpan<byte> Body { get; }

    /// <summary>
    /// Gets the document hash.
    /// </summary>
    DocumentHash Hash { get; }
}
