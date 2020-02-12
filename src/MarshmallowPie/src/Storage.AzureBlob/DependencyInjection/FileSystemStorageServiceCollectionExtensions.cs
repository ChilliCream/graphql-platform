using System;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.DependencyInjection;
using MarshmallowPie.Storage;
using MarshmallowPie.Storage.AzureBlob;

namespace MarshmallowPie
{
    public static class AzureBlobStorageServiceCollectionExtensions
    {
        public static IServiceCollection AddAzureBlobStorage(
            this IServiceCollection serviceCollection,
            Func<IServiceProvider, CloudBlobClient> cloudClient,
            string containerName)
        {
            return serviceCollection.AddSingleton<IFileStorage>(sp =>
                new FileStorage(cloudClient(sp), containerName));
        }
    }
}
