using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Language;

/// <summary>
/// The document cache is used to cache parsed GraphQL syntax trees.
/// </summary>
public interface IDocumentCache
{
    /// <summary>
    /// Gets the maximum number of GraphQL syntax trees that can be cached.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    /// Gets the number of GraphQL syntax trees residing in the cache.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Tries to get a cached GraphQL syntax tree by its <paramref name="documentId"/>.
    /// </summary>
    /// <param name="documentId">
    /// The document id.
    /// </param>
    /// <param name="document">
    /// The GraphQL syntax tree.
    /// </param>
    /// <returns>
    /// <c>true</c> if a cached GraphQL syntax tree was found that matches the
    /// <paramref name="documentId"/>, otherwise <c>false</c>.
    /// </returns>
    bool TryGetDocument(string documentId, [NotNullWhen(true)] out CachedDocument? document);

    /// <summary>
    /// Tries to add a parsed GraphQL syntax tree to the cache.
    /// </summary>
    /// <param name="documentId">
    /// The internal document id.
    /// </param>
    /// <param name="document">
    /// The GraphQL syntax tree.
    /// </param>
    void TryAddDocument(string documentId, CachedDocument document);
}
