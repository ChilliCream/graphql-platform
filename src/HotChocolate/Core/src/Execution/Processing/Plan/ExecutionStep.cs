using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HotChocolate.Execution.Processing.Plan;

/// <summary>
/// Represents an execution step within a query plan.
/// </summary>
internal abstract class ExecutionStep
{
    protected ExecutionStep(IReadOnlyList<ExecutionStep>? steps = null)
    {
        if (steps is not null)
        {
            Debug.Assert(steps.Count > 0, "Steps cannot be empty.");

            ExecutionStep? previous = null;

            for (var index = 0; index < steps.Count; index++)
            {
                ExecutionStep step = steps[index];
                step.Parent = this;

                if (previous is not null)
                {
                    previous.Next = step;
                }

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
    /// Tries to activate this execution step for the current request.
    /// If the activation returns <c>false</c> and it is
    /// not part of the current request and can be skipped.
    /// </summary>
    /// <param name="state">
    /// The current query plan execution state.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if this step is part of the current request;
    /// otherwise, <c>false</c>.
    /// </returns>
    public virtual bool TryActivate(IQueryPlanState state) => true;

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

    /// <summary>
    /// Defines if this execution step owns the given task.
    /// </summary>
    /// <param name="task">
    /// The execution task that is evaluated.
    /// </param>
    /// <returns>
    /// <c>true</c> if the task is owned by this step; otherwise, <c>false</c>.
    /// </returns>
    public virtual bool IsOwningTask(IExecutionTask task) => false;
}
