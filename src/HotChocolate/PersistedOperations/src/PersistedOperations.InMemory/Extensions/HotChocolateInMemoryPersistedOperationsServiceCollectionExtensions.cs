using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using HotChocolate.PersistedOperations;
using HotChocolate.PersistedOperations.FileSystem;
using HotChocolate.Utilities;

namespace HotChocolate;

/// <summary>
/// Provides utility methods to setup dependency injection.
/// </summary>
public static class HotChocolateInMemoryPersistedOperationsServiceCollectionExtensions
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
        ArgumentNullException.ThrowIfNull(services);

        return services
            .RemoveService<IOperationDocumentStorage>()
            .AddSingleton<IOperationDocumentStorage>(
                c => new InMemoryOperationDocumentStorage(
                    c.GetService<IMemoryCache>() ??
                    c.GetRootServiceProvider().GetRequiredService<IMemoryCache>()));
    }
}
