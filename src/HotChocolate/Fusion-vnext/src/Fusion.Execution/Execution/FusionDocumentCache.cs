using System.Diagnostics.CodeAnalysis;
using HotChocolate.Caching.Memory;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal sealed class DefaultDocumentCache(int capacity = 256) : IDocumentCache
{
    private readonly Cache<CachedDocument> _cache = new(capacity);

    public int Capacity => _cache.Capacity;

    public int Count => _cache.Count;

    public void TryAddDocument(string documentId, CachedDocument document)
        => _cache.GetOrCreate(documentId, static (_, d) => d, document);

    public bool TryGetDocument(string documentId, [NotNullWhen(true)] out CachedDocument? document)
        => _cache.TryGet(documentId, out document);
}