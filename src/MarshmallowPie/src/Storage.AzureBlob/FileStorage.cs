using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;

namespace MarshmallowPie.Storage.AzureBlob
{
    public class FileStorage
        : IFileStorage
    {
        private readonly CloudBlobClient _client;
        private readonly CloudBlobContainer _container;

        public FileStorage(CloudBlobClient client, string containerName)
        {
            _client = client;
            _container = client.GetContainerReference(containerName);
        }

        public Task<bool> ContainerExistsAsync(
            string containerName,
            CancellationToken cancellationToken = default)
        {
            CloudBlobDirectory directory = _container.GetDirectoryReference(containerName);
            return Task.Run(() => directory.ListBlobs().Any(), cancellationToken);
        }

        public async Task<IFileContainer> CreateContainerAsync(
            string containerName,
            CancellationToken cancellationToken = default)
        {
            if (await ContainerExistsAsync(
                containerName, cancellationToken)
                .ConfigureAwait(false) == false)
            {
                return await GetOrCreateContainerAsync(
                    containerName, cancellationToken)
                    .ConfigureAwait(false);
            }

            throw new ArgumentException(
                $"The specified directory `{containerName}` already exists.",
                nameof(containerName));
        }

        public async Task<IFileContainer> GetContainerAsync(
            string containerName,
            CancellationToken cancellationToken = default)
        {
            if (await ContainerExistsAsync(
                containerName, cancellationToken)
                .ConfigureAwait(false) == false)
            {
                return await GetOrCreateContainerAsync(
                    containerName, cancellationToken)
                    .ConfigureAwait(false);
            }

            throw new DirectoryNotFoundException(
                $"The specified directory `{containerName}` does not exists.");
        }

        public async Task<IFileContainer> GetOrCreateContainerAsync(
            string containerName,
            CancellationToken cancellationToken = default)
        {
            await _container.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);

            return new FileContainer(
                _client,
                _container.GetDirectoryReference(containerName),
                containerName);
        }
    }
}
