using System.Collections.Immutable;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Text.Json;

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
            return _currentWorkScheduler;
        }
    }

    public DeferExecutionCoordinator DeferExecutionCoordinator
    {
        get
        {
            AssertInitialized();
            return _currentDeferExecutionCoordinator;
        }
    }

    public int ExecutionBranchId
    {
        get
        {
            AssertInitialized();
            return _branchId;
        }
    }

    public OperationResultBuilder Result { get; } = new();

    public RequestContext RequestContext
    {
        get
        {
            AssertInitialized();
            return _requestContext;
        }
    }

    public ResolverTask CreateResolverTask(
        object? parent,
        Selection selection,
        ResultElement resultValue,
        IImmutableDictionary<string, object?> scopedContextData,
        int? executionBranchId = null,
        DeferUsage? deferUsage = null)
    {
        AssertInitialized();

        var resolverTask = _resolverTaskFactory.Create();

        resolverTask.Initialize(
            parent,
            selection,
            resultValue,
            this,
            scopedContextData,
            executionBranchId ?? _branchId,
            deferUsage);

        return resolverTask;
    }

    public DeferTask CreateDeferTask(
        SelectionSet selectionSet,
        Path selectionPath,
        object? parent,
        IImmutableDictionary<string, object?> scopedContextData,
        int executionBranchId,
        DeferUsage deferUsage)
    {
        AssertInitialized();

        var deferTask = new DeferTask();

        deferTask.Initialize(
            this,
            parent,
            scopedContextData,
            selectionSet,
            selectionPath,
            executionBranchId,
            deferUsage);

        return deferTask;
    }
}
