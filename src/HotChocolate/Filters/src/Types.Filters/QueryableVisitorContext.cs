using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorContext
        : FilterVisitorContext<Expression>
    {
        public QueryableFilterVisitorContext(
            IFilterInputType initialType,
            FilterVisitorDefinition<Expression> definition,
            ITypeConversion typeConverter,
            bool inMemory)
            : base(initialType, definition, typeConverter)
        {
            InMemory = inMemory;
            ClrTypes = new Stack<Type>();
            ClrTypes.Push(initialType.EntityType);
        }

        public bool InMemory { get; }

        public Stack<Type> ClrTypes { get; }

        public override FilterScope<Expression> CreateScope() =>
             new QueryableScope(ClrTypes.Peek(), "_s" + Scopes.Count, InMemory);
    }
}
