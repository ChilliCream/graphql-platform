using System.Collections.Generic;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters
{
    public interface IFilterVisitorContext<T>
        : IFilterVisitorContextBase
    {
        Stack<FilterScope<T>> Scopes { get; }

        FilterScope<T> CreateScope();
    }
}