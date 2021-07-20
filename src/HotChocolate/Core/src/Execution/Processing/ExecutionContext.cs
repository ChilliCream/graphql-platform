using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Fetching;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal partial class ExecutionContext : IExecutionContext
    {
        /// <inheritdoc />
        public IWorkBacklog Work
        {
            get
            {
                AssertNotPooled();
                return _workBacklog;
            }
        }

        /// <inheritdoc />
        public IDeferredWorkBacklog DeferredWork
        {
            get
            {
                AssertNotPooled();
                return _deferredWorkBacklog;
            }
        }

        /// <inheritdoc />
        public IBatchDispatcher BatchDispatcher
        {
            get
            {
                AssertNotPooled();
                return _batchDispatcher;
            }
        }

        /// <inheritdoc />
        public ObjectPool<ResolverTask> ResolverTasks
        {
            get
            {
                AssertNotPooled();
                return _resolverTasks;
            }
        }
    }
}
