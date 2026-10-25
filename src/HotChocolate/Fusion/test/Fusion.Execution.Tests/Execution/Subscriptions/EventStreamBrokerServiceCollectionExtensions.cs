using HotChocolate.Fusion.Subscriptions;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides test service registration helpers for Fusion event stream brokers.
/// </summary>
public static class EventStreamBrokerServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory event stream broker as the default broker.
    /// </summary>
    /// <param name="services">
    /// The service collection to configure.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddInMemoryEventStreamBroker(this IServiceCollection services)
        => services.AddInMemoryEventStreamBroker(broker: null);

    /// <summary>
    /// Registers the in-memory event stream broker for the specified broker label.
    /// </summary>
    /// <param name="services">
    /// The service collection to configure.
    /// </param>
    /// <param name="broker">
    /// The broker label used by the execution schema, or <c>null</c> to register the default broker.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddInMemoryEventStreamBroker(
        this IServiceCollection services,
        string? broker)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IEventStreamBrokerFactory, DefaultEventStreamBrokerFactory>();
        services.TryAddSingleton<InMemoryEventStreamBrokerHub>();
        services.TryAddSingleton<IInMemoryEventStreamPublisher>(
            static sp => sp.GetRequiredService<InMemoryEventStreamBrokerHub>());
        services.TryAddKeyedSingleton<IEventStreamBrokerProvider>(
            broker ?? DefaultEventStreamBrokerFactory.DefaultBrokerKey,
            static (sp, _) => new InMemoryEventStreamBrokerProvider(
                sp.GetRequiredService<InMemoryEventStreamBrokerHub>()));

        return services;
    }
}
