using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class CachedQuery
    {
        public string Query { get; }

        public DocumentNode Document { get; }

        public IReadOnlyList<FieldSelection> GetOrCollectFields(
            SelectionSetNode selectionSet,
            ObjectType type,
            Func<IReadOnlyList<FieldSelection>> collectFields)
        {
            ConcurrentDictionary<string, string> s;
            throw new NotImplementedException();
        }

        public FieldDelegate GetOrCreateMiddleware(
            FieldSelection fieldSelection,
            Func<FieldDelegate> createMiddleware)
        {
            throw new NotImplementedException();
        }
    }
}
