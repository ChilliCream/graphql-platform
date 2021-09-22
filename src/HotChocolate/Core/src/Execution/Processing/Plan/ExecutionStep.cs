using System;
using System.Collections.Generic;

namespace HotChocolate.Execution.Processing.Plan
{
    /// <summary>
    /// Represents an execution step within a query plan.
    /// </summary>
    internal abstract class ExecutionStep
    {
        protected ExecutionStep(IReadOnlyList<ExecutionStep>? steps = null)
        {
            if (steps is not null)
            {
                ExecutionStep? previous = null;

                for (var index = 0; index < steps.Count; index++)
                {
                    ExecutionStep step = steps[index];
                    step.Parent = this;
                    step.Next = previous;
                    previous = step;
                }
            }

            Steps = steps ?? Array.Empty<ExecutionStep>();
        }

        /// <summary>
        /// Gets the unique identifier for this execution step within its query plan.
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// Gets a name for this execution step.
        /// </summary>
        public virtual string Name => GetType().Name;

        /// <summary>
        /// Gets the parent execution step.
        /// </summary>
        public ExecutionStep? Parent { get; private set; }

        /// <summary>
        /// Gets the next execution step within the parent.
        /// </summary>
        public ExecutionStep? Next { get; private set; }

        /// <summary>
        /// Gets a list of child steps.
        /// </summary>
        public IReadOnlyList<ExecutionStep> Steps { get; }

        /// <summary>
        /// Tries to initialize this execution step for the current request.
        /// If the initialization returns <c>false</c> and does not initialize it is
        /// not part of the current request and can be skipped.
        /// </summary>
        /// <param name="state">
        /// The current query plan execution state.
        /// </param>
        /// <returns>
        /// Returns <c>true</c> if this step is part of the current request;
        /// otherwise, <c>false</c>.
        /// </returns>
        public virtual bool TryInitialize(IQueryPlanState state) => true;

        /// <summary>
        /// Completes a task that was spawned from this execution step.
        /// </summary>
        /// <param name="state">
        /// The current query plan execution state.
        /// </param>
        /// <param name="task">
        /// The execution task that was spawned from the execution task.
        /// </param>
        public virtual void CompleteTask(IQueryPlanState state, IExecutionTask task) { }
    }
}
