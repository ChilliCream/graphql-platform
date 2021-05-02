using System;

namespace HotChocolate.Execution
{
    public interface IExecutionTaskContext
    {
        void ReportError(IExecutionTask task, IError error);

        void ReportError(IExecutionTask task, Exception exception);

        IDisposable Track(IExecutionTask task);

        void Started(IExecutionTask task);

        void Completed(IExecutionTask task);
    }
}
