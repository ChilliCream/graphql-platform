using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorContext
        : FilterVisitorContextBase, IQueryableFilterVisitorContext
    {
        public QueryableFilterVisitorContext(
            InputObjectType initialType,
            Type source,
            FilterExpressionVisitorDefintion defintion,
            ITypeConversion typeConverter,
            bool inMemory)
            : base(initialType)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            Defintion = defintion ??
                throw new ArgumentNullException(nameof(defintion));
            TypeConverter = typeConverter ??
                throw new ArgumentNullException(nameof(typeConverter));
            InMemory = inMemory;
            Closures = new Stack<QueryableClosure>();
            Closures.Push(new QueryableClosure(source, "r", inMemory));
        }

        protected FilterExpressionVisitorDefintion Defintion { get; }

        public ITypeConversion TypeConverter { get; }

        public bool InMemory { get; }

        public Stack<QueryableClosure> Closures { get; }

        public bool TryGetEnterHandler(
            FilterKind kind,
            [NotNullWhen(true)] out FilterFieldEnter? enter)
        {
            if (Defintion.FieldHandler.TryGetValue(
                kind, out (FilterFieldEnter? enter, FilterFieldLeave? leave) val) &&
                val.enter is FilterFieldEnter)
            {
                enter = val.enter;
                return true;
            }
            enter = null;
            return false;
        }

        public bool TryGetLeaveHandler(
            FilterKind kind,
            [NotNullWhen(true)] out FilterFieldLeave? leave)
        {
            if (Defintion.FieldHandler.TryGetValue(
                kind, out (FilterFieldEnter? enter, FilterFieldLeave? leave) val) &&
                val.leave is FilterFieldLeave)
            {
                leave = val.leave;
                return true;
            }
            leave = null;
            return false;
        }

        public bool TryGetOperation(
            FilterKind kind,
            FilterOperationKind operationKind,
            [NotNullWhen(true)] out FilterOperationHandler? handler)
        {
            if (Defintion.OperationHandler.TryGetValue(
                kind,
                out IReadOnlyDictionary<FilterOperationKind, FilterOperationHandler>? operations) &&
                operations.TryGetValue(operationKind, out FilterOperationHandler? operationHandler)
            )
            {
                handler = operationHandler;
                return true;
            }
            handler = null;
            return false;
        }
    }
}
