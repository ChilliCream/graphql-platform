using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.PersistedOperations.FileSystem;
using HotChocolate.Utilities;

namespace HotChocolate;

/// <summary>
/// Provides utility methods to setup dependency injection.
/// </summary>
public static class HotChocolateFileSystemPersistedOperationsServiceCollectionExtensions
{
    /// <summary>
    /// Adds a file-system-based operation document storage to the service collection.
    /// </summary>
    /// <param name="services">
    /// The service collection to which the services are added.
    /// </param>
    /// <param name="cacheDirectory">
    /// The directory path that shall be used to store operation documents.
    /// </param>
    public static IServiceCollection AddFileSystemOperationDocumentStorage(
        this IServiceCollection services,
        string? cacheDirectory = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return services
            .RemoveService<IOperationDocumentStorage>()
            .RemoveService<IOperationDocumentFileMap>()
            .AddSingleton<IOperationDocumentStorage, FileSystemOperationDocumentStorage>()
            .AddSingleton<IOperationDocumentFileMap>(
                cacheDirectory is null
                    ? new DefaultOperationDocumentFileMap()
                    : new DefaultOperationDocumentFileMap(cacheDirectory));
    }
}
