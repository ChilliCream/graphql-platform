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
        event EventHandler<EventArgs>? NeedsMoreWorker;

        /// <summary>
        /// Defines if the backlog is empty and has no more tasks.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Try to take a task from the backlog.
        /// </summary>
        /// <param name="task">
        /// The task that was acquired from the backlog.
        /// The task is not null when the method returns<c>true</c>
        /// </param>
        /// <returns>Return <c>true</c> if there was a task on the backlog.</returns>
        bool TryTake([NotNullWhen(true)] out IExecutionTask? task);

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
