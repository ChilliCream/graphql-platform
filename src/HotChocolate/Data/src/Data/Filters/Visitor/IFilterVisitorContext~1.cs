using System.Collections.Generic;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters
{
    public interface IFilterVisitorContext<T>
        : IFilterVisitorContext
    {
        Stack<FilterScope<T>> Scopes { get; }

        FilterScope<T> CreateScope();
    }
}