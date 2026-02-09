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
        internal set
        {
            _currentWorkScheduler = value;
        }
    }

    public DeferExecutionCoordinator DeferExecutionCoordinator => throw new NotImplementedException();

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
        int executionBranchId = DeferExecutionCoordinator.MainBranchId,
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
            executionBranchId,
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

        // TODO : we need to pool this still
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
