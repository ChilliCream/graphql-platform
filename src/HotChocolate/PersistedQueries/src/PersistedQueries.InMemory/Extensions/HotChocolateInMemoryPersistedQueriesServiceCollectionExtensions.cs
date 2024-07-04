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
    /// Adds an in-memory operation document storage to the service collection.
    /// </summary>
    /// <param name="services">
    /// The service collection to which the services are added.
    /// </param>
    public static IServiceCollection AddInMemoryOperationDocumentStorage(
        this IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return services
            .RemoveService<IOperationDocumentStorage>()
            .AddSingleton<IOperationDocumentStorage>(
                c => new InMemoryQueryStorage(
                    c.GetService<IMemoryCache>() ??
                    c.GetApplicationService<IMemoryCache>()));
    }
}
