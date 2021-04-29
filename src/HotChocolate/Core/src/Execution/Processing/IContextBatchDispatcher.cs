using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Fetching;

namespace HotChocolate.Execution.Processing
{
    internal interface IContextBatchDispatcher
    {
        /// <summary>Scheduler for all non-batch tasks</summary>
        TaskScheduler TaskScheduler { get; }

        /// <summary>Marks a context as having started</summary>
        void Register(IExecutionContext context, CancellationToken ctx);

        /// <summary>Marks a context as having completed</summary>
        void Unregister(IExecutionContext context);

        /// <summary>Suspend the batch dispatching for all registered contexts</summary>
        void Suspend();

        /// <summary>Resume the batch dispatching for all registered contexts</summary>
        void Resume();
    }
}
