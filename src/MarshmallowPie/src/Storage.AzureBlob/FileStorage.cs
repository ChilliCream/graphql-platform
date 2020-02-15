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
        private bool _isInitialized;

        public FileStorage(CloudBlobClient client, string containerName)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentException(
                    "The container name mustn't be null or empty.",
                    nameof(containerName));
            }

            _client = client ?? throw new ArgumentNullException(nameof(client));
            _container = client.GetContainerReference(containerName);
        }

        public async Task<bool> ContainerExistsAsync(
            string containerName,
            CancellationToken cancellationToken = default)
        {
            await EnsureContainerExistsAsync(cancellationToken).ConfigureAwait(false);

            CloudBlobDirectory directory = _container.GetDirectoryReference(containerName);

            try
            {
                return await Task.Run(
                    () => directory.ListBlobs().Any(), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // TODO : FIX THIS
                return false;
            }
        }

        public async Task<IFileContainer> CreateContainerAsync(
            string containerName,
            CancellationToken cancellationToken = default)
        {
            await EnsureContainerExistsAsync(cancellationToken).ConfigureAwait(false);

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
            await EnsureContainerExistsAsync(cancellationToken).ConfigureAwait(false);

            if (await ContainerExistsAsync(
                containerName, cancellationToken)
                .ConfigureAwait(false))
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
            await EnsureContainerExistsAsync(cancellationToken).ConfigureAwait(false);

            await _container.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);

            return new FileContainer(
                _client,
                _container.GetDirectoryReference(containerName),
                containerName);
        }

        private async Task EnsureContainerExistsAsync(
            CancellationToken cancellationToken = default)
        {
            if (!_isInitialized)
            {
                await _container.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
                _isInitialized = true;
            }
        }
    }
}
