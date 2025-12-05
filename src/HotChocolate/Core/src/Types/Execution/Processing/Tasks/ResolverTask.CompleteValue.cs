using HotChocolate.Types;
using static HotChocolate.Execution.Processing.ValueCompletion;

namespace HotChocolate.Execution.Processing.Tasks;

internal sealed partial class ResolverTask
{
    /// <summary>
    /// Completes the resolver result.
    /// </summary>
    /// <param name="success">Defines if the resolver succeeded without errors.</param>
    /// <param name="cancellationToken">The execution cancellation token.</param>
    private void CompleteValue(bool success, CancellationToken cancellationToken)
    {
        var responseIndex = _context.ResponseIndex;
        var responseName = _context.ResponseName;
        var parentResult = _context.ParentResult;
        var result = _context.Result;
        object? completedResult = null;

        try
        {
            // we will only try to complete the resolver value if there are no known errors.
            if (success)
            {
                var completionContext = new ValueCompletionContext(_operationContext, _context, _taskBuffer);
                completedResult = Complete(completionContext, _selection, parentResult, responseIndex, result);
            }
        }
        catch (OperationCanceledException)
        {
            // If we run into this exception the request was aborted.
            // In this case we do nothing and just return.
            _completionStatus = ExecutionTaskStatus.Faulted;
            _context.Result = null;
            return;
        }
        catch (Exception ex)
        {
            _context.Result = null;

            if (!cancellationToken.IsCancellationRequested)
            {
                _context.ReportError(ex);
                completedResult = null;
            }
        }

        var isNonNullType = _selection.Type.Kind is TypeKind.NonNull;
        _context.ParentResult.SetValueUnsafe(responseIndex, responseName, completedResult, !isNonNullType);

        if (completedResult is null && isNonNullType)
        {
            PropagateNullValues(parentResult);
            _completionStatus = ExecutionTaskStatus.Faulted;
            _operationContext.Result.AddNonNullViolation(_selection, _context.Path);
            _taskBuffer.Clear();
        }
    }
}
