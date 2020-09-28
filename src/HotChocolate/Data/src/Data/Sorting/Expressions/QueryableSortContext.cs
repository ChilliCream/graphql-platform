using System.Collections.Generic;
using HotChocolate.Internal;

namespace HotChocolate.Data.Sorting.Expressions
{
    public class QueryableSortContext : SortVisitorContext<QueryableSortOperation>
    {
        public QueryableSortContext(
            ISortInputType initialType,
            bool inMemory)
            : base(initialType)
        {
            InMemory = inMemory;
            RuntimeTypes = new Stack<IExtendedType>();
            RuntimeTypes.Push(initialType.EntityType);
            Instance.Push(QueryableFieldSelector.New(initialType.EntityType.Source));
        }

        public bool InMemory { get; }

        public Stack<IExtendedType> RuntimeTypes { get; }
    }
}
