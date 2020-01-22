using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Sorting
{
    public class QueryableSortVisitorContext
        : IQueryableSortVisitorContext
    {
        public QueryableSortVisitorContext(
            bool inMemory,
            SortQueryableClosure closure)
        {
            if (closure is null)
            {
                throw new ArgumentNullException(nameof(closure));
            }
            Closure = closure;
            InMemory = inMemory;
        }
        public bool InMemory { get; }
        public Queue<SortOperationInvocation> SortOperations { get; } =
            new Queue<SortOperationInvocation>();
        public SortQueryableClosure Closure { get; }

    }
}
