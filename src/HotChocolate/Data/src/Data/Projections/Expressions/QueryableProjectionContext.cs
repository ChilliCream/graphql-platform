using System;
using System.Linq.Expressions;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions;

public class QueryableProjectionContext : ProjectionVisitorContext<Expression>
{
    public QueryableProjectionContext(
        IResolverContext context,
        IOutputType initialType,
        Type runtimeType)
        : base(context, initialType, new QueryableProjectionScope(runtimeType, "_s1"))
    {
    }

    public QueryableProjectionContext(
        IResolverContext context,
        IOutputType initialType,
        Type runtimeType,
        bool disableNullChecks)
        : this(context, initialType, runtimeType)
    {
        DisableNullChecks = disableNullChecks;
    }

    public bool DisableNullChecks { get; }
}
