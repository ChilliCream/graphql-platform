using System;
using Microsoft.Azure.Storage.Blob;
using MarshmallowPie;
using MarshmallowPie.Storage;
using MarshmallowPie.Storage.AzureBlob;

namespace Microsoft.Extensions.DependencyInjection
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
