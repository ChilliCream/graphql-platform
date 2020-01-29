using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IOFile = System.IO.File;

namespace MarshmallowPie.Storage.FileSystem
{
    public class File : IFile
    {
        private readonly string _fullFilePath;

        public File(string fullFilePath)
        {
            _fullFilePath = fullFilePath;
            Name = Path.GetFileName(fullFilePath);
        }

        public string Name { get; }

        public async Task<Stream> OpenAsync(CancellationToken cancellationToken = default)
        {
            if (IOFile.Exists(_fullFilePath))
            {
                return await Task.Factory.StartNew(
                    () => IOFile.Open(_fullFilePath, FileMode.Open),
                    cancellationToken,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default)
                    .ConfigureAwait(false);
            }

            throw new FileNotFoundException(
                $"The file `{_fullFilePath}` does not exist.");
        }

        public Task DeleteAsync(CancellationToken cancellationToken = default)
        {
            if (IOFile.Exists(_fullFilePath))
            {
                return Task.Factory.StartNew(
                    () => IOFile.Delete(_fullFilePath),
                    cancellationToken,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
            }

            throw new FileNotFoundException(
                $"The file `{_fullFilePath}` does not exist.");
        }
    }
}
