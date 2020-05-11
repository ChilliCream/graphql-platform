using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.DataLoader
{
    public interface IBatchDispatcher
    {
        bool HasTasks { get; }

        Task DispatchAsync(CancellationToken cancellationToken);
    }
}
