using System;

namespace HotChocolate.Execution.Processing
{
    internal sealed partial class OperationContext : IExecutionTaskContext
    {
        void IExecutionTaskContext.ReportError(IAsyncExecutionTask task, IError error)
        {
            ReportError(task, error);
        }

        void IExecutionTaskContext.ReportError(IAsyncExecutionTask task, Exception exception)
        {
            ReportError(task, ErrorHandler.CreateUnexpectedError(exception).Build());
        }

        private void ReportError(IAsyncExecutionTask task, IError error)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (error is null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            AssertNotPooled();
            error = ErrorHandler.Handle(error);
            Result.AddError(error);
            DiagnosticEvents.TaskError(task, error);
        }

        void IExecutionTaskContext.Started()
        {
            AssertNotPooled();
            Execution.TaskStats.TaskStarted();
        }

        void IExecutionTaskContext.Completed()
        {
            AssertNotPooled();
            Execution.TaskStats.TaskCompleted();
        }

        IDisposable IExecutionTaskContext.Track(IAsyncExecutionTask task)
        {
            AssertNotPooled();
            return DiagnosticEvents.RunTask(task);
        }
    }
}
