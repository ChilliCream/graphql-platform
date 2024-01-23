using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using HotChocolate.Execution;
using HotChocolate.PersistedQueries.FileSystem;
using HotChocolate.Utilities;

namespace HotChocolate;

/// <summary>
/// Provides utility methods to setup dependency injection.
/// </summary>
public static class HotChocolateInMemoryPersistedQueriesServiceCollectionExtensions
{
    /// <summary>
    /// Adds a file system read and write query storage to the
    /// services collection.
    /// </summary>
    /// <param name="services">
    /// The service collection to which the services are added.
    /// </param>
    public static IServiceCollection AddInMemoryQueryStorage(
        this IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return services
            .AddReadOnlyInMemoryQueryStorage()
            .AddSingleton<IWriteStoredQueries>(
                sp => sp.GetRequiredService<InMemoryQueryStorage>());
    }

    /// <summary>
    /// Adds a file system read-only query storage to the
    /// services collection.
    /// </summary>
    /// <param name="services">
    /// The service collection to which the services are added.
    /// </param>
    public static IServiceCollection AddReadOnlyInMemoryQueryStorage(
        this IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return services
            .RemoveService<IReadStoredQueries>()
            .RemoveService<IWriteStoredQueries>()
            .AddSingleton(c => new InMemoryQueryStorage(
                c.GetService<IMemoryCache>() ??
                c.GetApplicationService<IMemoryCache>()))
            .AddSingleton<IReadStoredQueries>(
                sp => sp.GetRequiredService<InMemoryQueryStorage>());
    }
}
