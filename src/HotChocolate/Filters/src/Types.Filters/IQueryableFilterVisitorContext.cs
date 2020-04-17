using System.Collections.Generic;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public interface IQueryableFilterVisitorContext
        : IFilterVisitorContextBase
    {
        ITypeConversion TypeConverter { get; }

        bool InMemory { get; }

        Stack<QueryableClosure> Closures { get; }
    }
}
