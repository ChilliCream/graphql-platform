using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Storage
{
    public interface IFile
    {
        string Name { get; }

        Task<Stream> OpenAsync(CancellationToken cancellationToken = default);

        Task DeleteAsync(CancellationToken cancellationToken = default);
    }
}
