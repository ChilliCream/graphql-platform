using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
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
                0 => NoopExecutionDiagnosticEvents.Instance,
                1 => listeners[0],
                _ => new AggregateExecutionDiagnosticEvents(listeners)
            };
        });

        services.TryAddSingleton<ICoreExecutionDiagnosticEvents>(
            sp => sp.GetRequiredService<IExecutionDiagnosticEvents>());

        return services;
    }
}
