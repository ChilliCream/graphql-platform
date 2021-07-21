using System;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// The task backlog of the execution engine stores <see cref="IExecutionTask"/>
    /// without any guaranteed order.
    /// </summary>
    internal interface IWorkBacklog
    {
        /// <summary>
        /// A task that can be awaited to wait for the completion of the current work backlog.
        /// </summary>
        Task Completion { get; }

        /// <summary>
        /// Registers work with the task backlog.
        /// </summary>
        void Register(IExecutionTask task);

        /// <summary>
        /// Registers work with the task backlog.
        /// </summary>
        void Register(IExecutionTask?[] tasks, int length);

        /// <summary>
        /// Complete a task
        /// </summary>
        void Complete(IExecutionTask task);
    }
}
