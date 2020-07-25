using System.Collections.Generic;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters
{
    public interface IFilterVisitorContext<T>
        : IFilterVisitorContextBase
    {
        ITypeConverter TypeConverter { get; }

        Stack<FilterScope<T>> Scopes { get; }

        FilterScope<T> CreateScope();
    }
}