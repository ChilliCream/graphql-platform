using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// The task backlog of the execution engine stores <see cref="IExecutionTask"/>
    /// without any guaranteed order.
    /// </summary>
    internal interface ITaskBacklog
    {
        /// <summary>
        /// Defines if the backlog is empty and has no more tasks or has been completed.
        /// </summary>
        bool IsIdle { get; }

        /// <summary>Waits till the task backlog is empty</summary>
        public Task WaitTillIdle(CancellationToken? ctx = null);

        /// <summary>
        /// Tries to remove an item from the queue.
        /// the item will not be counted as removed until the receiver has completed.
        /// </summary>
        /// <returns>true if receiver was called (throws if receiver throws)</returns>
        bool TryTake(Action<IExecutionTask> receiver);

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
        /// Registers work with the task backlog.
        /// </summary>
        void Register(IExecutionTask task);
    }
}
