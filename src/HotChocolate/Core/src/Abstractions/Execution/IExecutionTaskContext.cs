using System;

namespace HotChocolate.Execution
{
    public interface IExecutionTaskContext
    {
        void ReportError(IAsyncExecutionTask task, IError error);

        void ReportError(IAsyncExecutionTask task, Exception exception);

        IDisposable Track(IAsyncExecutionTask task);

        void Started();

        void Completed();
    }
}
