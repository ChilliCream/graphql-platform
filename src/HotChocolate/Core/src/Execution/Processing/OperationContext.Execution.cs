using System;
using System.Collections.Immutable;
using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing;

internal sealed partial class OperationContext
{
    /// <summary>
    /// The work scheduler organizes the processing of request tasks.
    /// </summary>
    public WorkScheduler Scheduler
    {
        get
        {
            AssertInitialized();
            return _workScheduler;
        }
    }

    /// <summary>
    /// Gets the backlog of the task that shall be processed after
    /// all the main tasks have been executed.
    /// </summary>
    public DeferredWorkScheduler DeferredScheduler
    {
        get
        {
            AssertInitialized();
            return _deferredWorkScheduler;
        }
    }

    /// <summary>
    /// The result helper which provides utilities to build up the result.
    /// </summary>
    public ResultBuilder Result
    {
        get
        {
            AssertInitialized();
            return _resultBuilder;
        }
    }

    public ResolverTask CreateResolverTask(
        ISelection selection,
        object? parent,
        ObjectResult parentResult,
        int responseIndex,
        IImmutableDictionary<string, object?> scopedContextData,
        Path? path = null)
    {
        AssertInitialized();

        var resolverTask = _resolverTaskFactory.Create();

        resolverTask.Initialize(
            this,
            selection,
            parentResult,
            responseIndex,
            parent,
            scopedContextData,
            path);

        return resolverTask;
    }
}
