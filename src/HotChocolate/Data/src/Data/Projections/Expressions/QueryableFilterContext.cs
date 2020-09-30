using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions
{
    public class QueryableProjectionContext : ProjectionVisitorContext<Expression>
    {
        public QueryableProjectionContext(IResolverContext context, IOutputType initialType)
            : base(
                context,
                initialType,
                new QueryableProjectionScope(initialType.RuntimeType, "_s1"))
        {
            RuntimeTypes = new Stack<IExtendedType>();
        }

        public bool InMemory { get; }

        public Stack<IExtendedType> RuntimeTypes { get; }

        public override ProjectionScope<Expression> CreateScope() =>
            new QueryableProjectionScope(RuntimeTypes.Peek().Source, "_s" + Scopes.Count);
    }
}
