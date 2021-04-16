using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.Batching
{
    /// <summary>
    /// Wraps an execution task tracker so that batch dispatching is suspended for as long as any task
    /// that is started in it is not completed.
    /// </summary>
    internal class ContextDispatchBlocker : IExecutionTaskContext
    {
        private readonly IExecutionTaskContext _context;
        private readonly IContextBatchDispatcher _dispatcher;

        public ContextDispatchBlocker(IExecutionTaskContext context, IContextBatchDispatcher dispatcher)
        {
            _context = context;
            _dispatcher = dispatcher;
        }

        public void Completed()
        {
            _context.Completed();
            _dispatcher.Resume();
        }

        public void ReportError(IExecutionTask task, IError error)
        {
            _context.ReportError(task, error);
        }

        public void ReportError(IExecutionTask task, Exception exception)
        {
            _context.ReportError(task, exception);
        }

        public void Started()
        {
            _dispatcher.Suspend();
            _context.Started();
        }

        public IDisposable Track(IExecutionTask task)
        {
            return _context.Track(task);
        }
    }
}
