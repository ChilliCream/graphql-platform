using System.Collections.Generic;

namespace HotChocolate.Data.Filters
{
    public interface IFilterVisitorContext<T>
        : IFilterVisitorContext
    {
        Stack<FilterScope<T>> Scopes { get; }

        FilterScope<T> CreateScope();
    }
}
