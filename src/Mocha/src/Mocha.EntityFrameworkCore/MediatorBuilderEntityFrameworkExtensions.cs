using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Extension methods for configuring Entity Framework Core transaction behavior
/// on the mediator pipeline.
/// </summary>
public static class MediatorBuilderEntityFrameworkExtensions
{
    /// <summary>
    /// Wraps command handler invocations in a database transaction using the specified
    /// <typeparamref name="TContext"/>. Commits on success, rolls back on failure.
    /// Streams, queries, and notifications are excluded at compile time.
    /// </summary>
    public static IMediatorHostBuilder UseEntityFrameworkTransactions<TContext>(
        this IMediatorHostBuilder builder,
        Action<MediatorEntityFrameworkOptions>? configure = null)
        where TContext : DbContext
    {
        builder.ConfigureMediator(b => b.UseEntityFrameworkTransactions<TContext>(configure));
        return builder;
    }

    /// <summary>
    /// Wraps command handler invocations in a database transaction using the specified
    /// <typeparamref name="TContext"/>. Commits on success, rolls back on failure.
    /// Streams, queries, and notifications are excluded at compile time.
    /// </summary>
    public static IMediatorBuilder UseEntityFrameworkTransactions<TContext>(
        this IMediatorBuilder builder,
        Action<MediatorEntityFrameworkOptions>? configure = null)
        where TContext : DbContext
    {
        var options = new MediatorEntityFrameworkOptions { ContextType = typeof(TContext) };
        configure?.Invoke(options);

        var feature = new EntityFrameworkTransactionFeature
        {
            ContextType = options.ContextType,
            ShouldCreateTransaction = options.ShouldCreateTransaction
        };

        builder.ConfigureFeature(features => features.Set(feature));
        builder.Use(EntityFrameworkTransactionMiddleware.Create());

        return builder;
    }
}
