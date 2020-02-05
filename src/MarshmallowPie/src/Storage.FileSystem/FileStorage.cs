using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Storage.FileSystem
{
    public class FileStorage : IFileStorage
    {
        private string _fullDirectoryPath;

        public FileStorage(string fullDirectoryPath)
        {
            if (!Directory.Exists(fullDirectoryPath))
            {
                throw new ArgumentException(
                    $"The specified directory `{fullDirectoryPath}` does not exist.",
                    nameof(fullDirectoryPath));
            }

            _fullDirectoryPath = fullDirectoryPath;
        }

        public Task<IFileContainer> CreateContainerAsync(
            string containerName,
            CancellationToken cancellationToken = default)
        {
            string fullContainerPath = Path.Combine(_fullDirectoryPath, containerName);

            if (Directory.Exists(fullContainerPath))
            {
                throw new ArgumentException(
                    $"The specified directory `{fullContainerPath}` already exists.",
                    nameof(containerName));
            }

            return CreateContainerInternalAsync(fullContainerPath, cancellationToken);
        }

        private async Task<IFileContainer> CreateContainerInternalAsync(
            string fullContainerPath,
            CancellationToken cancellationToken = default)
        {
            await Task.Factory.StartNew(
                () => Directory.CreateDirectory(fullContainerPath),
                cancellationToken,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default)
                .ConfigureAwait(false);

            return new FileContainer(fullContainerPath);
        }

        public Task<IFileContainer> GetContainerAsync(
            string containerName,
            CancellationToken cancellationToken = default)
        {
            string fullContainerPath = Path.Combine(_fullDirectoryPath, containerName);

            if (!Directory.Exists(fullContainerPath))
            {
                throw new DirectoryNotFoundException(
                    $"The specified directory `{fullContainerPath}` does not exists.");
            }

            return Task.FromResult<IFileContainer>(new FileContainer(fullContainerPath));
        }

        public Task<IFileContainer> GetOrCreateContainerAsync(
            string containerName,
            CancellationToken cancellationToken = default)
        {
            string fullContainerPath = Path.Combine(_fullDirectoryPath, containerName);

            if (Directory.Exists(fullContainerPath))
            {
                return GetContainerAsync(containerName);
            }
            else
            {
                return CreateContainerAsync(containerName);
            }
        }

        public Task<bool> ContainerExistsAsync(
            string containerName,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Directory.Exists(Path.Combine(_fullDirectoryPath, containerName)));
        }
    }
}
