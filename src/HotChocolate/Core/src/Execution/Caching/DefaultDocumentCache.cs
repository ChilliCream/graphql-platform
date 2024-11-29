using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Caching;

internal sealed class DefaultDocumentCache(int capacity = 100) : IDocumentCache
{
    private readonly Cache<CachedDocument> _cache = new(capacity);

    public int Capacity => _cache.Capacity;

    public int Count => _cache.Usage;

    public void TryAddDocument(string documentId, CachedDocument document)
        => _cache.GetOrCreate(documentId, () => document);

    public bool TryGetDocument(string documentId, [NotNullWhen(true)] out CachedDocument? document)
        => _cache.TryGet(documentId, out document!);

    public void Clear() => _cache.Clear();
}
