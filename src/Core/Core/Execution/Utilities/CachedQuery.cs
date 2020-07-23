using System;
using System.Collections.Concurrent;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Execution
{

    internal sealed partial class CachedQuery
        : ICachedQuery
    {
        private ConcurrentDictionary<string, IPreparedOperation> _operations =
            new ConcurrentDictionary<string, IPreparedOperation>();

        public CachedQuery(string queryKey, DocumentNode document)
        {
            if (string.IsNullOrEmpty(queryKey))
            {
                throw new ArgumentException(
                    CoreResources.CachedQuery_Key_Is_Null,
                    nameof(queryKey));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            QueryKey = queryKey;
            Document = document;
        }

        public string QueryKey { get; }

        public DocumentNode Document { get; }

        public IPreparedOperation GetOrCreate(
            string operationId, 
            Func<IPreparedOperation> create) =>
            _operations.GetOrAdd(operationId, s => create());
    }
}
