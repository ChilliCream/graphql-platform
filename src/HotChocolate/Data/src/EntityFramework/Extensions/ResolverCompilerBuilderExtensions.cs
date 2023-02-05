using HotChocolate.Data;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring an <see cref="IResolverCompilerBuilder"/>
/// </summary>
public static class EntityFrameworkRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Registers a well-known <see cref="DbContext"/> with the resolver compiler.
    /// The <see cref="DbContext"/> does no longer need any annotation in the resolver.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="kind">
    /// The <see cref="DbContext"/> kind defines the way a <see cref="DbContext"/> is injected
    /// and handled by the execution engine.
    /// </param>
    /// <typeparam name="TDbContext">
    /// The <see cref="DbContext"/> type.
    /// </typeparam>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    public static IRequestExecutorBuilder RegisterDbContext<TDbContext>(
        this IRequestExecutorBuilder builder,
        DbContextKind kind = DbContextKind.Resolver)
        where TDbContext : DbContext
    {
        builder.Services.AddSingleton<IParameterExpressionBuilder>(
            new DbContextParameterExpressionBuilder<TDbContext>(kind));
        return builder;
    }
}

