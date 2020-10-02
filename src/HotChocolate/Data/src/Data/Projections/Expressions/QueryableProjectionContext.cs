using System.Linq.Expressions;
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
        }

        public override ProjectionScope<Expression> CreateScope() =>
            new QueryableProjectionScope(Selection.Peek().Field.RuntimeType, "_s" + Scopes.Count);
    }
}
