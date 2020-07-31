using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableFilterContext : FilterVisitorContext<Expression>
    {
        public QueryableFilterContext(
            IFilterInputType initialType,
            bool inMemory)
            : base(initialType,
                  new QueryableScope(initialType.EntityType, "_s0", inMemory))
        {
            InMemory = inMemory;
            ClrTypes = new Stack<Type>();
            ClrTypes.Push(initialType.EntityType);
            TypeInfos = new Stack<FilterTypeInfo>();
            TypeInfos.Push(new FilterTypeInfo(
                !inMemory,
                initialType.EntityType,
                Array.Empty<FilterTypeInfo>()));
        }

        public bool InMemory { get; }

        public Stack<Type> ClrTypes { get; }

        public Stack<FilterTypeInfo> TypeInfos { get; }

        public override FilterScope<Expression> CreateScope() =>
             new QueryableScope(ClrTypes.Peek(), "_s" + Scopes.Count, InMemory);
    }
}
