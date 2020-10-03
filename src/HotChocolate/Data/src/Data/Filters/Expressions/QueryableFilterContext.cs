using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Internal;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableFilterContext : FilterVisitorContext<Expression>
    {
        public QueryableFilterContext(
            IFilterInputType initialType,
            bool inMemory,
            Type? entityType = null)
            : base(initialType,
                  new QueryableScope(entityType ?? initialType.EntityType.Source, "_s0", inMemory))
        {
            InMemory = inMemory;
            RuntimeTypes = new Stack<IExtendedType>();
            RuntimeTypes.Push(initialType.EntityType);
        }

        public bool InMemory { get; }

        public Stack<IExtendedType> RuntimeTypes { get; }

        public override FilterScope<Expression> CreateScope() =>
            new QueryableScope(RuntimeTypes.Peek().Source, "_s" + Scopes.Count, InMemory);
    }
}
