using System;
using System.Threading;
using HotChocolate.Types;

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
        object? completedValue = null;

        try
        {
            // we will only try to complete the resolver value if there are no known errors.
            if (success)
            {
                completedValue = ValueCompletion.Complete(
                    _operationContext,
                    _context,
                    _taskBuffer,
                    _context.Selection,
                    _context.Path,
                    _selection.Type,
                    _context.ResponseName,
                    _context.ResponseIndex,
                    _context.Result);

                if (completedValue is ResultData result)
                {
                    result.Parent = _context.ParentResult;
                }
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
                completedValue = null;
            }
        }

        var isNonNullType = _selection.Type.Kind is TypeKind.NonNull;

        _context.ParentResult.SetValueUnsafe(
            _context.ResponseIndex,
            _context.ResponseName,
            completedValue,
            !isNonNullType);

        if (completedValue is null && isNonNullType)
        {
            // if we detect a non-null violation we will stash it for later.
            // the non-null propagation is delayed so that we can parallelize better.
            _completionStatus = ExecutionTaskStatus.Faulted;
            _operationContext.Result.AddNonNullViolation(
                _context.Selection,
                _context.Path,
                _context.ParentResult);
            _taskBuffer.Clear();
        }
    }
}
