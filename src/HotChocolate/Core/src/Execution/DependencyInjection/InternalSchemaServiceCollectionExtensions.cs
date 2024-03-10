using System;
using System.Linq;
using HotChocolate;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class InternalSchemaServiceCollectionExtensions
{
    internal static IServiceCollection TryAddOperationExecutors(
        this IServiceCollection services)
    {
        services.TryAddSingleton<QueryExecutor>();
        services.TryAddSingleton(
            sp => new SubscriptionExecutor(
                sp.GetApplicationService<ObjectPool<OperationContext>>(),
                sp.GetRequiredService<QueryExecutor>(),
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
                _ => new AggregateExecutionDiagnosticEvents(listeners),
            };
        });
        return services;
    }
    
    public static T GetApplicationService<T>(this IServiceProvider services) where T : notnull
        => services.GetApplicationServices().GetRequiredService<T>();

    public static IServiceProvider GetApplicationServices(this IServiceProvider services) =>
        services.GetRequiredService<IApplicationServiceProvider>();

    /// <summary>
    /// Gets a service provided that represents the combined services from the schema services
    /// and application services.
    /// </summary>
    public static IServiceProvider GetCombinedServices(this IServiceProvider services) =>
        services is CombinedServiceProvider combined
            ? combined
            : new CombinedServiceProvider(
                services.GetApplicationServices(),
                services);
}
