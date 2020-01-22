using System.Collections.Generic;

namespace HotChocolate.Types.Sorting
{
    public interface IQueryableSortVisitorContext
    {
        bool InMemory { get; }
        public Queue<SortOperationInvocation> SortOperations { get; }
        public SortQueryableClosure Closure { get; }

    }
}
