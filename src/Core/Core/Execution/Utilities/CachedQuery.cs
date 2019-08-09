using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;

namespace HotChocolate.Execution
{

    internal sealed partial class CachedQuery
        : ICachedQuery
    {
        private ConcurrentDictionary<Key, IReadOnlyList<FieldSelection>> _flds =
            new ConcurrentDictionary<Key, IReadOnlyList<FieldSelection>>();

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

        public IReadOnlyList<FieldSelection> GetOrCollectFields(
            ObjectType type,
            SelectionSetNode selectionSet,
            Func<IReadOnlyList<FieldSelection>> collectFields)
        {
            var key = new Key(selectionSet, type);
            if (!_flds.TryGetValue(key, out IReadOnlyList<FieldSelection> flds))
            {
                flds = collectFields();
                _flds.TryAdd(key, flds);
            }
            return flds;
        }
    }
}
