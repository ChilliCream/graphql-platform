namespace HotChocolate.Execution.Processing;

internal sealed partial class OperationContext : IExecutionTaskContext
{
    void IExecutionTaskContext.ReportError(IExecutionTask task, IError error)
        => ReportError(task, error);

    void IExecutionTaskContext.ReportError(IExecutionTask task, Exception exception)
        => ReportError(task, ErrorHandler.CreateUnexpectedError(exception).Build());

    void IExecutionTaskContext.Register(IExecutionTask task)
        => Scheduler.Register(task);

    private void ReportError(IExecutionTask task, IError error)
    {
        if (task is null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        AssertInitialized();

        if (error is AggregateError aggregateError)
        {
            foreach (var innerError in aggregateError.Errors)
            {
                ReportSingle(innerError);
            }
        }
        else
        {
            ReportSingle(error);
        }

        void ReportSingle(IError singleError)
        {
            var handled = ErrorHandler.Handle(singleError);

            if (handled is AggregateError ar)
            {
                foreach (var ie in ar.Errors)
                {
                    Result.AddError(ie);
                    DiagnosticEvents.TaskError(task, ie);
                }
            }
            else
            {
                Result.AddError(handled);
                DiagnosticEvents.TaskError(task, handled);
            }
        }
    }

    void IExecutionTaskContext.Completed(IExecutionTask task)
    {
        AssertInitialized();
        Scheduler.Complete(task);
    }

    IDisposable IExecutionTaskContext.Track(IExecutionTask task)
    {
        AssertInitialized();
        return DiagnosticEvents.RunTask(task);
    }
}
