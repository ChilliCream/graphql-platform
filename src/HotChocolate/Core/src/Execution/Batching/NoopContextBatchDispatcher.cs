using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;

namespace HotChocolate.Execution.Batching
{
    class NoopContextBatchDispatcher : IContextBatchDispatcher
    {
        public IBatchDispatcher BatchDispatcher => NoopBatchDispatcher.Default;

        public TaskScheduler TaskScheduler => TaskScheduler.Current;

        public void Register(IExecutionContext context, CancellationToken ctx)
        {
            // empty
        }

        public void Resume()
        {
            // empty
        }

        public void Suspend()
        {
            // empty
        }

        public void Unregister(IExecutionContext context)
        {
            // empty
        }

        public static NoopContextBatchDispatcher Default { get; } = new NoopContextBatchDispatcher();
    }
}
