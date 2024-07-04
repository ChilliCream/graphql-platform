using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.PersistedQueries.FileSystem;
using HotChocolate.Utilities;

namespace HotChocolate;

/// <summary>
/// Provides utility methods to setup dependency injection.
/// </summary>
public static class HotChocolateFileSystemPersistedQueriesServiceCollectionExtensions
{
    /// <summary>
    /// Adds a file system based operation document storage to the service collection.
    /// </summary>
    /// <param name="services">
    /// The service collection to which the services are added.
    /// </param>
    /// <param name="cacheDirectory">
    /// The directory path that shall be used to read queries from.
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
            .RemoveService<IQueryFileMap>()
            .AddSingleton<IOperationDocumentStorage, FileSystemQueryStorage>()
            .AddSingleton<IQueryFileMap>(
                cacheDirectory is null
                    ? new DefaultQueryFileMap()
                    : new DefaultQueryFileMap(cacheDirectory));
    }
}