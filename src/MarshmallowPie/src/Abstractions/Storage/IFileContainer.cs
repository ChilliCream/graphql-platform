using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Storage
{
    public interface IFileContainer
    {
        string Name { get; }

        Task<IEnumerable<IFile>> GetFilesAsync(
            CancellationToken cancellationToken = default);

        Task<IFile> GetFileAsync(
            string fileName,
            CancellationToken cancellationToken = default);

        Task CreateFileAsync(
            string fileName,
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(CancellationToken cancellationToken = default);
    }
}
