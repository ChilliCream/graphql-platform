using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Internal;

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
            RuntimeTypes = new Stack<IExtendedType>();
            RuntimeTypes.Push(initialType.EntityType);
        }

        public bool InMemory { get; }

        public Stack<IExtendedType> RuntimeTypes { get; }

        public override FilterScope<Expression> CreateScope() =>
            new QueryableScope(RuntimeTypes.Peek(), "_s" + Scopes.Count, InMemory);
    }
}
