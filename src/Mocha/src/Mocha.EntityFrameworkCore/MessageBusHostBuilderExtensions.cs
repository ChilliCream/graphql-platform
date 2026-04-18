using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Provides extension methods on <see cref="IMessageBusHostBuilder"/> for integrating Entity Framework Core
/// persistence with the message bus infrastructure.
/// </summary>
public static class MessageBusHostBuilderExtensions
{
    /// <summary>
    /// Registers an Entity Framework Core <see cref="DbContext"/> with the message bus host and
    /// configures persistence features such as outbox and saga storage.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext"/> type used for messaging persistence.</typeparam>
    /// <param name="builder">The message bus host builder to configure.</param>
    /// <param name="configure">A delegate that configures Entity Framework Core features on the builder.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IMessageBusHostBuilder AddEntityFramework<TContext>(
        this IMessageBusHostBuilder builder,
        Action<IEntityFrameworkCoreBuilder> configure)
        where TContext : DbContext
    {
        var persistenceBuilder = new EntityFrameworkCoreBuilder
        {
            Services = builder.Services,
            HostBuilder = builder,
            ContextType = typeof(TContext),
            Name = typeof(TContext).FullName ?? typeof(TContext).Name
        };

        configure(persistenceBuilder);

        builder
            .Services.AddOptions<MessagingDbContextOptions>(persistenceBuilder.Name)
            .Configure<IServiceProvider>((options, serviceProvider) => options.ServiceProvider = serviceProvider);

        builder.Services.AddSingleton<
            IDbContextOptionsConfiguration<TContext>,
            MessagingDbContextOptionsConfiguration<TContext>
        >();

        builder.ConfigureMessageBus(x =>
            x.ConfigureFeature(f => f.Set(new EntityFrameworkConfigurationFeature { ContextType = typeof(TContext) }))
        );

        return builder;
    }
}

/// <summary>
/// Provides extension methods on <see cref="IEntityFrameworkCoreBuilder"/> for configuring
/// resilience and transaction middleware for Entity Framework Core consumers.
/// </summary>
public static class EntityFrameworkCoreBuilderExtensions
{
    /// <summary>
    /// Wraps consumer execution with the DbContext execution strategy, enabling automatic retry
    /// for transient database failures such as connection drops or deadlocks.
    /// </summary>
    /// <param name="builder">The Entity Framework Core builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IEntityFrameworkCoreBuilder UseResilience(this IEntityFrameworkCoreBuilder builder)
    {
        builder.HostBuilder.ConfigureMessageBus(x => x.UseConsume(EntityFrameworkResilienceConsumeMiddleware.Create()));

        return builder;
    }

    /// <summary>
    /// Wraps each consumer invocation in a database transaction, committing on success
    /// and rolling back on failure.
    /// </summary>
    /// <param name="builder">The Entity Framework Core builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IEntityFrameworkCoreBuilder UseTransaction(this IEntityFrameworkCoreBuilder builder)
    {
        builder.HostBuilder.ConfigureMessageBus(x =>
            x.UseConsume(EntityFrameworkTransactionConsumeMiddleware.Create())
        );
        return builder;
    }
}

internal sealed class EntityFrameworkConfigurationFeature
{
    public Type? ContextType { get; set; }
}

internal sealed class EntityFrameworkResilienceConsumeMiddleware(Type contextType)
{
    public async ValueTask InvokeAsync(IConsumeContext context, ConsumerDelegate next)
    {
        var dbContext = (DbContext)context.Services.GetRequiredService(contextType);
        var strategy = dbContext.Database.CreateExecutionStrategy();

        if (!strategy.RetriesOnFailure)
        {
            await next(context);
            return;
        }

        var originalServices = context.Services;

        await strategy.ExecuteAsync(async () =>
        {
            await using var scope = originalServices.CreateAsyncScope();
            context.Services = scope.ServiceProvider;

            try
            {
                await next(context);
            }
            finally
            {
                context.Services = originalServices;
            }
        });
    }

    public static ConsumerMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var contextType =
                    context
                        .Services.GetRequiredService<IFeatureCollection>()
                        .Get<EntityFrameworkConfigurationFeature>()
                        ?.ContextType
                    ?? throw new InvalidOperationException(
                        "No Entity Framework Core DbContext type has been configured. "
                        + "Call AddEntityFramework<TContext>() on the message bus host builder "
                        + "before using UseResilience().");

                var middleware = new EntityFrameworkResilienceConsumeMiddleware(contextType);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "EntityFrameworkResilience");
}

internal sealed class EntityFrameworkTransactionConsumeMiddleware(Type contextType)
{
    public async ValueTask InvokeAsync(IConsumeContext context, ConsumerDelegate next)
    {
        var dbContext = (DbContext)context.Services.GetRequiredService(contextType);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(context.CancellationToken);

        try
        {
            await next(context);

            await transaction.CommitAsync(context.CancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(context.CancellationToken);
            throw;
        }
    }

    public static ConsumerMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var contextType =
                    context
                        .Services.GetRequiredService<IFeatureCollection>()
                        .Get<EntityFrameworkConfigurationFeature>()
                        ?.ContextType
                    ?? throw new InvalidOperationException(
                        "No Entity Framework Core DbContext type has been configured. "
                        + "Call AddEntityFramework<TContext>() on the message bus host builder "
                        + "before using UseTransaction().");

                var middleware = new EntityFrameworkTransactionConsumeMiddleware(contextType);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "EntityFrameworkTransaction");
}
