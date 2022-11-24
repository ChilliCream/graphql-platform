using System;
using HotChocolate.Subscriptions.Redis;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection helpers to register the Newtonsoft based message
/// serializer for the Hot Chocolate Redis subscription provider.
/// </summary>
public static class NewtonsoftRedisSubscriptionsServiceCollectionExtensions
{
    /// <summary>
    /// Adds a Newtonsoft based message serializer for the Hot Chocolate
    /// Redis subscription provider.
    /// </summary>
    /// <param name="services">
    /// The service collection.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IServiceCollection"/> to chain configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="services"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddRedisNewtonsoftSerialization(
        this IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.RemoveAll<IMessageSerializer>();
        services.TryAddSingleton<IMessageSerializer, NewtonsoftJsonMessageSerializer>();
        return services;
    }
}
