using System.Collections.Immutable;

namespace HotChocolate.Execution.Processing.Tasks;

internal sealed partial class ResolverTask
{
    /// <summary>
    /// Initializes this task after it is retrieved from its pool.
    /// </summary>
    public void Initialize(
        OperationContext operationContext,
        ISelection selection,
        ObjectResult parentResult,
        int responseIndex,
        object? parent,
        IImmutableDictionary<string, object?> scopedContextData,
        Path? path)
    {
        _operationContext = operationContext;
        _selection = selection;
        _context.Initialize(operationContext, selection, parentResult, responseIndex, parent, scopedContextData, path);
        ParentResult = parentResult;
        IsSerial = selection.Strategy is SelectionExecutionStrategy.Serial;
    }

    /// <summary>
    /// Resets the resolver task before returning it to the pool.
    /// </summary>
    /// <returns></returns>
    internal bool Reset()
    {
        _completionStatus = ExecutionTaskStatus.Completed;
        _operationContext = null!;
        _selection = null!;
        _context.Clean();
        ParentResult = null!;
        Status = ExecutionTaskStatus.WaitingToRun;
        IsSerial = false;
        IsRegistered = false;
        Next = null;
        Previous = null;
        State = null;
        _taskBuffer.Clear();
        _args.Clear();
        return true;
    }
}
