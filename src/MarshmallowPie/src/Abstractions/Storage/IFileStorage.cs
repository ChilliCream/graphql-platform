using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Storage
{
    public interface IFileStorage
    {
        Task<IFileContainer> CreateContainerAsync(
            string containerName,
            CancellationToken cancellationToken = default);

        Task<IFileContainer> GetContainerAsync(
            string containerName,
            CancellationToken cancellationToken = default);

        Task<IFileContainer> GetOrCreateContainerAsync(
            string containerName,
            CancellationToken cancellationToken = default);
    }
}
