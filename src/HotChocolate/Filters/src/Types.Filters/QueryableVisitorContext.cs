using System;
using System.Collections.Generic;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public class QueryableFilterVisitorContext
        : FilterVisitorContextBase, IQueryableFilterVisitorContext
    {

        public QueryableFilterVisitorContext(
            InputObjectType initialType,
            Type source,
            ITypeConverter converter,
            bool inMemory)
            : this(
                initialType,
                source,
                ExpressionOperationHandlers.All,
                ExpressionFieldHandlers.All,
                converter,
                inMemory)
        {
        }

        public QueryableFilterVisitorContext(
            InputObjectType initialType,
            Type source,
            IReadOnlyList<IExpressionOperationHandler> operationHandlers,
            IReadOnlyList<IExpressionFieldHandler> fieldHandlers,
            ITypeConverter typeConverter,
            bool inMemory)
            : base(initialType)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            OperationHandlers = operationHandlers ??
                throw new ArgumentNullException(nameof(operationHandlers));
            FieldHandlers = fieldHandlers ??
                throw new ArgumentNullException(nameof(fieldHandlers));
            TypeConverter = typeConverter ??
                throw new ArgumentNullException(nameof(typeConverter));
            InMemory = inMemory;
            Closures = new Stack<QueryableClosure>();
            Closures.Push(new QueryableClosure(source, "r", inMemory));
        }

        public IReadOnlyList<IExpressionOperationHandler> OperationHandlers { get; }

        public IReadOnlyList<IExpressionFieldHandler> FieldHandlers { get; }

        public ITypeConverter TypeConverter { get; }

        public bool InMemory { get; }

        public Stack<QueryableClosure> Closures { get; }
    }
}
