using Azure.Storage.Blobs;
using HotChocolate.Execution.Configuration;
using HotChocolate;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides utility methods to setup dependency injection.
/// </summary>
public static class HotChocolateAzureBlobStoragePersistedOperationsRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds an Azure Blob Storage based operation document storage to the service collection.
    /// </summary>
    /// <param name="builder">
    /// The service collection to which the services are added.
    /// </param>
    /// <param name="containerClientFactory">
    /// A factory that resolves the Azure Blob Container Client that
    /// shall be used for persistence.
    /// </param>
    public static IRequestExecutorBuilder AddAzureBlobStorageOperationDocumentStorage(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, BlobContainerClient> containerClientFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(containerClientFactory);

        return builder.ConfigureSchemaServices(
            s => s.AddAzureBlobStorageOperationDocumentStorage(
                sp => containerClientFactory(sp.GetCombinedServices())));
    }
}
