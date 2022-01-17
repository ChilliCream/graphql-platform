using System;
using System.Collections.Generic;

namespace HotChocolate.Execution.Processing.Plan;

/// <summary>
/// The state of a query plan execution.
/// </summary>
internal interface IQueryPlanState : IObservable<ResolverResult>
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

    /// <summary>
    /// Triggered by the resolver execution step.
    /// </summary>
    /// <param name="selection">The selection that has be executed.</param>
    /// <param name="path">The execution path.</param>
    /// <param name="status">The execution status.</param>
    /// <param name="result">The resolver result.</param>
    void OnResolverCompleted(
        ISelection selection,
        Path path,
        ExecutionTaskStatus status,
        object? result);
}
