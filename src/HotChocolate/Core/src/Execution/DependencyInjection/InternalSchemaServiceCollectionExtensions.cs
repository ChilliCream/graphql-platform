using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for the <see cref="IServiceCollection"/> type.
/// </summary>
public static class InternalSchemaServiceCollectionExtensions
{
    internal static IServiceCollection TryAddOperationExecutors(
        this IServiceCollection services)
    {
        services.TryAddSingleton<QueryExecutor>();
        services.TryAddSingleton(
            sp => new SubscriptionExecutor(
                sp.GetRootServiceProvider().GetRequiredService<ObjectPool<OperationContext>>(),
                sp.GetRequiredService<QueryExecutor>(),
                sp.GetRequiredService<IErrorHandler>(),
                sp.GetRequiredService<IExecutionDiagnosticEvents>()));
        return services;
    }

    internal static IServiceCollection TryAddDiagnosticEvents(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IExecutionDiagnosticEvents>(sp =>
        {
            var listeners = sp.GetServices<IExecutionDiagnosticEventListener>().ToArray();
            return listeners.Length switch
            {
                0 => new NoopExecutionDiagnosticEvents(),
                1 => listeners[0],
                _ => new AggregateExecutionDiagnosticEvents(listeners)
            };
        });

        services.TryAddSingleton<ICoreExecutionDiagnosticEvents>(
            sp => sp.GetRequiredService<IExecutionDiagnosticEvents>());

        return services;
    }

    /// <summary>
    /// Gets the root service provider from the schema services. This allows
    /// schema services to access application level services.
    /// </summary>
    /// <param name="schema">
    /// The schema.
    /// </param>
    /// <returns>
    /// The root service provider.
    /// </returns>
    public static IServiceProvider GetRootServiceProvider(this Schema schema)
        => schema.Services.GetRequiredService<IRootServiceProviderAccessor>().ServiceProvider;

    /// <summary>
    /// Gets the root service provider from the schema services. This allows
    /// schema services to access application level services.
    /// </summary>
    /// <param name="services">
    /// The schema services.
    /// </param>
    /// <returns>
    /// The root service provider.
    /// </returns>
    public static IServiceProvider GetRootServiceProvider(this IServiceProvider services)
        => services.GetRequiredService<IRootServiceProviderAccessor>().ServiceProvider;

    /// <summary>
    /// Gets a service from the root service provider.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the service to get.
    /// </typeparam>
    /// <param name="services">
    /// The schema services.
    /// </param>
    /// <returns>
    /// The service.
    /// </returns>
    [Obsolete("Use GetRootServiceProvider instead.")]
    public static T GetApplicationService<T>(this IServiceProvider services) where T : notnull
        => services.GetApplicationServices().GetRequiredService<T>();

    /// <summary>
    /// Gets the root service provider from the schema services. This allows
    /// schema services to access application level services.
    /// </summary>
    /// <param name="services">
    /// The schema services.
    /// </param>
    /// <returns>
    /// The root service provider.
    /// </returns>
    [Obsolete("Use GetRootServiceProvider instead.")]
    public static IServiceProvider GetApplicationServices(this IServiceProvider services)
        => services.GetRootServiceProvider();

    /// <summary>
    /// Gets a service provided that represents the combined services from the schema services
    /// and application services.
    /// </summary>
    /// <param name="services">
    /// The schema services.
    /// </param>
    /// <returns>
    /// The service.
    /// </returns>
    public static IServiceProvider GetCombinedServices(this IServiceProvider services)
        => services is CombinedServiceProvider combined
            ? combined
            : new CombinedServiceProvider(
                services.GetRootServiceProvider(),
                services);
}
