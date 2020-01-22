using System.Collections.Generic;

namespace HotChocolate.Types.Sorting
{
    public interface IQueryableSortVisitorContext
    {
        bool InMemory { get; }
        Queue<SortOperationInvocation> SortOperations { get; }
        SortQueryableClosure Closure { get; }

    }
}
