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
        Path? path)
    {
        _operationContext = operationContext;
        _selection = selection;
        _context.Initialize(parent, selection, resultValue, operationContext, scopedContextData, path);
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
