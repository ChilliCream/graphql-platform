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
        public bool IsCompleted
        {
            get
            {
                AssertNotPooled();
                return Work.IsEmpty && !Work.HasRunningTasks;
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

        /// <inheritdoc />
        public ObjectPool<PureResolverTask> PureResolverTasks
        {
            get
            {
                AssertNotPooled();
                return _pureResolverTasks;
            }
        }

        /// <inheritdoc />
        public ObjectPool<IExecutionTask?[]> TaskBuffers
        {
            get
            {
                AssertNotPooled();
                return _taskBuffers;
            }
        }

        private void TryDispatchBatches()
        {
            AssertNotPooled();

            if (Work.IsEmpty && BatchDispatcher.HasTasks)
            {
                using (_diagnosticEvents.DispatchBatch(_operationContext.RequestContext))
                {
                    BatchDispatcher.Dispatch();
                }
            }
        }

        private void BatchDispatcherEventHandler(
            object? source, EventArgs args) =>
            TryDispatchBatches();
    }
}
