using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;

namespace HotChocolate.Execution.Batching
{
    class NoopContextBatchDispatcher : IContextBatchDispatcher
    {
        public IBatchDispatcher BatchDispatcher => NoopBatchDispatcher.Default;

        public void Register(IExecutionContext context)
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
