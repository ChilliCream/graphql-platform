using System;
using HotChocolate.Data;
using HotChocolate.Data.Internal;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring an <see cref="IResolverCompilerBuilder"/>
/// </summary>
public static class EntityFrameworkRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddPooledDbContext<TDbContext>(
        this IRequestExecutorBuilder builder)
        where TDbContext : DbContext
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services
            .AddSingleton<IParameterExpressionBuilder,
                PooledDbConfigurationParameterExpressionBuilder<TDbContext>>();
        builder.TryAddTypeInterceptor<PooledDbContextTypeInterceptor>();
        return builder;
    }

    public static IRequestExecutorBuilder AddScopedDbContext<TDbContext>(
        this IRequestExecutorBuilder builder,
        DbContextScope scope = DbContextScope.Request)
        where TDbContext : DbContext
    {
        builder.Services.AddSingleton<IParameterExpressionBuilder>(
            new ScopedDbConfigurationParameterExpressionBuilder<TDbContext>(scope));
        return builder;
    }
}

