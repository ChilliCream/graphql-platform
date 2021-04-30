using System;

namespace HotChocolate.Execution.Processing
{
    internal sealed partial class OperationContext : IExecutionTaskContext
    {
        void IExecutionTaskContext.ReportError(IExecutionTask task, IError error)
        {
            ReportError(task, error);
        }

        void IExecutionTaskContext.ReportError(IExecutionTask task, Exception exception)
        {
            ReportError(task, ErrorHandler.CreateUnexpectedError(exception).Build());
        }

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
            error = ErrorHandler.Handle(error);
            Result.AddError(error);
            DiagnosticEvents.TaskError(task, error);
        }

        void IExecutionTaskContext.Started()
        {
            AssertInitialized();
            Execution.TaskStats.TaskStarted();
        }

        void IExecutionTaskContext.Completed()
        {
            AssertInitialized();
            Execution.TaskStats.TaskCompleted();
        }

        IDisposable IExecutionTaskContext.Track(IExecutionTask task)
        {
            AssertInitialized();
            return DiagnosticEvents.RunTask(task);
        }
    }
}
