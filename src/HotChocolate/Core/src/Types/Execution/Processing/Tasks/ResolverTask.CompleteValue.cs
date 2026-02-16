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
            return;
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
            PropagateNullValues(resultValue);
            _completionStatus = ExecutionTaskStatus.Faulted;
            _operationContext.Result.AddNonNullViolation(_context.Path);
            _taskBuffer.Clear();
        }
    }
}
