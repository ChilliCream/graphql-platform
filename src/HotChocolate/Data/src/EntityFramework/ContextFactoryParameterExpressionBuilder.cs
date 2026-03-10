using System.Runtime.CompilerServices;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

internal sealed class ContextFactoryParameterExpressionBuilder<TDbContext>()
    : CustomParameterExpressionBuilder<TDbContext>(ctx => CreateDbContext(ctx))
    , IParameterBindingFactory
    , IParameterBinding
    where TDbContext : DbContext
{
    public ArgumentKind Kind => ArgumentKind.Custom;

    public bool IsPure => false;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterDescriptor parameter)
        => parameter.Type == typeof(TDbContext);

    public IParameterBinding Create(ParameterDescriptor parameter) => this;

    public T Execute<T>(IResolverContext context)
    {
        var dbContext = CreateDbContext(context);
        return Unsafe.As<TDbContext, T>(ref dbContext);
    }

    private static TDbContext CreateDbContext(IResolverContext context)
    {
        var factory = context.Service<IDbContextFactory<TDbContext>>();
        var dbContext = factory.CreateDbContext();
        ((IMiddlewareContext)context).RegisterForCleanup(dbContext.DisposeAsync);
        return dbContext;
    }
}
