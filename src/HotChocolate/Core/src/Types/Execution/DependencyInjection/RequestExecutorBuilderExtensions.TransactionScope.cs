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
    /// <remarks>
    /// The <typeparamref name="T"/> will be activated with the <see cref="IServiceProvider"/> of the schema services.
    /// If your <typeparamref name="T"/> needs to access application services you need to
    /// make the services available in the schema services via <see cref="AddApplicationService"/>.
    /// </remarks>
    public static IRequestExecutorBuilder AddTransactionScopeHandler<T>(
        this IRequestExecutorBuilder builder)
        where T : class, ITransactionScopeHandler
    {
        ArgumentNullException.ThrowIfNull(builder);

        // we host the transaction scope in the global DI.
        builder.Services.TryAddSingleton<T>();

        return ConfigureSchemaServices(
            builder,
            static services =>
            {
                services.RemoveAll<ITransactionScopeHandler>();
                services.AddSingleton<ITransactionScopeHandler, T>();
            });
    }

    /// <summary>
    /// Adds a custom transaction scope handler to the schema.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="factory">
    /// A factory to create the transaction scope.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="factory"/>
    /// is for the schema services. If you need to access application services
    /// you need to either make the services available in the schema services
    /// via <see cref="AddApplicationService"/> or use
    /// <see cref="ExecutionServiceProviderExtensions.GetRootServiceProvider(IServiceProvider)"/>
    /// to access the application services from within the schema service provider.
    /// </remarks>
    public static IRequestExecutorBuilder AddTransactionScopeHandler(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, ITransactionScopeHandler> factory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return ConfigureSchemaServices(
            builder,
            services =>
            {
                services.RemoveAll<ITransactionScopeHandler>();
                services.AddSingleton(factory);
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
        ArgumentNullException.ThrowIfNull(builder);

        return AddTransactionScopeHandler<DefaultTransactionScopeHandler>(builder);
    }

    internal static IRequestExecutorBuilder TryAddNoOpTransactionScopeHandler(
        this IRequestExecutorBuilder builder)
    {
        builder.Services.TryAddSingleton<NoOpTransactionScopeHandler>();

        return ConfigureSchemaServices(
            builder,
            static services =>
                services.TryAddSingleton<ITransactionScopeHandler, NoOpTransactionScopeHandler>());
    }
}
