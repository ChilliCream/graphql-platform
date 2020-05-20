using System.Collections.Generic;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public interface IFilterVisitorContext<T>
        : IFilterVisitorContextBase
    {
        ITypeConversion TypeConverter { get; }

        Stack<FilterScope<T>> Scopes { get; }

        FilterScope<T> CreateScope();
    }
}
