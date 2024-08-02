using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Processing;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a custom transaction scope handler to the schema.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <typeparam name="T">
    /// The concrete type of the transaction scope handler.
    /// </typeparam>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddTransactionScopeHandler<T>(
        this IRequestExecutorBuilder builder)
        where T : class, ITransactionScopeHandler
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        // we host the transaction scope in the global DI.
        builder.Services.TryAddSingleton<T>();

        return ConfigureSchemaServices(
            builder,
            services =>
            {
                // we remove all handlers from the schema DI
                services.RemoveAll(typeof(ITransactionScopeHandler));

                // and then reference the transaction scope handler from the global DI.
                services.AddSingleton<ITransactionScopeHandler>(
                s => s.GetApplicationServices().GetRequiredService<T>());
            });
    }

    /// <summary>
    /// Adds a custom transaction scope handler to the schema.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="create">
    /// A factory to create the transaction scope.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IRequestExecutorBuilder AddTransactionScopeHandler(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, ITransactionScopeHandler> create)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return ConfigureSchemaServices(
            builder,
            services =>
            {
                services.RemoveAll(typeof(ITransactionScopeHandler));
                services.AddSingleton(sp => create(sp.GetCombinedServices()));
            });
    }

    /// <summary>
    /// Adds the <see cref="DefaultTransactionScopeHandler"/> which uses
    /// <see cref="System.Transactions.TransactionScope"/> for mutation transactions.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddDefaultTransactionScopeHandler(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return AddTransactionScopeHandler<DefaultTransactionScopeHandler>(builder);
    }

    internal static IRequestExecutorBuilder TryAddNoOpTransactionScopeHandler(
        this IRequestExecutorBuilder builder)
    {
        builder.Services.AddSingleton<NoOpTransactionScopeHandler>();

        return ConfigureSchemaServices(
            builder,
            services =>
            {
                services.TryAddSingleton<ITransactionScopeHandler>(
                    sp => sp.GetApplicationService<NoOpTransactionScopeHandler>());
            });
    }
}
