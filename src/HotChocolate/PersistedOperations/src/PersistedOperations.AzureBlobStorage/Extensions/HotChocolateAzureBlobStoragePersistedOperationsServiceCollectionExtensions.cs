using Azure.Storage.Blobs;
using HotChocolate.PersistedOperations;
using HotChocolate.PersistedOperations.AzureBlobStorage;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate;

/// <summary>
/// Provides utility methods to set up dependency injection.
/// </summary>
public static class HotChocolateAzureBlobStoragePersistedOperationsServiceCollectionExtensions
{
    /// <summary>
    /// Adds an Azure Blob Storage based operation document storage to the service collection.
    /// </summary>
    /// <param name="services">
    /// The service collection to which the services are added.
    /// </param>
    /// <param name="containerClientFactory">
    /// A factory that resolves the Azure Blob Container Client that
    /// shall be used for persistence.
    /// </param>
    public static IServiceCollection AddAzureBlobStorageOperationDocumentStorage(
        this IServiceCollection services,
        Func<IServiceProvider, BlobContainerClient> containerClientFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(containerClientFactory);

        return services
            .RemoveService<IOperationDocumentStorage>()
            .AddSingleton<IOperationDocumentStorage>(
                sp => new AzureBlobOperationDocumentStorage(containerClientFactory(sp)));
    }

    private static IServiceCollection RemoveService<TService>(
        this IServiceCollection services)
    {
        var serviceDescriptor = services.FirstOrDefault(t => t.ServiceType == typeof(TService));

        if (serviceDescriptor != null)
        {
            services.Remove(serviceDescriptor);
        }

        return services;
    }
}
