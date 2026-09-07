using static HotChocolate.Execution.Processing.ValueCompletion;

namespace HotChocolate.Execution.Processing.Tasks;

internal sealed partial class ResolverTask
{
    /// <summary>
    /// Completes the resolver result and returns the path a propagated null landed on,
    /// or <c>null</c> when no null was propagated.
    /// </summary>
    /// <param name="success">Defines if the resolver succeeded without errors.</param>
    /// <param name="cancellationToken">The execution cancellation token.</param>
    private Path? CompleteValue(bool success, CancellationToken cancellationToken)
    {
        var resultValue = _context.ResultValue;
        var result = _context.Result;

        try
        {
            // we will only try to complete the resolver value if there are no known errors.
            if (success)
            {
                var completionContext = new ValueCompletionContext(_operationContext, _context, _taskBuffer, BranchId);
                Complete(completionContext, _selection, resultValue, result);
            }
        }
        catch (OperationCanceledException)
        {
            // If we run into this exception the request was aborted.
            // In this case we do nothing and just return.
            _completionStatus = ExecutionTaskStatus.Faulted;
            _context.Result = null;
            return null;
        }
        catch (Exception ex)
        {
            _context.Result = null;

            if (!cancellationToken.IsCancellationRequested)
            {
                _context.ReportError(ex);
                resultValue.SetNullValue();
            }
        }

        if (resultValue is { IsNullable: false, IsNullOrInvalidated: true })
        {
            Path? nulledPath = null;

            if (_operationContext.PropagateNullValues)
            {
                nulledPath = PropagateNullValues(resultValue).Path;
            }
            else
            {
                resultValue.SetNullValue();
            }

            _completionStatus = ExecutionTaskStatus.Faulted;
            _operationContext.Result.AddNonNullViolation(_context.Path);
            _taskBuffer.Clear();
            return nulledPath;
        }

        return null;
    }

    /// <summary>
    /// Aborts the deferred and streamed branches that were rooted in the result data
    /// that a propagated null has just removed from the response.
    /// </summary>
    private ValueTask AbortBranchesAsync(Path nulledPath)
    {
        var coordinator = _operationContext.DeferExecutionCoordinator;

        if (!coordinator.HasBranches)
        {
            return ValueTask.CompletedTask;
        }

        // only the branches nested inside this task's branch lost their data; the branch itself
        // reports its own completion and sibling branches are unaffected.
        return coordinator.AbortBranchesAsync(nulledPath, CollectCausingErrors(), BranchId);
    }

    /// <summary>
    /// Collects the errors that caused this field to complete to null.
    /// </summary>
    private IReadOnlyList<IError> CollectCausingErrors()
    {
        var path = _context.Path;
        List<IError>? causingErrors = null;

        foreach (var error in _operationContext.Result.Errors)
        {
            if (error.Path is not null && DeferExecutionCoordinator.IsAtOrBelow(path, error.Path))
            {
                (causingErrors ??= []).Add(error);
            }
        }

        return causingErrors
            ?? (IReadOnlyList<IError>)[ErrorHelper.NonNullOutputFieldViolation().SetPath(path).Build()];
    }
}
