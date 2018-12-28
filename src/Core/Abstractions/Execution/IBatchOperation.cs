using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public interface IBatchOperation
    {
        event EventHandler<EventArgs> BatchSizeIncreased;

        /// <summary>
        /// Gets count of items in the current batch.
        /// </summary>
        int BatchSize { get; }

        /// <summary>
        /// Executes the current batch
        /// </summary>
        /// <returns></returns>
        Task InvokeAsync(CancellationToken cancellationToken);
    }
}
