using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Utilities
{
    /// <summary>
    /// The task backlog of the execution engine stores <see cref="ITask"/> 
    /// without any guaranteed order.
    /// </summary>
    internal interface ITaskBacklog
    {
        /// <summary>
        /// Try to take a task from the backlog.
        /// </summary>
        /// <param name="task">
        /// The task that was acquired from the backlog. 
        /// The task is not null when the method returns<c>true</c>
        /// </param>
        /// <returns>Return <c>true</c> if there was a task on the backlog.</returns>
        bool TryTake([NotNullWhen(true)] out ITask? task);

        /// <summary>
        /// Waits for either the <paramref name="cancellationToken" /> to raise or 
        /// for a task to be added to the backlog.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// Returns a boolean indicating if there is something new to read on this queue.
        /// </returns>
        ValueTask<bool> WaitForTaskAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Registers work with the task backlog.
        /// </summary>
        void Register(ResolverTaskDefinition taskDefinition);

        /// <summary>
        /// Clears the queue and returns all the <see cref="ResolverTask"/> instances to the pool.
        /// </summary>
        void Reset();
    }
}
