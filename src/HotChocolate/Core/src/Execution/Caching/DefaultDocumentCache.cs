using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Caching
{
    internal sealed class DefaultDocumentCache : IDocumentCache
    {
        private readonly Cache<DocumentNode> _cache;

        public DefaultDocumentCache(int capacity = 100)
        {
            _cache = new Cache<DocumentNode>(capacity);
        }

        public int Capacity => _cache.Size;

        public int Count => _cache.Usage;

        public void TryAddDocument(
            string documentId,
            DocumentNode document) =>
            _cache.GetOrCreate(documentId, () => document);

        public bool TryGetDocument(
            string documentId,
            [NotNullWhen(true)] out DocumentNode document) =>
            _cache.TryGet(documentId, out document!);

        public void Clear() => _cache.Clear();
    }
}

