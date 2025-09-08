namespace HotChocolate.Language;

/// <summary>
/// The document hash provider is used to compute the hash of a GraphQL operation document.
/// </summary>
public interface IDocumentHashProvider
{
    /// <summary>
    /// Gets the name of the document hash provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the format of the document hash.
    /// </summary>
    HashFormat Format { get; }

    /// <summary>
    /// Computes the hash of a GraphQL operation document.
    /// </summary>
    /// <param name="document">The GraphQL operation document.</param>
    /// <returns>The hash of the GraphQL operation document.</returns>
    OperationDocumentHash ComputeHash(ReadOnlySpan<byte> document);
}
