using System.Collections.Generic;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public interface IQueryableFilterVisitorContext
    {
        IReadOnlyList<IExpressionOperationHandler> OperationHandlers { get; }

        IReadOnlyList<IExpressionFieldHandler> FieldHandlers { get; }

        ITypeConverter TypeConverter { get; }

        bool InMemory { get; }

        Stack<QueryableClosure> Closures { get; }
    }
}
