using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contains dependency injection Helpers to setup a subscription provider.
/// </summary>
public static class CoreSubscriptionsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the subscription provider diagnostics.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddSubscriptionDiagnostics(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSubscriptionDiagnostics();

        return builder;
    }

    /// <summary>
    /// Adds the subscription provider diagnostics.
    /// </summary>
    /// <param name="services">
    /// The ServiceCollection.
    /// </param>
    /// <returns>
    /// Returns the ServiceCollection for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddSubscriptionDiagnostics(
        this IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.TryAddSingleton<ISubscriptionDiagnosticEvents>(
            sp =>
            {
                var listeners = sp.GetService<IEnumerable<ISubscriptionDiagnosticEventsListener>>();

                // if we do not have any listeners registered we will register a noop listener.
                if (listeners is null)
                {
                    return NoopSubscriptionDiagnosticEventsListener.Default;
                }

                var listenerArray = listeners.ToArray();

                // we recheck if the array really has listeners; otherwise,
                // if we do not have any listeners registered we will register a noop listener.
                if (listenerArray.Length == 0)
                {
                    return NoopSubscriptionDiagnosticEventsListener.Default;
                }

                // if we only have a single listener we will just return this one.
                if (listenerArray.Length == 1)
                {
                    return listenerArray[0];
                }

                // if we have more than one listener we will return an aggregate listener,
                // which will trigger for each event all listeners.
                return new AggregateSubscriptionDiagnosticEventsListener(listenerArray);
            });

        return services;
    }
}
