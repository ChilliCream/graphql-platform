using HotChocolate.Internal;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

internal sealed class ContextFactoryParameterExpressionBuilder<T>()
    : CustomParameterExpressionBuilder<T>(ctx => CreateDbContext(ctx))
    , IParameterBindingFactory
    , IParameterBinding
    where T : DbContext
{
    public ArgumentKind Kind => ArgumentKind.Custom;

    public bool IsPure => false;

    public IParameterBinding Create(ParameterBindingContext context)
    {
        return this;
    }

    public TCast Execute<TCast>(IResolverContext context)
    {
        var factory = context.Service<IDbContextFactory<T>>();
        var dbContext = factory.CreateDbContext();
        ((IMiddlewareContext)context).RegisterForCleanup(dbContext.DisposeAsync);
        return (TCast)(object)dbContext;
    }

    private static T CreateDbContext(IResolverContext context)
    {
        var factory = context.Service<IDbContextFactory<T>>();
        var dbContext = factory.CreateDbContext();
        ((IMiddlewareContext)context).RegisterForCleanup(dbContext.DisposeAsync);
        return dbContext;
    }
}
