using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed partial class CachedQuery
    {
        private ConcurrentDictionary<Key, IReadOnlyList<FieldSelection>> _flds =
            new ConcurrentDictionary<Key, IReadOnlyList<FieldSelection>>();

        public CachedQuery(string query, DocumentNode document)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentException(
                    "The query mustn't be null or empty.",
                    nameof(query));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            Query = query;
            Document = document;
        }

        public string Query { get; }

        public DocumentNode Document { get; }

        public IReadOnlyList<FieldSelection> GetOrCollectFields(
            SelectionSetNode selectionSet,
            ObjectType type,
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
