using System.Collections.Generic;

namespace HotChocolate.Execution.Processing.Plan
{
    /// <summary>
    /// The state of a query plan execution.
    /// </summary>
    internal interface IQueryPlanState
    {
        /// <summary>
        /// Gets the current operation context.
        /// </summary>
        IOperationContext Context { get; }

        /// <summary>
        /// Gets a set representing the selections that were registered for execution.
        /// </summary>
        ISet<int> Selections { get; }

        /// <summary>
        /// Registers work with the task backlog.
        /// </summary>
        void RegisterUnsafe(IReadOnlyList<IExecutionTask> tasks);
    }
}
