using System;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    public class DefaultDocumentCache
        : IDocumentCache
    {
        private readonly Cache<ICachedQuery> _cache;

        public DefaultDocumentCache(Cache<ICachedQuery> cache)
        {
            _cache = cache
                ?? throw new ArgumentNullException(nameof(cache));
        }

        public bool TryGetDocument(string key, out DocumentNode document)
        {
            if (_cache.TryGet(key, out ICachedQuery query))
            {
                document = query.Document;
                return true;
            }

            document = null;
            return false;
        }
    }
}
