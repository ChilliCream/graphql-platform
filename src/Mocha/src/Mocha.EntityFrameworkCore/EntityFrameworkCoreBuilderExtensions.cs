namespace Mocha.EntityFrameworkCore;

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
        builder.HostBuilder.ConfigureMessageBus(x =>
            x.ConfigureFeature(f =>
                f.Set(new ConsumerExecutionStrategyFeature
                {
                    Strategy = new EntityFrameworkConsumerExecutionStrategy(builder.ContextType)
                })));

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
            x.UseConsume(EntityFrameworkTransactionConsumeMiddleware.Create(builder.ContextType))
        );
        return builder;
    }
}
