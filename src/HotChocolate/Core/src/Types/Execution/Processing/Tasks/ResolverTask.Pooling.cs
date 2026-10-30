using System.Collections.Immutable;
using HotChocolate.Text.Json;

namespace HotChocolate.Execution.Processing.Tasks;

internal sealed partial class ResolverTask
{
    /// <summary>
    /// Initializes this task after it is retrieved from its pool.
    /// </summary>
    public void Initialize(
        object? parent,
        Selection selection,
        ResultElement resultValue,
        OperationContext operationContext,
        IImmutableDictionary<string, object?> scopedContextData,
        int executionBranchId,
        DeferUsage? deferUsage)
    {
        _operationContext = operationContext;
        _isContextReleased = false;
        _selection = selection;
        _context.Initialize(parent, selection, resultValue, operationContext, deferUsage, scopedContextData);
        _context.BranchId = executionBranchId;
        IsSerial = selection.Strategy is SelectionExecutionStrategy.Serial;
        BranchId = executionBranchId;
        DeferUsage = deferUsage;
    }

    /// <summary>
    /// Makes this task a participant of the execution group scope if the selection is part of its plan.
    /// </summary>
    internal void JoinExecutionGroup(ExecutionGroupScope? scope)
    {
        if (scope is not null && scope.Plan.TryGetNode(_selection.Id, out var node))
        {
            _groupScope = scope;
            _groupSlot = node.SlotIndex;
        }
    }

    /// <summary>
    /// Resets the resolver task before returning it to the pool.
    /// </summary>
    /// <returns>Always <c>true</c>.</returns>
    internal bool Reset()
    {
        ReleaseOperationContext();
        _completionStatus = ExecutionTaskStatus.Completed;
        _operationContext = null!;
        _selection = null!;
        _context.Clean();
        Status = ExecutionTaskStatus.WaitingToRun;
        IsSerial = false;
        BranchId = int.MinValue;
        DeferUsage = null;
        _groupScope = null;
        _groupSlot = -1;
        IsRegistered = false;
#pragma warning disable CS0618
        Next = null;
        Previous = null;
#pragma warning restore CS0618
        State = null;
        _taskBuffer.Clear();
        _args.Clear();
        return true;
    }

    /// <summary>
    /// Tells the operation context that this task no longer uses it. Calling it again has no effect.
    /// </summary>
    internal void ReleaseOperationContext()
    {
        if (_isContextReleased || _operationContext is null)
        {
            return;
        }

        _isContextReleased = true;
        _operationContext.TaskReturned();
    }

    private void CompleteOnScheduler()
    {
        // the context can be reused as soon as it is released, so the scheduler is read first.
        var scheduler = _operationContext.Scheduler;
        ReleaseOperationContext();
        scheduler.Complete(this);
    }
}
