using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Fetching
{
    public interface IBatchDispatcher
    {
        bool HasTasks { get; }

        event EventHandler? TaskEnqueued;

        Task DispatchAsync(CancellationToken cancellationToken);
    }
}
