using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;

namespace MarshmallowPie.Storage.AzureBlob
{
    public class File
        : IFile
    {
        private readonly ICloudBlob _blob;

        public File(ICloudBlob blob)
        {
            _blob = blob;
        }

        public string Name => _blob.Name;

        public Task DeleteAsync(CancellationToken cancellationToken = default) =>
            _blob.DeleteIfExistsAsync(cancellationToken);

        public async Task<Stream> OpenAsync(
            CancellationToken cancellationToken = default)
        {
            var stream = new MemoryStream();
            await _blob.DownloadToStreamAsync(
                stream, cancellationToken)
                .ConfigureAwait(false);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }
}
