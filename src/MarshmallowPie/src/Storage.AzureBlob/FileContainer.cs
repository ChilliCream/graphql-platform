using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;

namespace MarshmallowPie.Storage.AzureBlob
{
    public class FileContainer : IFileContainer
    {
        private readonly CloudBlobClient _client;
        private readonly CloudBlobDirectory _directory;

        public FileContainer(
            CloudBlobClient client,
            CloudBlobDirectory directory,
            string name)
        {
            _client = client;
            _directory = directory;
            Name = name;
        }

        public string Name { get; }

        public Task CreateFileAsync(
            string fileName, byte[] buffer, int offset, int count,
            CancellationToken cancellationToken = default)
        {
            return _directory
                .GetBlockBlobReference(fileName)
                .UploadFromByteArrayAsync(buffer, offset, count, cancellationToken);
        }

        public Task DeleteAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(async () =>
            {
                foreach (IListBlobItem listItem in _directory.ListBlobs())
                {
                    ICloudBlob blob = await _client.GetBlobReferenceFromServerAsync(
                        listItem.Uri, cancellationToken)
                        .ConfigureAwait(false);
                    await blob.DeleteIfExistsAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public async Task<IFile> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
        {
            CloudBlockBlob blob = _directory.GetBlockBlobReference(fileName);

            if (await blob.ExistsAsync().ConfigureAwait(false) == false)
            {
                throw new DirectoryNotFoundException(
                    $"The file `{blob.Uri}` does not exist.");
            }

            return new File(blob);
        }

        public async Task<IEnumerable<IFile>> GetFilesAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(async () =>
            {
                var list = new List<IFile>();

                foreach (IListBlobItem listItem in _directory.ListBlobs())
                {
                    ICloudBlob blob = await _client.GetBlobReferenceFromServerAsync(
                        listItem.Uri, cancellationToken)
                        .ConfigureAwait(false);
                    list.Add(new File(blob));
                }

                return list;
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
