using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IOFile = System.IO.File;

namespace MarshmallowPie.Storage.FileSystem
{
    public class FileContainer
        : IFileContainer
    {
        private readonly string _fullDirectoryPath;

        public FileContainer(string fullDirectoryPath)
        {
            _fullDirectoryPath = fullDirectoryPath;
            Name = Path.GetFileName(fullDirectoryPath)!;
        }

        public string Name { get; }

        public Task CreateFileAsync(
            string fileName, byte[] buffer, int offset, int count,
            CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(_fullDirectoryPath))
            {
                throw new DirectoryNotFoundException(
                    $"The directory `{_fullDirectoryPath}` does not exist.");
            }

            var fullFilePath = Path.Combine(_fullDirectoryPath, fileName);

            if (IOFile.Exists(fullFilePath))
            {
                throw new ArgumentException(
                    $"The specified file `{fullFilePath}` already exists.",
                    nameof(fileName));
            }

            return CreateFileInternalAsync(fullFilePath, buffer, offset, count, cancellationToken);
        }

        public async Task CreateFileInternalAsync(
            string fullFilePath, byte[] buffer, int offset, int count,
            CancellationToken cancellationToken = default)
        {
            await using FileStream stream = IOFile
                .Create(fullFilePath)
                .ConfigureAwait(false);
            await stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        }

        public Task DeleteAsync(CancellationToken cancellationToken = default)
        {
            if (Directory.Exists(_fullDirectoryPath))
            {
                return Task.Factory.StartNew(
                    () => Directory.Delete(_fullDirectoryPath, true),
                    cancellationToken,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
            }

            throw new DirectoryNotFoundException(
                $"The directory `{_fullDirectoryPath}` does not exist.");
        }

        public Task<IFile> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
        {
            var fullFilePath = Path.Combine(_fullDirectoryPath, fileName);

            if (!IOFile.Exists(fullFilePath))
            {
                throw new DirectoryNotFoundException(
                    $"The file `{fullFilePath}` does not exist.");
            }

            return Task.FromResult<IFile>(new File(fullFilePath));
        }

        public async Task<IEnumerable<IFile>> GetFilesAsync(
            CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(_fullDirectoryPath))
            {
                throw new DirectoryNotFoundException(
                    $"The directory `{_fullDirectoryPath}` does not exist.");
            }

            string[] files = await Task.Factory.StartNew(
                () => Directory.GetFiles(_fullDirectoryPath),
                cancellationToken,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default)
                .ConfigureAwait(false);

            return files.Select(f => new File(f)).ToList();
        }
    }
}
