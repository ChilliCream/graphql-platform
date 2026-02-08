using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Processing;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds an operation compiler optimizer to the GraphQL configuration.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The <see cref="IOperationCompilerOptimizer"/> implementation.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <remarks>
    /// The <typeparamref name="T"/> will be activated with the <see cref="IServiceProvider"/> of the schema services.
    /// If your <typeparamref name="T"/> needs to access application services you need to
    /// make the services available in the schema services via <see cref="RequestExecutorBuilderExtensions.AddApplicationService"/>.
    /// </remarks>
    public static IRequestExecutorBuilder AddOperationCompilerOptimizer<T>(
        this IRequestExecutorBuilder builder)
        where T : class, IOperationCompilerOptimizer
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureSchemaServices(s => s.AddSingleton<IOperationCompilerOptimizer, T>());
        return builder;
    }

    /// <summary>
    /// Adds an operation compiler optimizer to the GraphQL configuration.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// A factory that creates the optimizer instance.
    /// </param>
    /// <typeparam name="T">
    /// The <see cref="IOperationCompilerOptimizer"/> implementation.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="factory"/>
    /// is for the schema services. If you need to access application services
    /// you need to either make the services available in the schema services
    /// via <see cref="RequestExecutorBuilderExtensions.AddApplicationService"/> or use
    /// <see cref="ExecutionServiceProviderExtensions.GetRootServiceProvider(IServiceProvider)"/>
    /// to access the application services from within the schema service provider.
    /// </remarks>
    public static IRequestExecutorBuilder AddOperationCompilerOptimizer<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IOperationCompilerOptimizer
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureSchemaServices(
            sc => sc.AddSingleton<IOperationCompilerOptimizer>(factory));
        return builder;
    }
}
