using System.Collections.Generic;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public interface IQueryableFilterVisitorContext
    {
        ITypeConversion TypeConverter { get; }

        bool InMemory { get; }

        Stack<QueryableClosure> Closures { get; }
    }
}
