using System.Linq.Expressions;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions;

public class QueryableProjectionContext(
    IResolverContext context,
    IOutputType initialType,
    Type runtimeType,
    bool inMemory)
    : ProjectionVisitorContext<Expression>(
        context,
        initialType,
        new QueryableProjectionScope(runtimeType, "_s1"))
{
    public bool InMemory { get; } = inMemory;
}
