using System;
using System.Collections.Generic;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorContext
        : IQueryableFilterVisitorContext
    {
        public QueryableFilterVisitorContext(
            IReadOnlyList<IExpressionOperationHandler> operationHandlers,
            IReadOnlyList<IExpressionFieldHandler> fieldHandlers,
            ITypeConversion typeConverter,
            QueryableClosure closures,
            bool inMemory)
        {
            if (closures is null)
            {
                throw new ArgumentNullException(nameof(closures));
            }
            OperationHandlers = operationHandlers ??
                throw new ArgumentNullException(nameof(operationHandlers));
            FieldHandlers = fieldHandlers ??
                throw new ArgumentNullException(nameof(fieldHandlers));
            TypeConverter = typeConverter ??
                throw new ArgumentNullException(nameof(typeConverter));
            InMemory = inMemory;
            Closures = new Stack<QueryableClosure>();
            Closures.Push(closures);
        }

        public IReadOnlyList<IExpressionOperationHandler> OperationHandlers { get; }

        public IReadOnlyList<IExpressionFieldHandler> FieldHandlers { get; }

        public ITypeConversion TypeConverter { get; }

        public bool InMemory { get; }

        public Stack<QueryableClosure> Closures { get; }
    }
}
