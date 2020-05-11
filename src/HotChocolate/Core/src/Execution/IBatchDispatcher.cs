using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public interface IBatchDispatcher
    {
        bool HasTasks { get; }

        Task DispatchAsync(CancellationToken cancellationToken);
    }
}
