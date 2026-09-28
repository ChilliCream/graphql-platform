using System.Diagnostics.CodeAnalysis;
using HotChocolate.Caching.Memory;
using HotChocolate.Language;

namespace HotChocolate.Execution.Caching;

/// <summary>
/// Caches normalized operation documents keyed by the operation id so that the
/// <see cref="Pipeline.OperationDocumentNormalizer"/> does not have to re-inline
/// fragments for an operation that has already been normalized.
/// </summary>
internal sealed class NormalizedDocumentCache(int capacity = 256)
{
    private readonly Cache<DocumentNode> _cache = new(capacity);

    /// <summary>
    /// Gets the maximum number of normalized documents that can be cached.
    /// </summary>
    public int Capacity => _cache.Capacity;

    /// <summary>
    /// Gets the number of normalized documents currently cached.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Tries to get a normalized document by its <paramref name="operationId"/>.
    /// </summary>
    public bool TryGet(string operationId, [NotNullWhen(true)] out DocumentNode? document)
        => _cache.TryGet(operationId, out document);

    /// <summary>
    /// Tries to add a normalized document to the cache.
    /// </summary>
    public void TryAdd(string operationId, DocumentNode document)
        => _cache.TryAdd(operationId, document);
}
