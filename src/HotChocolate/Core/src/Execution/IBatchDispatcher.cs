using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public interface IBatchDispatcher
    {
        event EventHandler<EventArgs> TaskEnqueued;

        bool HasTasks { get; }

        Task DispatchAsync(CancellationToken cancellationToken);
    }
}
